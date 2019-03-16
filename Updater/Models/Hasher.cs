using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace Updater
{
    internal enum HashType
    {
        MD5,
        SHA1,
        SHA512
    }

    internal static class Hasher
    {
        internal static string Hashfile(string filepath, HashType algo)
        {
            switch (algo)
            {
                case HashType.MD5:
                    return MakeHashString(MD5.Create().ComputeHash(new FileStream(filepath, FileMode.Open)));
                case HashType.SHA1:
                    return MakeHashString(SHA1.Create().ComputeHash(new FileStream(filepath, FileMode.Open)));
                case HashType.SHA512:
                    return MakeHashString(SHA512.Create().ComputeHash(new FileStream(filepath, FileMode.Open)));
                default:
                    return "";
            }
        }

        private static string MakeHashString(byte[] hash)
        {
            StringBuilder s = new StringBuilder(hash.Length * 2);

            foreach (byte b in hash)
            {
                s.Append(b.ToString("X2").ToLower());
            }

            return s.ToString();
        }
    }
}
