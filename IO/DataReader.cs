using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Arc.IO
{
    public sealed class DataReader
    {
        private BinaryReader _binaryReader;

        public bool IsBigEndian { get; set; } = false;

        public DataReader(Stream input)
            => _binaryReader = new BinaryReader(input);

        public DataReader(Stream input, Encoding encoding)
            => _binaryReader = new BinaryReader(input, encoding);

        public DataReader(Stream input, Encoding encoding, bool leaveOpen)
            => _binaryReader = new BinaryReader(input, encoding, leaveOpen);

        public sbyte ReadSByte()
            => _binaryReader.ReadSByte();
        
        public byte ReadByte()
            => _binaryReader.ReadByte();
        
        public byte[] ReadBytes(int length)
            => _binaryReader.ReadBytes(length);

        public bool ReadBoolean()
            => _binaryReader.ReadBoolean();

        public string ReadString()
            => _binaryReader.ReadString();

        public string ReadAsciiString()
        {
            var dataBytes = new List<byte>();

            while (PeekChar() != 0)
                dataBytes.Add(ReadByte());
            
            ReadByte();
            return Encoding.ASCII.GetString(dataBytes.ToArray());
        }

        public int PeekChar()
            => _binaryReader.PeekChar();

        public short ReadInt16()
            => TrySwapByteOrder(BitConverter.ToInt16);

        public ushort ReadUInt16()
            => TrySwapByteOrder(BitConverter.ToUInt16);

        public int ReadInt32()
            => TrySwapByteOrder(BitConverter.ToInt32);

        public uint ReadUInt32()
            => TrySwapByteOrder(BitConverter.ToUInt32);

        public long ReadInt64()
            => TrySwapByteOrder(BitConverter.ToInt64);

        public ulong ReadUInt64()
            => TrySwapByteOrder(BitConverter.ToUInt64);

        public double ReadDouble()
            => TrySwapByteOrder(BitConverter.ToDouble);

        public float ReadSingle()
            => TrySwapByteOrder(BitConverter.ToSingle);

        public Half ReadHalf()
            => TrySwapByteOrder(BitConverter.ToHalf);

        public UInt24 ReadUInt24()
        {
            var le = ReadBytes(3);

            if (IsBigEndian)
                Array.Reverse(le);

            return new UInt24(le);
        }

        private T TrySwapByteOrder<T>(Func<byte[], int, T> convFunc) where T : struct
        {
            var be = _binaryReader.ReadBytes(Marshal.SizeOf<T>());

            if (IsBigEndian)
                Array.Reverse(be);

            return convFunc(be, 0);
        }

        internal void Dispose()
        {
            _binaryReader?.Dispose();
        }
    }
}