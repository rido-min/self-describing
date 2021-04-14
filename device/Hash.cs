using System.Security.Cryptography;
using System.Text;

namespace common
{
    public static class Hash
    {
        public static string GetHashString(string inputString)
        {
            static byte[] GetHash(string inputString)
            {
                using HashAlgorithm algorithm = SHA256.Create();
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }

            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString)) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
