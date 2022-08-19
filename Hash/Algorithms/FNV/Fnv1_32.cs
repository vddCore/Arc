using Arc.Hash.Algorithms.FNV.Base;

namespace Arc.Hash.Algorithms
{
    public class Fnv1_32 : Fnv32
    {
        internal Fnv1_32()
        {
        }

        public override uint Calculate(byte[] data)
        {
            var hash = Basis;

            foreach (var b in data)
            {
                hash *= Prime;
                hash ^= b;
            }

            return hash;
        }
    }
}