using System.Security.Cryptography;
using System.Text;

namespace Service.Helpers;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder();
        
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        
        return sb.ToString();
    }

    public static bool VerifyPassword(string inputPassword, string storedHash)
    {
        var inputHash = HashPassword(inputPassword);
        var hashBytes1 = StringToByteArray(inputHash);
        var hashBytes2 = StringToByteArray(storedHash);

        return CryptographicOperations.FixedTimeEquals(hashBytes1, hashBytes2);
    }
    
    private static byte[] StringToByteArray(string hex)
    {
        var length = hex.Length;
        var bytes = new byte[length / 2];
        
        for (var i = 0; i < length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        
        return bytes;
    }
}