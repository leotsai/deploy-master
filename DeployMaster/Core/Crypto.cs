using System;
using System.Security.Cryptography;
using System.Text;

namespace DeployMaster.Core
{
    internal class Crypto
    {
        public static string Md5HashEncrypt(string input)
        {
            var md5 = new MD5CryptoServiceProvider();
            var result = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(result);
        }

        public static string Sha1HashEncrypt(string input)
        {
            var sha1 = new SHA1CryptoServiceProvider();
            var result = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(result);
        }
    }
}
