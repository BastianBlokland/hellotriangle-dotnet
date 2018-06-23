using System;

namespace HT.Engine.Utils
{
    /// <summary>
    /// Wrapper around a array that keeps a count and resizes when you add something and its empty.
    /// Why not use a list? This has the advantage that it gives you access to the raw data array
    /// so its usefull in allot of apis where you need to pass a array (and can pass a count) but 
    /// you don't know the amount of elements ahead of time.
    /// </summary>
    public sealed class ResizeArray<T>
        where T : struct
    {
        //Helper properties
        public int CurrentCapacity => data.Length;
        public bool IsEmpty => count == 0;
        public T FirstOrDefault => IsEmpty ? default(T) : data[0];
        public T LastOrDefault => IsEmpty ? default(T) : data[Count - 1];
        public int Count
        {
            get => count;
            set
            {
                if (value < 0 || value > CurrentCapacity)
                    throw new ArgumentOutOfRangeException(
                        $"[{nameof(ResizeArray<T>)}] Count < 0 or bigger then capacity");
                count = value;
            }
        }
        public T[] Data => data;

        //Data
        private T[] data;
        private int count;

        public ResizeArray(params T[] array)
        {
            this.data = array;
            count = array.Length;
        }

        public ResizeArray(int initialCapacity = 20)
        {
            data = new T[initialCapacity];
            count = 0;
        }

        public void Resize(int capacity) => Array.Resize(ref data, capacity);
        
        public void Add(T item)
        {
            //if the current array is full we need to allocate a new one
            if (count >= data.Length)
                Resize(data.Length * 2); //Current strategy is to just double
            data[count] = item;
            count++;
        }

        public void RemoveFromEnd()
        {
            if (count <= 0)
                throw new Exception($"[{nameof(ResizeArray<T>)}] Collection is empty!");
            count--;
        }

        public void RemoveAt(int index)
        {
            count--;
            if (index < count)
                Array.Copy(data, index + 1, data, index, Count - index);
        }

        public void Clear() => count = 0;

        public T[] ToArray()
        {
            var ret = new T[Count];
            Array.Copy(data, ret, Count);
            return ret;
        }
    }
}