using Rise.Common.Constants;
using Rise.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Media.Playback;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;
using Windows.Web.Http;

namespace Rise.Data.ViewModels
{
    public sealed partial class LastFMViewModel : ViewModel
    {
        private readonly string _key;
        private readonly string _secret;
        private string _vaultResource;

        /// <summary>
        /// Initializes a new instance of this class with the provided
        /// API keys.
        /// </summary>
        /// <param name="key">last.fm API key.</param>
        /// <param name="secret">last.fm secret key.</param>
        public LastFMViewModel(string key, string secret)
        {
            _key = key;
            _secret = secret;
        }

        private bool _authenticated = false;
        /// <summary>
        /// Whether the user has been authenticated.
        /// </summary>
        public bool Authenticated
        {
            get => _authenticated;
            private set => Set(ref _authenticated, value);
        }

        /// <summary>
        /// Session key used for the LastFM API.
        /// </summary>
        private string _sessionKey;

        private string _username;
        /// <summary>
        /// Username for the currently logged in user.
        /// </summary>
        public string Username
        {
            get => _username;
            private set => Set(ref _username, value);
        }

        /// <summary>
        /// Attempts to authenticate the user to last.fm.
        /// </summary>
        /// <returns>true if the authentication is successful,
        /// false otherwise.</returns>
        public async Task<bool> TryAuthenticateAsync()
        {
            string token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var uriBuilder = new StringBuilder()
                .Append("https://www.last.fm/api/auth?api_key=")
                .Append(Uri.EscapeDataString(_key))
                .Append("&token=")
                .Append(Uri.EscapeDataString(token))
                .Append("&redirect_uri=")
                .Append(Uri.EscapeDataString("https://www.google.com"));

            var startUri = new Uri(uriBuilder.ToString());
            Dictionary<string, string> args = new()
            {
                { "method", "auth.getSession" },
                { "api_key", _key },
                { "token", token }
            };

            var endUri = GetSignedUri(args);

            WebAuthenticationResult result;
            try
            {
                result = await WebAuthenticationBroker.
                    AuthenticateAsync(WebAuthenticationOptions.UseTitle, startUri, endUri);
            }
            catch (FileNotFoundException)
            {
                // FileNotFound generally means no access to the host
                return false;
            }

            if (result.ResponseStatus != WebAuthenticationStatus.Success)
                return false;

            string response;
            using (var client = new HttpClient())
            {
                try
                {
                    var httpResponse = await client.GetAsync(endUri);
                    if (!httpResponse.IsSuccessStatusCode)
                        return false;

                    response = await httpResponse.Content.ReadAsStringAsync();
                }
                catch (Exception)
                {
                    return false;
                }
            }

            if (!TryLoadXml(response, out var doc))
                return false;

            string sessionKey = GetNodeFromResponse(doc, "/lfm/session/key");
            string username = GetNodeFromResponse(doc, "/lfm/session/name");
            if (string.IsNullOrEmpty(sessionKey) || string.IsNullOrEmpty(username))
                return false;

            _sessionKey = sessionKey;
            Username = username;
            Authenticated = true;
            return true;
        }

        /// <summary>
        /// Saves the username and session key to the vault.
        /// </summary>
        public void SaveCredentialsToVault(string resource)
        {
            if (!_authenticated || string.IsNullOrEmpty(resource))
                return;

            _vaultResource = resource;
            var vault = new PasswordVault();
            RemoveCredentialsForResource(vault, resource);
            vault.Add(new PasswordCredential(resource, Username, _sessionKey));
        }

