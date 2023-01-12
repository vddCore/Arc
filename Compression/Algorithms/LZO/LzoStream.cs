using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Arc.Compression.Algorithms.LZO
{
    internal class LzoStream : Stream
    {
        protected readonly Stream Source;
        private long? _length;
        private readonly bool _leaveOpen;
        protected byte[] DecodedBuffer;
        protected const int MaxWindowSize = (1 << 14) + ((255 & 8) << 11) + (255 << 6) + (255 >> 2);
        protected RingBuffer RingBuffer = new(MaxWindowSize);
        protected long OutputPosition;
        protected int Instruction;
        protected LzoState State;

        protected enum LzoState
        {
            ZeroCopy = 0,
            SmallCopy1 = 1,
            SmallCopy2 = 2,
            SmallCopy3 = 3,
            LargeCopy = 4
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                if (_length.HasValue)
                    return _length.Value;
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get => OutputPosition;
            set
            {
                if (OutputPosition == value) return;
                Seek(value, SeekOrigin.Begin);
            }
        }

        public LzoStream(Stream stream, CompressionMode mode)
            : this(stream, mode, false)
        {
        }

        public LzoStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            if (mode != CompressionMode.Decompress)
                throw new NotSupportedException("Compression is not supported");
            if (!stream.CanRead)
                throw new ArgumentException("write-only stream cannot be used for decompression");
            Source = stream;
            if (!(stream is BufferedStream))
                Source = new BufferedStream(stream);
            _leaveOpen = leaveOpen;
            DecodeFirstByte();
        }

        private void DecodeFirstByte()
        {
            Instruction = Source.ReadByte();
            if (Instruction == -1)
                throw new EndOfStreamException();
            if (Instruction > 15 && Instruction <= 17)
            {
                throw new Exception();
            }
        }

        private void Copy(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count > 0);
            do
            {
                var read = Source.Read(buffer, offset, count);
                if (read == 0)
                    throw new EndOfStreamException();
                RingBuffer.Write(buffer, offset, read);
                offset += read;
                count -= read;
            } while (count > 0);
        }

        protected virtual int Decode(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count > 0);
            Debug.Assert(DecodedBuffer == null);
            int read;
            var i = Instruction >> 4;
            switch (i)
            {
                case 0:
                {
                    switch (State)
                    {
                        case LzoState.ZeroCopy:
                        {
                            var length = 3;
                            if (Instruction != 0)
                            {
                                length += Instruction;
                            }
                            else
                            {
                                length += 15 + ReadLength();
                            }
                            State = LzoState.LargeCopy;
                            if (length <= count)
                            {
                                Copy(buffer, offset, length);
                                read = length;
                            }
                            else
                            {
                                Copy(buffer, offset, count);
                                DecodedBuffer = new byte[length - count];
                                Copy(DecodedBuffer, 0, length - count);
                                read = count;
                            }
                            break;
                        }
                        case LzoState.SmallCopy1:
                        case LzoState.SmallCopy2:
                        case LzoState.SmallCopy3:
                            read = SmallCopy(buffer, offset, count);
                            break;
                        case LzoState.LargeCopy:
                            read = LargeCopy(buffer, offset, count);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                }
                case 1:
                {
                    int length = (Instruction & 0x7) + 2;
                    if (length == 2)
                    {
                        length += 7 + ReadLength();
                    }
                    var s = Source.ReadByte();
                    var d = Source.ReadByte();
                    if (s != -1 && d != -1)
                    {
                        d = ((d << 8) | s) >> 2;
                        var distance = 16384 + ((Instruction & 0x8) << 11) | d;
                        if (distance == 16384)
                            return -1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, s & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
                case 2:
                case 3:
                {
                    int length = (Instruction & 0x1f) + 2;
                    if (length == 2)
                    {
                        length += 31 + ReadLength();
                    }
                    var s = Source.ReadByte();
                    var d = Source.ReadByte();
                    if (s != -1 && d != -1)
                    {
                        d = ((d << 8) | s) >> 2;
                        var distance = d + 1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, s & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
                case 4:
                case 5:
                case 6:
                case 7:
                {
                    var length = 3 + ((Instruction >> 5) & 0x1);
                    var result = Source.ReadByte();
                    if (result != -1)
                    {
                        var distance = (result << 3) + ((Instruction >> 2) & 0x7) + 1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, Instruction & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
                default:
                {
                    var length = 5 + ((Instruction >> 5) & 0x3);
                    var result = Source.ReadByte();
                    if (result != -1)
                    {
                        var distance = (result << 3) + ((Instruction & 0x1c) >> 2) + 1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, Instruction & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
            }
            Instruction = Source.ReadByte();
            if (Instruction != -1)
            {
                OutputPosition += read;
                return read;
            }
            throw new EndOfStreamException();
        }

        private int LargeCopy(byte[] buffer, int offset, int count)
        {
            var result = Source.ReadByte();
            if (result != -1)
            {
                var distance = (result << 2) + ((Instruction & 0xc) >> 2) + 2049;

                return CopyFromRingBuffer(buffer, offset, count, distance, 3, Instruction & 0x3);
            }
            throw new EndOfStreamException();
        }

        private int SmallCopy(byte[] buffer, int offset, int count)
        {
            var h = Source.ReadByte();
            if (h != -1)
            {
                var distance = (h << 2) + ((Instruction & 0xc) >> 2) + 1;

                return CopyFromRingBuffer(buffer, offset, count, distance, 2, Instruction & 0x3);
            }

            throw new EndOfStreamException();
        }

        private int ReadLength()
        {
            int b;
            int length = 0;
            while ((b = Source.ReadByte()) == 0)
            {
                if (length >= Int32.MaxValue - 1000)
                {
                    throw new Exception();
                }
                length += 255;
            }
            if (b != -1) return length + b;
            throw new EndOfStreamException();
        }

        private int CopyFromRingBuffer(byte[] buffer, int offset, int count, int distance, int copy, int state)
        {
            Debug.Assert(copy >= 0);
            var result = copy + state;
            State = (LzoState)state;
            if (count >= result)
            {
                var size = copy;
                if (copy > distance)
                {
                    size = distance;
                    RingBuffer.Copy(buffer, offset, distance, size);
                    copy -= size;
                    var copies = copy / distance;
                    for (int i = 0; i < copies; i++)
                    {
                        Buffer.BlockCopy(buffer, offset, buffer, offset + size, size);
                        offset += size;
                        copy -= size;
                    }
                    if (copies > 0)
                    {
                        var length = size * copies;
                        RingBuffer.Write(buffer, offset - length, length);
                    }
                    offset += size;
                }
                if (copy > 0)
                {
                    if (copy < size)
                        size = copy;
                    RingBuffer.Copy(buffer, offset, distance, size);
                    offset += size;
                }
                if (state > 0)
                {
                    Copy(buffer, offset, state);
                }
                return result;
            }

            if (count <= copy)
            {
                CopyFromRingBuffer(buffer, offset, count, distance, count, 0);
                DecodedBuffer = new byte[result - count];
                CopyFromRingBuffer(DecodedBuffer, 0, DecodedBuffer.Length, distance, copy - count, state);
                return count;
            }
            CopyFromRingBuffer(buffer, offset, count, distance, copy, 0);
            var remaining = count - copy;
            DecodedBuffer = new byte[state - remaining];
            Copy(buffer, offset + copy, remaining);
            Copy(DecodedBuffer, 0, state - remaining);
            return count;
        }

        private int ReadInternal(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count > 0);
            if (_length.HasValue && OutputPosition >= _length)
                return -1;
            int read;
            if (DecodedBuffer == null)
            {
                if ((read = Decode(buffer, offset, count)) >= 0) return read;
                _length = OutputPosition;
                return -1;
            }
            var decodedLength = DecodedBuffer.Length;
            if (count > decodedLength)
            {
                Buffer.BlockCopy(DecodedBuffer, 0, buffer, offset, decodedLength);
                DecodedBuffer = null;
                OutputPosition += decodedLength;
                return decodedLength;
            }
            Buffer.BlockCopy(DecodedBuffer, 0, buffer, offset, count);
            if (decodedLength > count)
            {
                var remaining = new byte[decodedLength - count];
                Buffer.BlockCopy(DecodedBuffer, count, remaining, 0, remaining.Length);
                DecodedBuffer = remaining;
            }
            else
            {
                DecodedBuffer = null;
            }
            OutputPosition += count;
            return count;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_length.HasValue && OutputPosition >= _length)
                return 0;
            var result = 0;
            while (count > 0)
            {
                var read = ReadInternal(buffer, offset, count);
                if (read == -1)
                    return result;
                result += read;
                offset += read;
                count -= read;
            }
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("cannot write to readonly stream");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_leaveOpen)
                Source.Dispose();

            base.Dispose(disposing);
        }
    }
}