using Arc.Compression;
using Arc.Cryptography;

namespace Arc
{
    public static partial class Extensions
    {
        public static uint Adler32(this byte[] data)
        {
            uint a = 1, b = 0;
            
            foreach (byte c in data) {
                a = (a + c) % 65521;
                b = (b + a) % 65521;
            }
            
            return (b << 16) | a;
        }

        public static uint FNV1_32(this byte[] data)
        {

        }
        
        public static uint FNV1A_32(this byte[] data)
        {

        }
        
        public static uint FNV0_32(this byte[] data)
        {
            const uint prime = 0x01000193;


        }
    }
}