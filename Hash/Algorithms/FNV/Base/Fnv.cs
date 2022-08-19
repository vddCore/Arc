namespace Arc.Hash.Algorithms.FNV.Base
{
    public abstract class Fnv : IHash<uint>
    {
        protected abstract uint Prime { get; }
        protected abstract uint Basis { get; }

        public abstract uint Calculate(byte[] data);
    }
}