namespace Arc.Hash.Algorithms.FNV.Base
{
    public abstract class Fnv32 : Fnv
    {
        protected override uint Prime => 0x01000193;
        protected override uint Basis => 0x811C9DC5;
    }
}