        /// <summary>
        /// Attempts to load credentials from the vault.
        /// </summary>
        /// <returns>true if the credentials were loaded successfully,
        /// false otherwise.</returns>
        public bool TryLoadCredentials(string resource)
        {
            try
            {
                _vaultResource = resource;
                var vault = new PasswordVault();
                var credential = vault.RetrieveAll().LastOrDefault(p => p.Resource == resource);
                if (credential != null)
                {
                    credential.RetrievePassword();
                    Username = credential.UserName;
                    _sessionKey = credential.Password;
                }

                Authenticated = !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(_sessionKey);
                return Authenticated;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to scrobble the provided <see cref="MediaPlaybackItem"/>.
        /// </summary>
        /// <returns>true if the item was successfully scrobbled,
        /// false otherwise.</returns>
        public async Task<bool> TryScrobbleItemAsync(MediaPlaybackItem item)
        {
            if (!_authenticated || item == null)
                return false;

            string curr = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var props = item.GetDisplayProperties().MusicProperties;
            string title = props.Title ?? string.Empty;
            string artist = props.Artist ?? string.Empty;

            Dictionary<string, string> parameters = new()
            {
                { "artist[0]", artist },
                { "track[0]", title },
                { "timestamp[0]", curr },
                { "method", "track.scrobble" },
                { "api_key", _key },
                { "sk", _sessionKey }
            };

            var uri = GetSignedUri(parameters);
            using var content = new HttpStringContent(string.Empty);
            using var client = new HttpClient();
            try
            {
                var response = await client.PostAsync(uri, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Logs out of the current session, clearing
        /// the username, session key, and any cached vault credentials.
        /// </summary>
        public void LogOut()
        {
            Authenticated = false;
            Username = null;
            _sessionKey = null;

            if (!string.IsNullOrEmpty(_vaultResource))
            {
                try
                {
                    RemoveCredentialsForResource(new PasswordVault(), _vaultResource);
                }
                catch (Exception) { }
            }
        }
    }

    // Private methods
    public partial class LastFMViewModel
    {
        private static void RemoveCredentialsForResource(PasswordVault vault, string resource)
        {
            foreach (var existing in vault.RetrieveAll().Where(p => p.Resource == resource).ToList())
                vault.Remove(existing);
        }

        private async Task<string> GetTokenAsync()
        {
            string url = $"{URLs.LastFM}auth.gettoken&api_key={Uri.EscapeDataString(_key)}";

            string response;
            using (var client = new HttpClient())
            {
                try
                {
                    var httpResponse = await client.GetAsync(new Uri(url));
                    if (!httpResponse.IsSuccessStatusCode)
                        return null;

                    response = await httpResponse.Content.ReadAsStringAsync();
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return TryLoadXml(response, out var doc)
                ? GetNodeFromResponse(doc, "/lfm/token")
                : null;
        }

        private Uri GetSignedUri(Dictionary<string, string> args)
        {
            var stringBuilder = new StringBuilder("https://ws.audioscrobbler.com/2.0/?");

            foreach (var kvp in args)
            {
                _ = stringBuilder
                    .Append(Uri.EscapeDataString(kvp.Key))
                    .Append('=')
                    .Append(Uri.EscapeDataString(kvp.Value ?? string.Empty))
                    .Append('&');
            }

            _ = stringBuilder
                .Append("api_sig=")
                .Append(GetSignature(args));

            return new Uri(stringBuilder.ToString());
        }

        private string GetSignature(Dictionary<string, string> parameters)
        {
            var resultBuilder = new StringBuilder();
            foreach (var kvp in parameters.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                _ = resultBuilder.Append(kvp.Key);
                _ = resultBuilder.Append(kvp.Value);
            }

            _ = resultBuilder.Append(_secret);
            return resultBuilder.ToString().GetEncodedHash("MD5");
        }

        private static bool TryLoadXml(string xml, out XmlDocument doc)
        {
            doc = null;
            if (string.IsNullOrWhiteSpace(xml))
                return false;

            try
            {
                doc = new XmlDocument();
                doc.LoadXml(xml);
                return true;
            }
            catch (XmlException)
            {
                doc = null;
                return false;
            }
        }

        private static string GetNodeFromResponse(XmlDocument doc, string node)
            => doc?.DocumentElement?.SelectSingleNode(node)?.InnerText;
    }
}
