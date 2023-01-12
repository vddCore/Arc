using System.Security.Cryptography;

namespace Arc.Hash.Algorithms
{
    public class Md5 : IHash<byte[]>
    {
        internal Md5()
        {
        }
        
        public byte[] Calculate(byte[] data)
        {
            using (var md5 = MD5.Create())
                return md5.ComputeHash(data);
        }
    }
}