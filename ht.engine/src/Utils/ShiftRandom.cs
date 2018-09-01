using System;

namespace HT.Engine.Utils
{
    /// <summary>
    /// Fast random, can be used for anything where distribution isn't crucially important.
    /// </summary>
    public sealed class ShiftRandom : IRandom
    {
        private readonly object lockObject;
        private UInt16 lfsr;
        private UInt16 bit;

        public ShiftRandom() 
            : this(seed: (UInt16)(DateTimeOffset.Now.UtcTicks % ushort.MaxValue)) { }

        public ShiftRandom(UInt16 seed)
        {
            lockObject = new object();
            lfsr = seed > 0 ? seed : (UInt16)1; //Seed of 0 is unsupported
            bit = 0;
        }

        public float GetNext()
        {
            float result = 0f;
            lock(lockObject)
            {
                //Implementation of: Linear Feedback Shift Register 
                //https://en.wikipedia.org/wiki/Linear-feedback_shift_register
                //Taps: 16 14 13 11; feedback polynomial: x^16 + x^14 + x^13 + x^11 + 1
                for (int i = 0; i < 7; i++) //Execute multiple shifts to improve distribution
                {
                    bit = (UInt16)(((lfsr >> 0) ^ (lfsr >> 2) ^ (lfsr >> 3) ^ (lfsr >> 5)) & 1);
                    lfsr = (UInt16)((lfsr >> 1) | (bit << 15));
                }

                //Convert to standard 0 - 1 float range
                result = (float)lfsr / ushort.MaxValue;
            }
            return result;
        }
    }
}