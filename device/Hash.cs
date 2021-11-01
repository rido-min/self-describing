using System.Linq;
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

            var sb = new StringBuilder();
            GetHash(inputString).ToList().ForEach(b => sb.Append(b.ToString("X2")));
            return sb.ToString();
        }
    }
}
