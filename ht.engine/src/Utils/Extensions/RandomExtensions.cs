using System;
using System.Collections.Generic;

using HT.Engine.Math;

namespace HT.Engine.Utils
{
    public static class RandomExtensions
    {
        public static float GetNextAngle(this IRandom random)
            => random.GetNext() * FloatUtils.DOUBLE_PI;

        public static float GetBetween(this IRandom random, float minValue, float maxValue)
            => minValue + (maxValue - minValue) * random.GetNext();

        public static Float2 GetBetween(this IRandom random, Float2 minValue, Float2 maxValue)
            => (random.GetBetween(minValue.X, maxValue.X),
                random.GetBetween(minValue.Y, maxValue.Y));

        public static Float3 GetBetween(this IRandom random, Float3 minValue, Float3 maxValue)
            => (random.GetBetween(minValue.X, maxValue.X),
                random.GetBetween(minValue.Y, maxValue.Y),
                random.GetBetween(minValue.Z, maxValue.Z));

        public static Float4 GetBetween(this IRandom random, Float4 minValue, Float4 maxValue)
            => (random.GetBetween(minValue.X, maxValue.X),
                random.GetBetween(minValue.Y, maxValue.Y),
                random.GetBetween(minValue.Z, maxValue.Z),
                random.GetBetween(minValue.W, maxValue.W));

        //NOTE: minValue is inclusive and maxValue is exclusive
        public static int GetBetween(this IRandom random, int minValue, int maxValue)
            => IntUtils.Min((int)random.GetBetween((float)minValue, (float)maxValue), maxValue - 1);

        //Fisher–Yates shuffle: https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        public static void Shuffle<T>(this IRandom random, IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.GetBetween(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static T PickRandom<T>(this IRandom random, IList<T> list)
            => list.Count == 0 ? default(T) : list[random.GetBetween(0, list.Count)];
    }
}