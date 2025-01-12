using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    internal class SecureMethods
    {
        //Encodes string to base 64
        public static string EncodeBase64(string data)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(plainTextBytes);
        }

        //Decodes string from base64
        public static string DecodeBase64(string data)
        {
            var base64EncodedBytes = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        //Hashes string data, used on base 64 encoded
        public static string HashData(string data)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(data));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
