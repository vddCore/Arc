using System;

namespace Arc.Compression.Algorithms.LZO
{
    internal class RingBuffer
    {
        private readonly byte[] _buffer;
        private int _position;
        private readonly int _size;

        public RingBuffer(int size)
        {
            _buffer = new byte[size];
            _size = size;
        }

        public void Seek(int offset)
        {
            _position += offset;
            if (_position > _size)
            {
                do
                {
                    _position -= _size;
                } while (_position > _size);
                return;
            }
            while (_position < 0)
            {
                _position += _size;
            }
        }

        public void Copy(byte[] buffer, int offset, int distance, int count)
        {
            if (_position - distance > 0 && _position + count < _size)
            {
                if (count < 10)
                {
                    do
                    {
                        var value = _buffer[_position - distance];
                        _buffer[_position++] = value;
                        buffer[offset++] = value;
                    } while (--count > 0);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _position - distance, buffer, offset, count);
                    Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
                    _position += count;
                }
            }
            else
            {
                Seek(-distance);
                Read(buffer, offset, count);
                Seek(distance - count);
                Write(buffer, offset, count);
            }
        }

        public void Read(byte[] buffer, int offset, int count)
        {
            if (count < 10 && (_position + count) < _size)
            {
                do
                {
                    buffer[offset++] = _buffer[_position++];
                } while (--count > 0);
            }
            else
            {
                while (count > 0)
                {
                    var copy = _size - _position;
                    if (copy > count)
                    {
                        Buffer.BlockCopy(_buffer, _position, buffer, offset, count);
                        _position += count;
                        break;
                    }
                    Buffer.BlockCopy(_buffer, _position, buffer, offset, copy);
                    _position = 0;
                    count -= copy;
                    offset += copy;
                }
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (count < 10 && (_position + count) < _size)
            {
                do
                {
                    _buffer[_position++] = buffer[offset++];
                } while (--count > 0);
            }
            else
            {
                while (count > 0)
                {
                    var cnt = _size - _position;
                    if (cnt > count)
                    {
                        Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
                        _position += count;
                        return;
                    }
                    Buffer.BlockCopy(buffer, offset, _buffer, _position, cnt);
                    _position = 0;
                    offset += cnt;
                    count -= cnt;
                }
            }
        }

        public RingBuffer Clone()
        {
            var result = new RingBuffer(_size) { _position = _position };
            Buffer.BlockCopy(_buffer, 0, result._buffer, 0, _size);
            return result;
        }
    }
}