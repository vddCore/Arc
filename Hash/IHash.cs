namespace Arc.Hash
{
    public interface IHash<T>
    {
        public T Calculate(byte[] data);
    }
}