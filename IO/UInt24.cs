using System;

namespace Arc.IO
{
    public struct UInt24 : IEquatable<UInt24>
    {
        private byte _i0;
        private byte _i1;
        private byte _i2;

        public UInt24(ReadOnlySpan<byte> data)
        {
            _i0 = data[0];
            _i1 = data[1];
            _i2 = data[2];
        }

        public uint ToUInt32()
            => (uint)((_i2 << 16) | (_i1 << 8) | _i0 << 0);
        
        public int ToInt32()
            => (int)((_i2 << 16) | (_i1 << 8) | _i0 << 0);
        
        public static implicit operator uint(UInt24 u24)
            => u24.ToUInt32();

        public static implicit operator int(UInt24 u24)
            => u24.ToInt32();

        public static implicit operator UInt24(uint u32)
        {
            var bytes = BitConverter.GetBytes(u32);
            return new UInt24(new[] { bytes[0], bytes[1], bytes[2] });
        }

        public static implicit operator UInt24(int i32)
        {
            var bytes = BitConverter.GetBytes(i32);
            return new UInt24(new[] { bytes[0], bytes[1], bytes[2] });
        }

        public bool Equals(UInt24 other)
            => _i0 == other._i0
               && _i1 == other._i1
               && _i2 == other._i2;

        public override bool Equals(object obj)
            => obj is UInt24 other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(_i0, _i1, _i2);

        public static bool operator ==(UInt24 left, UInt24 right)
            => left.Equals(right);

        public static bool operator !=(UInt24 left, UInt24 right)
            => !left.Equals(right);

        public override string ToString()
            => ToUInt32().ToString();
    }
}