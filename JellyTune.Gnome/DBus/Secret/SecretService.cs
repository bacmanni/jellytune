using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Tmds.DBus;

namespace JellyTune.Gnome.DBus.Secret;

public class SecretService : ISecurityService
{
    private readonly ApplicationInfo _applicationInfo;
    private readonly Connection _connection = Connection.Session;

    private BigInteger PrivateKey { get; set; }
    private BigInteger PublicKey { get; set; }
    private byte[] ServerPublicKey { get; set; }
    private ObjectPath ServerSessionPath { get; set; }
    
    // IETF 1024-bit MODP group (Oakley Group 2)
    private static readonly byte[] OakleyGroup2PBytes = Convert.FromHexString(
        "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1" +
        "29024E088A67CC74020BBEA63B139B22514A08798E3404DD" +
        "EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245" +
        "E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED" +
        "EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE65381" +
        "FFFFFFFFFFFFFFFF");

    private static readonly BigInteger P = new(OakleyGroup2PBytes, isUnsigned: true, isBigEndian: true);
    private static readonly BigInteger G = new(2);
    
    public SecretService(ApplicationInfo applicationInfo)
    {
        _applicationInfo = applicationInfo;
    }
    
    private byte[] HKDF_SHA256(byte[] ikm, byte[] salt, byte[] info, int outputLength)
    {
        salt ??= new byte[32]; // NULL salt → 32 zero bytes for SHA-256
        using var hmac = new HMACSHA256(salt);

        // Extract
        var prk = hmac.ComputeHash(ikm);

        // Expand
        var okm = new byte[outputLength];
        var previous = Array.Empty<byte>();
        var offset = 0;
        byte counter = 1;

        while (offset < outputLength)
        {
            hmac.Key = prk;

            var input = new byte[previous.Length + info.Length + 1];
            Buffer.BlockCopy(previous, 0, input, 0, previous.Length);
            Buffer.BlockCopy(info, 0, input, previous.Length, info.Length);
            input[input.Length - 1] = counter;

            previous = hmac.ComputeHash(input);

            var toCopy = Math.Min(previous.Length, outputLength - offset);
            Buffer.BlockCopy(previous, 0, okm, offset, toCopy);

            offset += toCopy;
            counter++;
        }

        return okm;
    }

    private (BigInteger PublicKey, BigInteger PrivateKey) GenerateKey()
    {
        // Generate private key x in [2, p-2]
        var privateKeyBytes = new byte[128];
        RandomNumberGenerator.Fill(privateKeyBytes);
        var privateKey = new BigInteger(privateKeyBytes, isUnsigned: true, isBigEndian: true);
        privateKey %= (P - 2);
        privateKey += 2;

        // Compute public key: g^x mod p
        var publicKey = BigInteger.ModPow(G, privateKey, P);

        // Convert public key to 128-byte big-endian
        var publicKeyBytes = publicKey.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (publicKeyBytes.Length < 128)
        {
            var padded = new byte[128];
            Buffer.BlockCopy(publicKeyBytes, 0, padded, 128 - publicKeyBytes.Length, publicKeyBytes.Length);
            publicKeyBytes = padded;
        }

        //Console.WriteLine("Public Key (hex): " + BitConverter.ToString(publicKeyBytes).Replace("-", ""));
        return (PublicKey: publicKey, PrivateKey: privateKey);
    }

    private Secret Encrypt(
        ObjectPath sessionPath,
        BigInteger clientPrivateKey,
        byte[] serverPublicKeyBytes,
        string plaintext)
    {
        var peerPublicKey = new BigInteger(serverPublicKeyBytes, isUnsigned: true, isBigEndian: true);

        // Compute shared secret: (peerPublicKey ^ privateKey) mod p
        var sharedSecret = BigInteger.ModPow(peerPublicKey, clientPrivateKey, P);
        var sharedSecretBytes = sharedSecret.ToByteArray(isUnsigned: true, isBigEndian: true);

        // Optional: pad shared secret to 128 bytes
        if (sharedSecretBytes.Length < 128)
        {
            var padded = new byte[128];
            Buffer.BlockCopy(sharedSecretBytes, 0, padded, 128 - sharedSecretBytes.Length, sharedSecretBytes.Length);
            sharedSecretBytes = padded;
        }

        //Console.WriteLine("Shared secret:  " + BitConverter.ToString(sharedSecretBytes).Replace("-", ""));

        // HKDF-SHA256 → 128-bit AES key
        var aesKey = HKDF_SHA256(sharedSecretBytes, salt: new byte[32], info: Array.Empty<byte>(), outputLength: 16);
        //Console.WriteLine("AES Key (hex): " + BitConverter.ToString(aesKey).Replace("-", ""));

        // AES-CBC with PKCS7
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        byte[] ciphertext;
        using (var encryptor = aes.CreateEncryptor())
        {
            ciphertext = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
        }

        //Console.WriteLine("IV:  " + BitConverter.ToString(aes.IV).Replace("-", ""));
        //Console.WriteLine("CT:  " + BitConverter.ToString(ciphertext).Replace("-", ""));

        return new Secret(sessionPath, aes.IV, ciphertext);
    }

