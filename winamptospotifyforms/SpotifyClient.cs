using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace winamptospotifyforms
{
    public class SpotifyClient
    {

        private string CLIENT_ID = ConfigurationManager.AppSettings["ClientID"];
        private string CLIENT_SECRET = ConfigurationManager.AppSettings["SecretID"];
        private string PLAYLIST_BASE_URL = ConfigurationManager.AppSettings["BasePlaylistUrl"] + ConfigurationManager.AppSettings["UserID"] + "/playlists";
        private string TRACK_SEARCH_BASE_URL = ConfigurationManager.AppSettings["TrackSearchBaseUrl"];
        private string PLAYLIST_ADD_TRACK_BASE_URL = ConfigurationManager.AppSettings["PlaylistAddTrackhBaseUrl"];
        private string FORGE_CALLBACK_URL = ConfigurationManager.AppSettings["ForgeCallbackUrl"];
        private string FORGE_SCOPE = ConfigurationManager.AppSettings["ForgeScope"];
        private readonly ILogger logger = new LoggerConfiguration().WriteTo.File("log-spotify-client-.txt", rollingInterval: RollingInterval.Day).CreateLogger();


        /// <summary>Gets Access Token from Spotify Web API./// </summary>
        /// <param name="code"></param>
        /// <returns>Access token string</returns>
        public async Task<string> GetAccessToken(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentNullException($"{nameof(code)} is empty");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    logger.Information(Environment.NewLine + "Your basic bearer: " + Convert.ToBase64String(Encoding.ASCII.GetBytes(CLIENT_ID + ":" + CLIENT_SECRET)));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(CLIENT_ID + ":" + CLIENT_SECRET)));

                    FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("redirect_uri", FORGE_CALLBACK_URL),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    });

                    var result = await client.PostAsync(ConfigurationManager.AppSettings["SpotityTokenBaseUrl"], formContent);
                    result.EnsureSuccessStatusCode();
                    var content = await result.Content.ReadAsStringAsync();
                    var spotifyAuth = JsonConvert.DeserializeObject<SpotifyJsonResponseWrapper.AccessToken>(content);
                    return spotifyAuth.access_token;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }
        }

        /// <summary>Creates playlist on Spotify.</summary>
        /// <param name="playlistname"></param>
        /// <param name="access_token"></param>
        /// <returns>Playlist Id returned from Spotify API</returns>
        public async Task<string> CreatePlayList(string playlistname, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(playlistname)) throw new ArgumentNullException($"{nameof(playlistname)} is empty");
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException($"{nameof(accessToken)} is empty");

            string playlistId = "";
            var stringPayload = new
            {
                name = playlistname,
                description = playlistname
            };
            var bodyPayload = new StringContent(JsonConvert.SerializeObject(stringPayload), Encoding.UTF8, "application/json");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                    var result = await client.PostAsync(PLAYLIST_BASE_URL, bodyPayload);
                    result.EnsureSuccessStatusCode();
                    var content = await result.Content.ReadAsStringAsync();
                    var playlist = JsonConvert.DeserializeObject<SpotifyJsonResponseWrapper.PlayList>(content);
                    playlistId = playlist.id;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }

            logger.Information($"{playlistname} created successfully");
            return playlistId;
        }

        public Uri GetAuthorizationURL()
        {
            var qb = new QueryBuilder();
            qb.Add("response_type", "code");
            qb.Add("client_id", CLIENT_ID);
            qb.Add("scope", FORGE_SCOPE);
            qb.Add("redirect_uri", FORGE_CALLBACK_URL);

            return new Uri($"https://accounts.spotify.com/authorize/{qb.ToQueryString()}");
        }

        /// <summary>Returns track uris.</summary>
        /// <param name="filePath"></param>
        /// <param name="accessToken"></param>
        /// <returns>List string with track URIs</returns>
        public async Task<Dictionary<string, string>> GetTrackUri(string filePath, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException($"{nameof(filePath)} is empty");
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException($"{nameof(accessToken)} is empty");

            Dictionary<string, string> trackInfoDict = new Dictionary<string, string>();

            string artist = filePath.Split('\\')[filePath.Split('\\').Length - 1].Split(' ')[0];
            bool isArtistNameExistsInFolderPath = false;
            List<string> fileNamesList = new FolderOperations().GetMp3FileNames(filePath, artist, ref isArtistNameExistsInFolderPath);

            if (fileNamesList.Count > 0)
            {
                foreach (var item in fileNamesList)
                {
                    var qb = new QueryBuilder();
                    var queryTrackString = item;
                    if (isArtistNameExistsInFolderPath)
                        queryTrackString += $" artist:{artist}";
                    qb.Add("q", queryTrackString);
                    qb.Add("type", "track");
                    qb.Add("limit", "1");
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                            var trackUrl = TRACK_SEARCH_BASE_URL + qb.ToQueryString().ToString();
                            var result = await client.GetAsync(trackUrl);
                            if (result.IsSuccessStatusCode)
                            {
                                var content = await result.Content.ReadAsStringAsync();
                                var results = JsonConvert.DeserializeObject<SpotifyJsonResponseWrapper.RootObject>(content);
                                var tracks = results.tracks;
                                if (tracks.items.Count > 0)
                                {
                                    trackInfoDict.TryAdd(tracks.items[0].uri, tracks.items[0].name);
                                    logger.Information($"Track {tracks.items[0].name} found.");
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message);
                        throw;
                    }

                }

                return trackInfoDict;
            }
            else
            {
                return new Dictionary<string, string>();
            }
        }


        /// <summary>/// Add tracks to playlist.</summary>
        /// <param name="playlistId">Created playlist ID</param>
        /// <param name="uris">Track uris</param>
        /// <param name="accessToken">Access Token</param>
        public async Task<bool> AddTrackToPlaylist(string playlistId, string uris, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(playlistId)) throw new ArgumentNullException($"{nameof(playlistId)} is empty");
            if (string.IsNullOrWhiteSpace(uris)) throw new ArgumentNullException($"{nameof(uris)} is empty");
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException($"{nameof(accessToken)} is empty");

            var qb = new QueryBuilder();
            qb.Add("uris", uris);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                    var result = await client.PostAsync(PLAYLIST_ADD_TRACK_BASE_URL.Replace("{playlist_id}", playlistId) + qb.ToQueryString(), null);
                    result.EnsureSuccessStatusCode();
                    var responseContent = await result.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(responseContent))
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }
        }
    }
}
