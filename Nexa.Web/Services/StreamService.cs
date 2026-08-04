using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Nexa.Web.Services;

public class StreamService
{
    private readonly IDataProtector _protector;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public StreamService(IDataProtectionProvider dataProtection, IConfiguration config, IWebHostEnvironment env)
    {
        _protector = dataProtection.CreateProtector("nexa.stream.v1");
        _config = config;
        _env = env;
    }

    public (byte[] Key, byte[] Iv) GenerateContentKey() =>
        (RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(16));

    public string CreateStreamToken(string userId, string courseId, string lessonId, byte[] key, byte[] iv)
    {
        var payload = JsonSerializer.Serialize(new StreamTokenPayload
        {
            Sub = userId,
            CourseId = courseId,
            LessonId = lessonId,
            KeyB64 = Convert.ToBase64String(key),
            IvB64 = Convert.ToBase64String(iv),
            Exp = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
        });
        return _protector.Protect(payload);
    }

    public StreamClaims? VerifyStreamToken(string token)
    {
        try
        {
            var json = _protector.Unprotect(token);
            var payload = JsonSerializer.Deserialize<StreamTokenPayload>(json);
            if (payload == null || payload.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return null;
            if (string.IsNullOrEmpty(payload.Sub) || string.IsNullOrEmpty(payload.CourseId) ||
                string.IsNullOrEmpty(payload.LessonId) || string.IsNullOrEmpty(payload.KeyB64) ||
                string.IsNullOrEmpty(payload.IvB64))
                return null;
            return new StreamClaims(payload.Sub, payload.CourseId, payload.LessonId, payload.KeyB64, payload.IvB64);
        }
        catch
        {
            return null;
        }
    }

    public static byte[] EncryptChunk(byte[] plain, byte[] key, byte[] iv, long byteOffset)
    {
        var counter = (byte[])iv.Clone();
        var blockIndex = byteOffset / 16;
        for (var i = 15; i >= 0 && blockIndex > 0; i--)
        {
            var sum = counter[i] + (int)(blockIndex & 0xff);
            counter[i] = (byte)(sum & 0xff);
            blockIndex = (blockIndex >> 8) + (sum > 0xff ? 1 : 0);
        }

        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;

        var prefix = (int)(byteOffset % 16);
        var input = prefix == 0 ? plain : PadLeft(plain, prefix);
        using var encryptor = aes.CreateEncryptor();

        var output = new byte[input.Length];
        var block = new byte[16];
        var keystream = new byte[16];
        for (var offset = 0; offset < input.Length; offset += 16)
        {
            Buffer.BlockCopy(counter, 0, block, 0, 16);
            encryptor.TransformBlock(block, 0, 16, keystream, 0);
            var len = Math.Min(16, input.Length - offset);
            for (var j = 0; j < len; j++)
                output[offset + j] = (byte)(input[offset + j] ^ keystream[j]);
            IncrementCounter(counter);
        }

        return prefix == 0 ? output : output[prefix..];
    }

    private static byte[] PadLeft(byte[] plain, int prefix)
    {
        var padded = new byte[prefix + plain.Length];
        Buffer.BlockCopy(plain, 0, padded, prefix, plain.Length);
        return padded;
    }

    private static void IncrementCounter(byte[] counter)
    {
        for (var i = 15; i >= 0; i--)
        {
            if (++counter[i] != 0) break;
        }
    }

    public static string WatermarkFingerprint(string userId, string email)
    {
        var bytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes("nexa-wm"), Encoding.UTF8.GetBytes($"{userId}:{email}"));
        return Convert.ToHexString(bytes)[..12].ToUpperInvariant();
    }

    public string GetVideoRoot()
    {
        var solutionRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
        var configured = _config["VideoPath"];
        var videoRoot = !string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(_env.ContentRootPath, configured))
            : Path.Combine(solutionRoot, "content", "videos");
        Directory.CreateDirectory(videoRoot);
        return videoRoot;
    }

    public async Task<string> SaveUploadedVideoAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Archivo vacío.");
        if (file.Length > 500L * 1024 * 1024)
            throw new InvalidOperationException("El video no puede superar 500 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".mp4" or ".webm" or ".mov"))
            throw new InvalidOperationException("Solo se permiten .mp4, .webm o .mov.");

        var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(GetVideoRoot(), safeName);
        await using (var fs = File.Create(full))
            await file.CopyToAsync(fs);

        return VideoSources.LocalPrefix + safeName;
    }

    public async Task<VideoSource> ReadVideoSourceAsync(string sourceUrl, string? rangeHeader)
    {
        var fileName = VideoSources.LocalFileName(sourceUrl)
            ?? throw new InvalidOperationException("ONLY_LOCAL_SUPPORTED");
        if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            throw new InvalidOperationException("INVALID_LOCAL_SOURCE");

        var videoRoot = GetVideoRoot();
        var full = Path.Combine(videoRoot, fileName);
        var file = await File.ReadAllBytesAsync(full);
        var totalSize = file.Length;

        if (!string.IsNullOrEmpty(rangeHeader))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                rangeHeader, @"bytes=(\d+)-(\d*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var start = long.Parse(match.Groups[1].Value);
                var end = string.IsNullOrEmpty(match.Groups[2].Value) ? totalSize - 1 : long.Parse(match.Groups[2].Value);
                var slice = file.AsSpan((int)start, (int)(end - start + 1)).ToArray();
                return new VideoSource(slice, 206, $"bytes {start}-{end}/{totalSize}", totalSize, start);
            }
        }

        return new VideoSource(file, 200, null, totalSize, 0);
    }

    private sealed class StreamTokenPayload
    {
        public string Sub { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string LessonId { get; set; } = "";
        public string KeyB64 { get; set; } = "";
        public string IvB64 { get; set; } = "";
        public long Exp { get; set; }
    }
}

public record StreamClaims(string Sub, string CourseId, string LessonId, string KeyB64, string IvB64);
public record VideoSource(byte[] Body, int Status, string? ContentRange, long TotalSize, long Start);
