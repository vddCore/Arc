using System.IO;
using System.IO.Compression;
using Arc.Compression.Algorithms.LZO;

namespace Arc.Compression.Algorithms
{
    public class Lzo : IDecompressor, ICompressor
    {
        internal Lzo()
        {
        }
        
        public byte[] Decompress(byte[] compressed)
        {
            using var ms = new MemoryStream(compressed);
            using var lzoStream = new LzoStream(ms, CompressionMode.Decompress);
            using var outms = new MemoryStream();
            lzoStream.CopyTo(outms);

            return outms.ToArray();
        }

        public int Decompress(Stream input, Stream output)
        {
            using var lzoStream = new LzoStream(input, CompressionMode.Decompress);
            lzoStream.CopyTo(output);
            
            return (int)output.Length;
        }

        public byte[] Compress(byte[] decompressed)
        {
            using var ms = new MemoryStream(decompressed);
            using var lzoStream = new LzoStream(ms, CompressionMode.Compress);
            using var outms = new MemoryStream();
            lzoStream.CopyTo(outms);

            return outms.ToArray();
        }

        public int Compress(Stream input, Stream output)
        {
            using var lzoStream = new LzoStream(input, CompressionMode.Compress);
            lzoStream.CopyTo(output);
            
            return (int)output.Length;
        }
    }
}