using Arc.Hash.Algorithms.FNV.Base;

namespace Arc.Hash.Algorithms
{
    public class Fnv0_32 : Fnv32
    {
        public override uint Calculate(byte[] data)
        {
            uint hash = 0;

            foreach (var b in data)
            {
                hash ^= b;
                hash *= Prime;
            }

            return hash;
        }
    }
}