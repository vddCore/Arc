using System;
using System.IO;
using System.Linq;
using System.Text;
using Arc.Compression;
using Arc.Hash;
using Arc.IO;

namespace Arc
{
    public class Container : IDisposable
    {
        private Stream _stream;
        private DataReader _dataReader;

        public Container(string filePath)
        {
            _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            _dataReader = new DataReader(_stream);
        }

        public Container(Stream stream)
        {
            _stream = stream;
            _dataReader = new DataReader(_stream);
        }

        public Container Offset(long offset, SeekOrigin origin = SeekOrigin.Current)
        {
            _stream.Seek(offset, origin);
            return this;
        }

        public Container Magic(string magic)
            => Magic(magic, Encoding.UTF8);

        public Container Magic(string magic, Encoding encoding)
            => Magic(encoding.GetBytes(magic));

        public Container Magic(byte[] magicBytes)
        {
            var data = _dataReader.ReadBytes(magicBytes.Length);

            if (!magicBytes.SequenceEqual(data))
                throw new InvalidDataException("Magic number mismatch.");

            return this;
        }

        public Container WriteToDisk(string filePath, int length)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                var pos = _stream.Position;
                var data = _dataReader.ReadBytes(length);
                _stream.Seek(pos, SeekOrigin.Begin);

                fs.Write(data);
                fs.Flush(true);
            }

            return this;
        }

        public Container SubFile(int length, out Container subFile)
        {
            subFile = new Container(
                new MemoryStream(
                    _dataReader.ReadBytes(length)
                )
            );

            return this;
        }

        public Container Parse<T>(out T[] segments, int count = 1)
            where T : Segment, new()
        {
            segments = new T[count];

            for (var i = 0; i < count; i++)
            {
                segments[i] = new T();
                segments[i].Read(_dataReader);
            }

            return this;
        }

        public Container Decompress<T>(out Container decompressed, int compressedLength = 0, bool preserveOffset = true)
            where T : IDecompressor
        {
            var offset = _stream.Position;

            var decompressedBytes = Activator.CreateInstance<T>().Decompress(
                _dataReader.ReadBytes(compressedLength > 0 ? compressedLength : (int)_stream.Length)
            );

            if (preserveOffset)
            {
                _stream.Seek(-offset, SeekOrigin.Current);
            }

            decompressed = new Container(
                new MemoryStream(decompressedBytes)
            );

            return this;
        }

        public Container Hash<T, U>(out U checksum, int length = 0, bool preserveOffset = true)
            where T : IHash<U>
        {
            var offset = _stream.Position;

            checksum = Activator.CreateInstance<T>().Calculate(
                _dataReader.ReadBytes(length > 0 ? length : (int)_stream.Length)
            );

            if (preserveOffset)
            {
                _stream.Seek(-offset, SeekOrigin.Current);
            }

            return this;
        }

        public Container SByte(out sbyte value)
        {
            value = _dataReader.ReadSByte();
            return this;
        }

        public Container Int16(out short value, Endian endian = Endian.Little)
        {
            value = WithEndianness(endian, () => _dataReader.ReadInt16());
            return this;
        }

        public Container Int32(out int value, Endian endian = Endian.Little)
        {
            value = WithEndianness(endian, () => _dataReader.ReadInt32());
            return this;
        }

        public Container Int64(out long value, Endian endian = Endian.Little)
        {
            value = WithEndianness(endian, () => _dataReader.ReadInt64());
            return this;
        }

        public Container Byte(out byte value)
        {
            value = _dataReader.ReadByte();
            return this;
        }

        public Container UInt16(out ushort value, Endian endian = Endian.Little)
        {
            value = WithEndianness(endian, () => _dataReader.ReadUInt16());
            return this;
        }

        public Container UInt32(out uint value, Endian endian = Endian.Little)
        {
            value = WithEndianness(endian, () => _dataReader.ReadUInt32());
            return this;
        }

        public Container UInt64(out ulong value, Endian endian = Endian.Little)
        {
            value = WithEndianness(endian, () => _dataReader.ReadUInt64());
            return this;
        }

        public Container Bytes(int length, out byte[] value)
        {
            value = _dataReader.ReadBytes(length);
            return this;
        }

        public void Dispose()
        {
            _dataReader?.Dispose();
        }

        private T WithEndianness<T>(Endian endian, Func<T> action)
        {
            var prevState = _dataReader.IsBigEndian;
            _dataReader.IsBigEndian = endian == Endian.Big;
            var ret = action();
            _dataReader.IsBigEndian = prevState;

            return ret;
        }
    }
}