using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace winamptospotifyforms
{
    [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
    public partial class WinampToSpotify : Form
    {
        private Button selectFolderButton;
        private TextBox folderNameTxt;
        private TextBox accessTokenTxt;
        private TextBox resultTxt;
        private Button getAccessToken;
        private FolderBrowserDialog openFolderDialog;        
        private SpotifyClient spotifyClient = new SpotifyClient();
        private CustomWebBrowser webBrowser = new CustomWebBrowser()
        {
            Size = new Size(600, 800),
            Location = new Point(15, 80)
        };


        private readonly ILogger logger = new LoggerConfiguration().WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();
             
        public WinampToSpotify()
        {            
            InitializeComponent();
            openFolderDialog = new FolderBrowserDialog();
            openFolderDialog.Description = "Select the directory that you want to use as the default.";

            webBrowser.NavigateError += new WebBrowserNavigateErrorEventHandler(wb_NavigateError);
            Controls.Add(webBrowser);

            // Do not allow the user to create new files via the FolderBrowserDialog.
            openFolderDialog.ShowNewFolderButton = false;

            // Default to the My Documents folder.
            openFolderDialog.RootFolder = Environment.SpecialFolder.Personal;

            getAccessToken = new Button()
            {
                Size = new Size(100, 20),
                Location = new Point(15, 45),
                Text = "Get token"
            };
            getAccessToken.Click += new EventHandler(getAccessToken_Click);
            Controls.Add(getAccessToken);

            accessTokenTxt = new TextBox()
            {
                Size = new Size(500, 20),
                Location = new Point(125, 45),
                Visible = false
            };
            Controls.Add(accessTokenTxt);

            selectFolderButton = new Button()
            {
                Size = new Size(100, 20),
                Location = new Point(15, 15),
                Text = "Select folder"
            };
            selectFolderButton.Click += new EventHandler(selectButton_Click);
            Controls.Add(selectFolderButton);

            folderNameTxt = new TextBox()
            {
                Size = new Size(500, 20),
                Location = new Point(125, 15)
            };
            Controls.Add(folderNameTxt);

            resultTxt = new TextBox()
            {
                Size = new Size(600, 600),
                Location = new Point(15, 80),
                Multiline = true
            };
            Controls.Add(resultTxt);

        }

        /// <summary>Web browser navigate error handler.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void wb_NavigateError(object sender, WebBrowserNavigateErrorEventArgs e)
        {
            // This will track errors: we want to track the 404 when the login
            // page redirects to our callback URL, let's check if is the error
            // we're tracking.
            Uri callbackURL = new Uri(e.Url);
            //if (e.Url.IndexOf(FORGE_CALLBACK_URL) == -1)
            //{
            //    MessageBox.Show("Sorry, the authorization failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}

            // extract the code
            var query = HttpUtility.ParseQueryString(callbackURL.Query);
            string code = query["code"];

            if (code != null)
            {

                accessTokenTxt.Text = await spotifyClient.GetAccessToken(code);
                webBrowser.Hide();
                MessageBox.Show("Access token successfully created.");
            }
            else
            {
                webBrowser.Navigate(e.Url);
            }
        }

        /// <summary>Getting access token button event handler.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void getAccessToken_Click(object sender, EventArgs e)
        {
            try
            {
                webBrowser.Navigate(new SpotifyClient().GetAuthorizationURL());
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }
        }

        /// <summary>Folder select click event handler method.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void selectButton_Click(object sender, EventArgs e)
        {
            DialogResult result = openFolderDialog.ShowDialog();
            resultTxt.Clear();
            if (result == DialogResult.OK)
            {
                var folderName = openFolderDialog.SelectedPath;
                folderNameTxt.Text = folderName;
                string access_token = accessTokenTxt.Text;
                Cursor = Cursors.WaitCursor;
                resultTxt.Text = "Loading data. Please wait...";
                await ProcessFolder(folderName, access_token);
                Cursor = Cursors.Arrow;
            }
        }

        /// <summary>Process selected folder.</summary>
        /// <param name="folderPath"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task ProcessFolder(string folderPath, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) throw new ArgumentNullException($"{nameof(folderPath)} is empty");
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException($"{nameof(accessToken)} is empty");
            try
            {
                string albumName = folderPath.Split('\\')[folderPath.Split('\\').Length - 1];
                string playlistId = await CreatePlaylist(albumName, accessToken);
                TrackInfo trackInfo = await GetTrackUriAndNames(folderPath, accessToken);
                bool isTracksAdded = await spotifyClient.AddTrackToPlaylist(playlistId, trackInfo.TrackUri, accessToken);
                if (isTracksAdded && trackInfo != null && !string.IsNullOrEmpty(trackInfo.TrackName))
                {
                    resultTxt.Clear();
                    resultTxt.Text = $"{albumName} album created successfully.{Environment.NewLine}";
                    resultTxt.Text += $"Tracks added:{ Environment.NewLine}";
                    foreach (string trackName in trackInfo.TrackName.Split(','))
                    {
                        resultTxt.Text += $"{trackName}{Environment.NewLine}";
                    }
                    logger.Information($"{albumName} album created successfully. Tracks added: {trackInfo.TrackName}");
                }

            }
            catch (Exception ex)
            {

                logger.Error(ex.Message);
                throw;
            }
        }

        /// <summary>Creates playlists</summary>
        /// <param name="albumName">Selected folder name as album name</param>
        /// <param name="accessToken">Access Token</param>
        /// <returns>Playlist ID</returns>
        private async Task<string> CreatePlaylist(string albumName, string accessToken)
        {
            return await spotifyClient.CreatePlayList(albumName, accessToken);
        }

        /// <summary>
        /// Gets track uris and names
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private async Task<TrackInfo> GetTrackUriAndNames(string folderPath, string accessToken)
        {
            Dictionary<string, string> trackInfoDict = await spotifyClient.GetTrackUri(folderPath, accessToken);
            TrackInfo albumRelatedTrackInfos = new TrackInfo();
            StringBuilder trackNameStrBuilder = new StringBuilder();
            StringBuilder trackUriBuilder = new StringBuilder();

            if (trackInfoDict.Count > 0)
            {
                foreach (KeyValuePair<string,string> kv in trackInfoDict)
                {
                    trackNameStrBuilder.Append(kv.Value + ',');
                    trackUriBuilder.Append(kv.Key + ',');
                }                
                albumRelatedTrackInfos.TrackName = trackNameStrBuilder.ToString();
                albumRelatedTrackInfos.TrackUri = trackUriBuilder.ToString();
                return albumRelatedTrackInfos;
            }
            else
            {
                logger.Error("There is a problem getting track uris or tracknames");
                throw new Exception("There is a problem getting track uris or tracknames");
            }
        }

    }
}

