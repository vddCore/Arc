namespace Arc.Hash.Algorithms.Miscellaneous
{
    public class Adler32 : IHash<uint>
    {
        private const int Mod = 65521;

        internal Adler32()
        {
        }
        
        public uint Calculate(byte[] data)
        {
            uint c0 = 1, c1 = 0;
            
            foreach (var c in data) {
                c0 = (c1 + c) % Mod;
                c1 = (c1 + c0) % Mod;
            }
            
            return (c1 << 16) | c0;
        }
    }
}