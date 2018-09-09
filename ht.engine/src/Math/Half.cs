using System;
using System.Runtime.InteropServices;

using HT.Engine.Math;

namespace HT.Engine.Math
{
    /// <summary>
    /// Representation of a 16 bit floating point value, use only as a storage / conversion type. 
    /// Conversion implementation based on xna's halfSingle implementation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE)]
    public readonly struct Half : IEquatable<Half>
    {
        public const int SIZE = 2;

        //Presets
        public static readonly Half Zero = FromFloat(0f);
        public static readonly Half One = FromFloat(1f);

        //Data
        private readonly ushort data;

        private Half(ushort data) => this.data = data;

        //Utilities
        public float ToFloat()
        {
            uint rst;
			uint mantissa = (uint)(data & 1023);
			uint exp = 0xfffffff2;

			if ((data & -33792) == 0)
			{
				if (mantissa != 0)
				{
					while ((mantissa & 1024) == 0)
					{
						exp--;
						mantissa = mantissa << 1;
					}
					mantissa &= 0xfffffbff;
					rst = ((uint) ((((uint) data & 0x8000) << 16) | ((exp + 127) << 23))) | (mantissa << 13);
				}
				else
					rst = (uint) ((data & 0x8000) << 16);
			}
			else
				rst = (uint) (((((uint) data & 0x8000) << 16) | ((((((uint) data >> 10) & 0x1f) - 15) + 127) << 23)) | (mantissa << 13));

            return rst.AsFloat();
        }

        //Creation
        public static Half FromFloat(float val)
        {
            int i = val.AsInt();
            int s = (i >> 16) & 0x00008000;
            int e = ((i >> 23) & 0x000000ff) - (127 - 15);
            int m = i & 0x007fffff;

            if (e <= 0)
            {
                if (e < -10)
                    return new Half((ushort)s);

                m = m | 0x00800000;

                int t = 14 - e;
                int a = (1 << (t - 1)) - 1;
                int b = (m >> t) & 1;

                m = (m + a + b) >> t;
                return new Half((ushort)(s | m));
            }
            else if (e == 0xff - (127 - 15))
            {
                if (m == 0)
                    return new Half((ushort)(s | 0x7c00));
                else
                {
                    m >>= 13;
                    return new Half((ushort)(s | 0x7c00 | m | ((m == 0) ? 1 : 0)));
                }
            }
            else
            {
                m = m + 0x00000fff + ((m >> 13) & 1);

                if ((m & 0x00800000) != 0)
                {
                    m = 0;
                    e += 1;
                }
                if (e > 30)
                    return new Half((ushort)(s | 0x7c00));
                return new Half((ushort)(s | (e << 10) | (m >> 13)));
            }
        }

        //Equality
        public static bool operator ==(Half a, Half b) => a.Equals(b);

        public static bool operator !=(Half a, Half b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Half && Equals((Half)obj);

        public bool Equals(Half other) => other.data == data;

        public override int GetHashCode() => data.GetHashCode();

        public override string ToString() => ToFloat().ToString();
    }
}