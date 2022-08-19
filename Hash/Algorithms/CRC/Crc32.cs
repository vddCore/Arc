namespace Arc.Hash.Algorithms.CRC
{
    public class Crc32 : IHash<uint>
    {
        internal Crc32()
        {
        }
        
        public uint Calculate(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            uint mask;
            
            for (var i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (var j = 7; j >= 0; j--)
                {
                    mask = (uint)(-(crc & 1));
                    crc >>= 1;
                    crc ^= 0xEDB88320 & mask;
                }
            }

            return ~crc;
        }
    }
}