using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Nexa.Web.Services;

public static class VideoSources
{
    public const string LocalPrefix = "local:";
    public const string YoutubePrefix = "youtube:";

    public static string Kind(string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl)) return "empty";
        if (sourceUrl.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase)) return "local";
        if (sourceUrl.StartsWith(YoutubePrefix, StringComparison.OrdinalIgnoreCase)) return "youtube";
        if (TryParseYouTubeId(sourceUrl, out _)) return "youtube";
        return "unknown";
    }

    public static string Normalize(string? raw)
    {
        var value = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(value)) return "";

        if (value.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var file = value[LocalPrefix.Length..].Trim();
            if (string.IsNullOrEmpty(file) || file.Contains("..") || file.Contains('/') || file.Contains('\\'))
                throw new InvalidOperationException("Nombre de archivo local inválido.");
            return LocalPrefix + file;
        }

        if (TryParseYouTubeId(value, out var id))
            return YoutubePrefix + id;

        throw new InvalidOperationException(
            "Fuente inválida. Usá un link de YouTube (watch, youtu.be o youtube.com/embed) o un video local subido.");
    }

    public static string? YouTubeId(string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl)) return null;
        if (sourceUrl.StartsWith(YoutubePrefix, StringComparison.OrdinalIgnoreCase))
            return sourceUrl[YoutubePrefix.Length..].Trim();
        return TryParseYouTubeId(sourceUrl, out var id) ? id : null;
    }

    public static string? YouTubeEmbedUrl(string? sourceUrl)
    {
        var id = YouTubeId(sourceUrl);
        return string.IsNullOrEmpty(id) ? null : $"https://www.youtube.com/embed/{id}?rel=0";
    }

    public static string? LocalFileName(string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl)) return null;
        if (!sourceUrl.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase)) return null;
        return sourceUrl[LocalPrefix.Length..];
    }

    public static bool TryParseYouTubeId(string input, out string videoId)
    {
        videoId = "";
        var value = input.Trim();
        if (value.StartsWith(YoutubePrefix, StringComparison.OrdinalIgnoreCase))
            value = value[YoutubePrefix.Length..].Trim();

        // Already a bare ID
        if (Regex.IsMatch(value, @"^[A-Za-z0-9_-]{11}$"))
        {
            videoId = value;
            return true;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
        if (host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var id = uri.AbsolutePath.Trim('/');
            if (Regex.IsMatch(id, @"^[A-Za-z0-9_-]{11}$"))
            {
                videoId = id;
                return true;
            }
        }

        if (host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            var query = QueryHelpers.ParseQuery(uri.Query);
            var v = query.TryGetValue("v", out var vv) ? vv.ToString() : null;
            if (!string.IsNullOrEmpty(v) && Regex.IsMatch(v, @"^[A-Za-z0-9_-]{11}$"))
            {
                videoId = v;
                return true;
            }

            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // /embed/ID /shorts/ID /live/ID
            if (parts.Length >= 2 &&
                (parts[0] is "embed" or "shorts" or "live") &&
                Regex.IsMatch(parts[1], @"^[A-Za-z0-9_-]{11}$"))
            {
                videoId = parts[1];
                return true;
            }
        }

        return false;
    }
}
