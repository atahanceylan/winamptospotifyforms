using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using winamptospotifyforms;

namespace winamptospotifyTest
{
    public class SpotifyClientTest
    {
        private readonly SpotifyClient spotifyClient = new SpotifyClient();
        private const string spotifyUserName = "costanzo88";
        private string playlistBaseUrl = $"https://api.spotify.com/v1/users/{spotifyUserName}/playlists";
        private const string trackSearchBaseUrl = "https://api.spotify.com/v1/search";
        private const string playlistAddTrackhBaseUrl = "https://api.spotify.com/v1/playlists/{playlist_id}/tracks";

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void GetAccessToken_Should_Throw_ArgumentNullException_When_Code_IsNullOrWhitespace()
        {
            //Arrange
            string code = null;

            //Act            
            AsyncTestDelegate act = async () => await spotifyClient.GetAccessToken(code);

            // Assert
            Assert.That(act, Throws.TypeOf<ArgumentNullException>());

        }

        [Test]
        public void CreatePlayList_Should_Throw_ArgumentException_When_AccessToken_IsNullOrWhitespace()
        {
            //Arrange
            string access_token = null;

            //Act            
            AsyncTestDelegate act = async () => await spotifyClient.CreatePlayList("dummyPlaylistName", access_token);

            // Assert
            Assert.That(act, Throws.TypeOf<ArgumentNullException>());

        }

        [Test]
        public void CreatePlayList_Should_Throw_ArgumentException_When_PlaylistName_IsNullOrWhitespace()
        {
            //Arrange
            string playlistname = null;

            //Act            
            AsyncTestDelegate act = async () => await spotifyClient.CreatePlayList(playlistname, "dummyAccessToken");

            // Assert
            Assert.That(act, Throws.TypeOf<ArgumentNullException>());

        }


        [Test]
        [TestCase("asdfsafas")]
        public async Task GetAccessToken_Should_Return_Http_Not_Found(string code)
        {
            //Arrange
            var fakeResponseHandler = new FakeResponseHandler();
            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test"), new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(fakeResponseHandler);

            string clientID = "yourClientID";
            string clientSecret = "yourClientSecret";
            string FORGE_CALLBACK_URL = "https://fake.com/api/forge/callback/oauth";
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(clientID + ":" + clientSecret)));

            FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
            {
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("redirect_uri", FORGE_CALLBACK_URL),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
            });

            //Act            
            var result = await httpClient.PostAsync("https://accounts.spotify.com/api/token", formContent);

            //Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        [TestCase("Michael Jackson", "accessToken")]
        public async Task CreatePlaylist_Should_Return_Http_Not_found(string playlistname, string accessToken)
        {
            //Arrange
            var fakeResponseHandler = new FakeResponseHandler();
            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test"), new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(fakeResponseHandler);
            var stringPayload = new
            {
                name = playlistname,
                description = playlistname
            };
            var bodyPayload = new StringContent(JsonConvert.SerializeObject(stringPayload), Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            //Act
            var result = await httpClient.PostAsync(playlistBaseUrl, bodyPayload);

            //Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        [TestCase("N:\\Fake", "accessToken")]
        public async Task GetTrackUri_Should_Return_Http_Not_found(string filePath, string accessToken)
        {

            //Arrange
            var fakeResponseHandler = new FakeResponseHandler();
            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test"), new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(fakeResponseHandler);

            string artist = "Michael Jackson";

            var qb = new QueryBuilder();
            qb.Add("q", $"artist:{artist} " + "Beat It");
            qb.Add("type", "track");
            qb.Add("limit", "1");

            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            var trackUrl = trackSearchBaseUrl + qb.ToQueryString().ToString();

            //Act
            var result = await httpClient.GetAsync(trackUrl);

            //Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        [TestCase("playlistId212313", "uri1,uri2,uri3", "accessToken")]
        public async Task AddTrackToPlaylistFunc_Should_Return_Http_Not_found(string playlistId, string uris, string accessToken)
        {
            //Arrange
            var fakeResponseHandler = new FakeResponseHandler();
            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test"), new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(fakeResponseHandler);

            var qb = new QueryBuilder();
            qb.Add("uris", uris);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            //Act
            var result = await httpClient.PostAsync(playlistAddTrackhBaseUrl.Replace("{playlist_id}", playlistId) + qb.ToQueryString(), null);

            //Assert
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        }
    }
}