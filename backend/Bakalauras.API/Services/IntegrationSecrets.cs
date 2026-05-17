using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class IntegrationSecrets
{
    // Injected once at startup via IntegrationSecrets.Configure(key)
    private static byte[] _key = Array.Empty<byte>();

    public static void Configure(string base64OrPlainKey)
    {
        // Accept either a plain 32-char string or a base64-encoded 32-byte key
        byte[] raw;
        try { raw = Convert.FromBase64String(base64OrPlainKey); }
        catch { raw = Encoding.UTF8.GetBytes(base64OrPlainKey); }

        if (raw.Length != 32)
            throw new InvalidOperationException(
                $"Encryption key must be 32 bytes (256-bit). Got {raw.Length} bytes.");

        _key = raw;
    }

    // -------------------------------------------------------
    // Pack = serialize to JSON, then AES-256-GCM encrypt
    // Output format: base64(nonce[12] + ciphertext + tag[16])
    // -------------------------------------------------------
    public static string Pack(string username, string password, string? baseUrl)
    {
        EnsureKeyConfigured();

        var payload = JsonSerializer.Serialize(new
        {
            username = username?.Trim(),
            password = password ?? "",
            baseUrl = baseUrl?.Trim()
        });

        var plainBytes = Encoding.UTF8.GetBytes(payload);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize); // tag = 16 bytes

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];   // 16 bytes

        RandomNumberGenerator.Fill(nonce);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // Concatenate: nonce(12) + ciphertext(n) + tag(16)
        var combined = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(combined);
    }

    // -------------------------------------------------------
    // Unpack = base64 decode, AES-256-GCM decrypt, JSON parse
    // -------------------------------------------------------
    public static (string? Username, string? Password, string? BaseUrl) TryUnpack(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return (null, null, null);

        EnsureKeyConfigured();

        try
        {
            var combined = Convert.FromBase64String(stored);
            const int nonceLen = 12;
            const int tagLen = 16;

            if (combined.Length < nonceLen + tagLen)
                throw new CryptographicException("Payload too short.");

            var nonce = combined[..nonceLen];
            var tag = combined[^tagLen..];
            var ciphertext = combined[nonceLen..^tagLen];
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(plaintext));
            var root = doc.RootElement;

            var u = root.TryGetProperty("username", out var uEl) ? uEl.GetString() : null;
            var p = root.TryGetProperty("password", out var pEl) ? pEl.GetString() : null;
            var b = root.TryGetProperty("baseUrl", out var bEl) ? bEl.GetString() : null;

            return (u, p, b);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IntegrationSecrets] TryUnpack failed: {ex.Message}");
            return (null, null, null);
        }
    }

    private static void EnsureKeyConfigured()
    {
        if (_key.Length == 0)
            throw new InvalidOperationException(
                "IntegrationSecrets has not been configured. Call IntegrationSecrets.Configure() at startup.");
    }
}