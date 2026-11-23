using Rise.Models;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Rise.App.Helpers
{
    // Thanks to @ahmed605 (https://github.com/ahmed605) for the Musixmatch code contribution.
    public static class MusixmatchHelper
    {
        private const string BaseUrl = "https://apic-desktop.musixmatch.com/ws/1.1/";
        private const string DefaultUserToken = "2306258e8a658a197b52f987c6f83479b4cce70202a27357e58b68";
        private static readonly HttpClient _httpClient = new();

        private static async Task<string> GetStringAsync(Uri url)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                _ = response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private static async Task<T> FetchAsync<T>(string relativeQuery, Func<string, T> parser)
            where T : class
        {
            string response = await GetStringAsync(new Uri(BaseUrl + relativeQuery));
            return !string.IsNullOrWhiteSpace(response) ? parser(response) : null;
        }

        private static string GetUserToken(UserToken token)
        {
            var message = token?.Message;
            string userToken = message?.Body?.Token;

            if (message?.Header?.StatusCode == 200 && !string.IsNullOrEmpty(userToken))
                return userToken;

            return DefaultUserToken;
        }

        private static string BuildMatcherQuery(string endpoint, string trackName, string artistName, int? duration, string subtitleFormat, UserToken token)
        {
            string userToken = Uri.EscapeDataString(GetUserToken(token));
            string track = Uri.EscapeDataString(trackName ?? string.Empty);
            string artist = Uri.EscapeDataString(artistName ?? string.Empty);
            string durationPart = duration.HasValue ? $"&q_duration={duration.Value}" : string.Empty;

            return $"{endpoint}?format=json&q_track={track}&q_artist={artist}{durationPart}&user_language=en&subtitle_format={subtitleFormat}&app_id=web-desktop-app-v1.0&usertoken={userToken}";
        }

        private static string BuildTrackIdQuery(string endpoint, string id, string subtitleFormat, UserToken token)
        {
            string userToken = Uri.EscapeDataString(GetUserToken(token));
            string escapedId = Uri.EscapeDataString(id ?? string.Empty);

            return $"{endpoint}?format=json&track_id={escapedId}&user_language=en&subtitle_format={subtitleFormat}&app_id=web-desktop-app-v1.0&usertoken={userToken}";
        }

        public static Task<MusixmatchLyrics> GetLyricsAsync(string trackName, string artistName, UserToken token = null)
        {
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(artistName))
                return Task.FromResult<MusixmatchLyrics>(null);

            return FetchAsync(BuildMatcherQuery("matcher.lyrics.get", trackName, artistName, null, "lrc", token), MusixmatchLyrics.FromJson);
        }

        // Duration is in seconds
        public static Task<MusixmatchLyrics> GetLyricsAsync(string trackName, string artistName, int duration, UserToken token = null)
        {
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(artistName))
                return Task.FromResult<MusixmatchLyrics>(null);

            return FetchAsync(BuildMatcherQuery("matcher.lyrics.get", trackName, artistName, duration, "mxm", token), MusixmatchLyrics.FromJson);
        }

        public static Task<MusixmatchLyrics> GetLyricsAsync(string id, UserToken token = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.FromResult<MusixmatchLyrics>(null);

            return FetchAsync(BuildTrackIdQuery("track.lyrics.get", id, "mxm", token), MusixmatchLyrics.FromJson);
        }

        public static Task<SyncedLyrics> GetSyncedLyricsAsync(string trackName, string artistName, UserToken token = null)
        {
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(artistName))
                return Task.FromResult<SyncedLyrics>(null);

            return FetchAsync(BuildMatcherQuery("matcher.subtitle.get", trackName, artistName, null, "mxm", token), SyncedLyrics.FromJson);
        }

        // Duration is in seconds
        public static Task<SyncedLyrics> GetSyncedLyricsAsync(string trackName, string artistName, int duration, UserToken token = null)
        {
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(artistName))
                return Task.FromResult<SyncedLyrics>(null);

            return FetchAsync(BuildMatcherQuery("matcher.subtitle.get", trackName, artistName, duration, "mxm", token), SyncedLyrics.FromJson);
        }

        public static Task<SyncedLyrics> GetSyncedLyricsAsync(string id, UserToken token = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.FromResult<SyncedLyrics>(null);

            return FetchAsync(BuildTrackIdQuery("track.subtitle.get", id, "mxm", token), SyncedLyrics.FromJson);
        }
    }
}
