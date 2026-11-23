using Rise.App.ViewModels;
using Rise.Common.Constants;
using Rise.Common.Extensions.Markup;
using Rise.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Rise.App.Services
{
    /// <summary>
    /// Fetches and caches remote artist artwork URLs.
    /// </summary>
    public sealed class ArtistArtService
    {
        private static readonly HttpClient SharedHttpClient = new(new HttpClientHandler
        {
            Proxy = null,
            UseProxy = false
        });

        public async Task PopulateArtistsArtAsync(IEnumerable<ArtistViewModel> artists, CancellationToken token = default)
        {
            if (artists == null || !WebHelpers.IsInternetAccessAvailable())
                return;

            foreach (var artist in artists.ToList())
            {
                if (token.IsCancellationRequested || !WebHelpers.IsInternetAccessAvailable())
                    return;

                // ms-appx URLs denote bundled placeholder artwork rather than user-set or already-fetched art.
                if (string.IsNullOrEmpty(artist.Picture) ||
                    !artist.Picture.StartsWith("ms-appx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string pic = await GetArtistImageAsync(artist.Name, token).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(pic))
                    artist.Picture = pic;
            }
        }

        public async Task<string> GetArtistImageAsync(string artist, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(artist) || artist == ResourceHelper.GetString("UnknownArtistResource"))
                return string.Empty;

            try
            {
                string url = $"{URLs.Deezer}/search/artist/?q={Uri.EscapeDataString(artist)}&output=xml";
                using var response = await SharedHttpClient.GetAsync(url, token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return string.Empty;

                string xmlStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlStr);

                var node = xmlDoc.DocumentElement?.SelectSingleNode("/root/data/artist/picture_medium");
                if (node != null)
                    return node.InnerText.Replace("<![CDATA[ ", string.Empty).Replace(" ]]>", string.Empty);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }

            return string.Empty;
        }
    }
}
