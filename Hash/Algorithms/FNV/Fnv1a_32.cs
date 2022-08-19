using Arc.Hash.Algorithms.FNV.Base;

namespace Arc.Hash.Algorithms
{
    public class Fnv1a_32 : Fnv32
    {
        internal Fnv1a_32()
        {
        }
        
        public override uint Calculate(byte[] data)
        {
            var hash = Basis;

            foreach (var b in data)
            {
                hash ^= b;
                hash *= Prime;
            }

            return hash;
        }
    }
}