    private string Decrypt(
        BigInteger clientPrivateKey,
        byte[] serverPublicKeyBytes,
        byte[] iv,
        byte[] ciphertext)
    {
        // Convert server public key to BigInteger
        var peerPublicKey = new BigInteger(serverPublicKeyBytes, isUnsigned: true, isBigEndian: true);

        // Compute shared secret: (serverPubKey ^ clientPrivKey) mod p
        var sharedSecret = BigInteger.ModPow(peerPublicKey, clientPrivateKey, P);
        var sharedSecretBytes = sharedSecret.ToByteArray(isUnsigned: true, isBigEndian: true);

        // Pad shared secret to 128 bytes (same as Encrypt)
        if (sharedSecretBytes.Length < 128)
        {
            var padded = new byte[128];
            Buffer.BlockCopy(sharedSecretBytes, 0, padded, 128 - sharedSecretBytes.Length, sharedSecretBytes.Length);
            sharedSecretBytes = padded;
        }

        //Console.WriteLine("Shared secret (dec): " + BitConverter.ToString(sharedSecretBytes).Replace("-", ""));

        // --- HKDF-SHA256 → 128-bit AES key (MUST MATCH ENCRYPT) ---
        var aesKey = HKDF_SHA256(
            sharedSecretBytes,
            salt: new byte[32],               // same as encrypt
            info: Array.Empty<byte>(),
            outputLength: 16);

        //Console.WriteLine("AES Key (dec): " + BitConverter.ToString(aesKey).Replace("-", ""));

        // --- AES-CBC decryption with PKCS7 ---
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] plaintextBytes;
        using (var decryptor = aes.CreateDecryptor())
        {
            plaintextBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        return Encoding.UTF8.GetString(plaintextBytes);
    }
    
    public async Task OpenSessionAsync()
    {
        var service = _connection.CreateProxy<ISecretService>(
            "org.freedesktop.secrets",
            new ObjectPath("/org/freedesktop/secrets")
        );

        // Generate keys and open session
        var key = GenerateKey();
        PrivateKey = key.PrivateKey;
        PublicKey = key.PublicKey;
        
        var publicKeyBytes = key.PublicKey.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (publicKeyBytes.Length < 128)
        {
            var padded = new byte[128];
            Buffer.BlockCopy(publicKeyBytes, 0, padded, 128 - publicKeyBytes.Length, publicKeyBytes.Length);
            publicKeyBytes = padded;
        }

        var sessionResult = await service.OpenSessionAsync(
            "dh-ietf1024-sha256-aes128-cbc-pkcs7",
            publicKeyBytes);
        
        var serverPublicKeyBytes = (byte[])sessionResult.Session;
        
        // Ensure server public key is 128 bytes
        if (serverPublicKeyBytes.Length < 128)
        {
            var padded = new byte[128];
            Buffer.BlockCopy(serverPublicKeyBytes, 0, padded, 128 - serverPublicKeyBytes.Length, serverPublicKeyBytes.Length);
            serverPublicKeyBytes = padded;
        }
        
        ServerSessionPath = sessionResult.Path;
        ServerPublicKey = serverPublicKeyBytes;
    }

    private async Task<ISecretItem?> GetSecretItemAsync()
    {
        var collection = _connection.CreateProxy<ISecretCollection>(
            "org.freedesktop.secrets",
            new ObjectPath("/org/freedesktop/secrets/collection/login")
        );

        var isLocked = await collection.GetAsync<bool>("Locked");
        if (isLocked) return null;

        var items = await collection.GetAsync<ObjectPath[]>("Items");
        foreach (var item in items)
        {
            var secretItem = _connection.CreateProxy<ISecretItem>(
                "org.freedesktop.secrets",
                item
            );

            var attributes = await secretItem.GetAsync<IDictionary<string, string>>("Attributes");
            if (attributes.TryGetValue("app_id", out var applicationId) && applicationId == _applicationInfo.Id)
            {
                return secretItem;
            }
        }

        return null;
    }

    private async Task SetSecretItemAsync(string password)
    {
        var collection = _connection.CreateProxy<ISecretCollection>(
            "org.freedesktop.secrets",
            new ObjectPath("/org/freedesktop/secrets/collection/login")
        );

        var isLocked = await collection.GetAsync<bool>("Locked");
        if (isLocked) return;
        
        // No existing item → create new
        var properties = new Dictionary<string, object>()
        {
            {
                "org.freedesktop.Secret.Item.Attributes", new Dictionary<string, string>()
                {
                    { "app_id", _applicationInfo.Id },
                }
            },
            { "org.freedesktop.Secret.Item.Label", $"{_applicationInfo.Name} Secret" },
        };

        var secret = Encrypt(ServerSessionPath, PrivateKey, ServerPublicKey, password);
        var createdPath = await collection.CreateItemAsync(properties, secret, true);
    }
    
    /// <summary>
    /// Store password to keyring
    /// </summary>
    /// <param name="password"></param>
    public async Task SetPasswordAsync(string? password)
    {
        var item = await GetSecretItemAsync();
        if (item != null)
        {
            await item.DeleteAsync();    
        }
        
        await SetSecretItemAsync(password ?? "");
    }

    /// <summary>
    /// Get stored password
    /// </summary>
    /// <returns></returns>
    public async Task<string?> GetPasswordAsync()
    {
        var item = await GetSecretItemAsync();
        if (item == null) return null;

        var secret = await item.GetSecretAsync(ServerSessionPath);
        return Decrypt(PrivateKey, ServerPublicKey, secret.Parameters, secret.Value);
    }

    public void Dispose()
    {
        // nothing yet
    }
}
