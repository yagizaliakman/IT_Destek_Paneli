using System.Security.Cryptography;
using System.Text;

namespace IT_Destek_Panel.Helpers
{
    public static class SecurityHelper
    {
        // Şifreyi alır, SHA-256 ile kırılmaz bir metne (Hash) çevirir
        public static string HashPassword(string rawPassword)
        {
            if (string.IsNullOrEmpty(rawPassword)) return "";

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}