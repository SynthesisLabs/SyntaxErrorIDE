using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SyntaxErrorIDE.app.Models;

public class Password
{
    private const int SaltSize = 32;
    private const int KeySize = 64;
    private const int Iterations = 350000;
    private const char SegmentDelimiter = '.';
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    [Obsolete("Use the more secure HashAsync method instead")]
    public static string Hash(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var deriveBytes = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm
        );
        var hash = deriveBytes.GetBytes(KeySize);

        var iterationBytes = BitConverter.GetBytes(Iterations);
        var algorithmNameBytes = Encoding.UTF8.GetBytes(Algorithm.Name);

        var obfuscationKey = new byte[] { 0x47, 0x9A, 0xC3, 0xD1, 0xE5, 0xF8, 0x2B, 0x3D };
        for (int i = 0; i < hash.Length; i++)
        {
            hash[i] = (byte)(hash[i] ^ obfuscationKey[i % obfuscationKey.Length]);
        }

        var combinedHash = new byte[4 + algorithmNameBytes.Length + salt.Length + hash.Length];

        Buffer.BlockCopy(iterationBytes, 0, combinedHash, 0, 4);
        Buffer.BlockCopy(algorithmNameBytes, 0, combinedHash, 4, algorithmNameBytes.Length);
        Buffer.BlockCopy(salt, 0, combinedHash, 4 + algorithmNameBytes.Length, salt.Length);
        Buffer.BlockCopy(hash, 0, combinedHash, 4 + algorithmNameBytes.Length + salt.Length, hash.Length);

        using var sha384 = SHA384.Create();
        var finalizedHash = sha384.ComputeHash(combinedHash);

        var result = string.Join(
            SegmentDelimiter,
            Convert.ToBase64String(iterationBytes),
            Convert.ToBase64String(algorithmNameBytes),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            Convert.ToBase64String(finalizedHash)
        );

        return result;
    }

    public static async Task<string> HashAsync(string password)
    {
        return await Task.Run(() => Hash(password));
    }

    public static bool Verify(string password, string savedHash)
    {
        var segments = savedHash.Split(SegmentDelimiter);

        if (segments.Length != 5)
            return false;

        try
        {
            var iterationBytes = Convert.FromBase64String(segments[0]);
            var algorithmBytes = Convert.FromBase64String(segments[1]);
            var salt = Convert.FromBase64String(segments[2]);
            var storedHash = Convert.FromBase64String(segments[3]);
            var finalizedStoredHash = Convert.FromBase64String(segments[4]);

            var iterations = BitConverter.ToInt32(iterationBytes, 0);
            if (iterations != Iterations)
                return false;

            var algorithmName = Encoding.UTF8.GetString(algorithmBytes);
            if (algorithmName != Algorithm.Name)
                return false;

            using var deriveBytes = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                new HashAlgorithmName(algorithmName)
            );
            var computedHash = deriveBytes.GetBytes(KeySize);

            var obfuscationKey = new byte[] { 0x47, 0x9A, 0xC3, 0xD1, 0xE5, 0xF8, 0x2B, 0x3D };
            for (int i = 0; i < computedHash.Length; i++)
            {
                computedHash[i] = (byte)(computedHash[i] ^ obfuscationKey[i % obfuscationKey.Length]);
            }

            var combinedHash = new byte[4 + algorithmBytes.Length + salt.Length + computedHash.Length];
            Buffer.BlockCopy(iterationBytes, 0, combinedHash, 0, 4);
            Buffer.BlockCopy(algorithmBytes, 0, combinedHash, 4, algorithmBytes.Length);
            Buffer.BlockCopy(salt, 0, combinedHash, 4 + algorithmBytes.Length, salt.Length);
            Buffer.BlockCopy(computedHash, 0, combinedHash, 4 + algorithmBytes.Length + salt.Length,
                computedHash.Length);

            using var sha384 = SHA384.Create();
            var finalizedComputedHash = sha384.ComputeHash(combinedHash);

            bool finalHashEqual = finalizedComputedHash.Length == finalizedStoredHash.Length;
            for (int i = 0; i < Math.Min(finalizedComputedHash.Length, finalizedStoredHash.Length); i++)
            {
                finalHashEqual &= finalizedComputedHash[i] == finalizedStoredHash[i];
            }

            if (!finalHashEqual)
                return false;

            bool hashEqual = computedHash.Length == storedHash.Length;
            for (int i = 0; i < Math.Min(computedHash.Length, storedHash.Length); i++)
            {
                hashEqual &= computedHash[i] == storedHash[i];
            }

            return hashEqual;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> VerifyAsync(string password, string savedHash)
    {
        return await Task.Run(() => Verify(password, savedHash));
    }
}