using System;
using System.Collections.Generic;

namespace ZAsset
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> data;

        public PriorityQueue()
        {
            this.data = new List<T>();
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int childIndex = data.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (data[childIndex].CompareTo(data[parentIndex]) >= 0)
                    break;

                T temp = data[childIndex];
                data[childIndex] = data[parentIndex];
                data[parentIndex] = temp;
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Queue is Empty!");

            int lastIndex = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);
            lastIndex--;

            int parentIndex = 0;
            while(true)
            {
                int leftChildIndex = parentIndex * 2 + 1;
                if (leftChildIndex > lastIndex) break;

                int rightChildIndex = leftChildIndex + 1;
                if (rightChildIndex <= lastIndex && data[rightChildIndex].CompareTo(data[leftChildIndex]) < 0)
                    leftChildIndex = rightChildIndex;

                if (data[parentIndex].CompareTo(data[leftChildIndex]) <= 0)
                    break;

                T temp = data[parentIndex];
                data[parentIndex] = data[leftChildIndex];
                data[leftChildIndex] = temp;
                parentIndex = leftChildIndex;
            }

            return frontItem;
        }

        public T Peek()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Queue is Empty!");
            return data[0];
        }
    }
}
