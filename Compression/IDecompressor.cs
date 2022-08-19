using System.IO;

namespace Arc.Compression
{
    public interface IDecompressor
    {
        byte[] Decompress(byte[] compressed);
        int Decompress(Stream input, Stream output);
    }
}