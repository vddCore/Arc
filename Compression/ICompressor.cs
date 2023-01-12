using System.IO;

namespace Arc.Compression
{
    public interface ICompressor
    {
        byte[] Compress(byte[] decompressed);
        int Compress(Stream input, Stream output);
    }
}