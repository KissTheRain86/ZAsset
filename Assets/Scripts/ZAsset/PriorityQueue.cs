using System;
using System.Collections.Generic;

namespace ZAsset
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> heap;
        private Dictionary<T, int> itemIndices;

        public PriorityQueue()
        {
            heap = new List<T>();
            itemIndices = new Dictionary<T, int>();
        }

        public PriorityQueue(IEnumerable<T> collection) : this()
        {
            foreach (var item in collection)
            {
                Enqueue(item);
            }
        }

        public int Count => heap.Count;

        public void Enqueue(T item)
        {
            heap.Add(item);
            int index = heap.Count - 1;
            itemIndices[item] = index;
            BubbleUp(index);
        }

        public T Dequeue()
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("队列为空");

            T result = heap[0];
            itemIndices.Remove(result);

            if (heap.Count == 1)
            {
                heap.RemoveAt(0);
            }
            else
            {
                // 将最后一个元素移到顶部
                T lastItem = heap[heap.Count - 1];
                heap[0] = lastItem;
                itemIndices[lastItem] = 0;
                heap.RemoveAt(heap.Count - 1);

                // 向下调整堆
                BubbleDown(0);
            }

            return result;
        }

        public T Peek()
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("队列为空");
            return heap[0];
        }

        public bool Contains(T item)
        {
            return itemIndices.ContainsKey(item);
        }

        public bool Remove(T item)
        {
            if (!itemIndices.TryGetValue(item, out int index))
                return false;

            // 如果是最后一个元素，直接移除
            if (index == heap.Count - 1)
            {
                heap.RemoveAt(index);
                itemIndices.Remove(item);
                return true;
            }

            // 用最后一个元素替换要删除的元素
            T lastItem = heap[heap.Count - 1];
            heap[index] = lastItem;
            itemIndices[lastItem] = index;

            // 移除最后一个元素
            heap.RemoveAt(heap.Count - 1);
            itemIndices.Remove(item);

            // 调整堆
            if (!BubbleUp(index))
            {
                BubbleDown(index);
            }

            return true;
        }

        public void Clear()
        {
            heap.Clear();
            itemIndices.Clear();
        }

        private bool BubbleUp(int index)
        {
            bool moved = false;
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (heap[index].CompareTo(heap[parentIndex]) >= 0)
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
                moved = true;
            }
            return moved;
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int leftChildIndex = index * 2 + 1;
                int rightChildIndex = index * 2 + 2;
                int smallestChildIndex = index;

                if (leftChildIndex < heap.Count &&
                    heap[leftChildIndex].CompareTo(heap[smallestChildIndex]) < 0)
                {
                    smallestChildIndex = leftChildIndex;
                }

                if (rightChildIndex < heap.Count &&
                    heap[rightChildIndex].CompareTo(heap[smallestChildIndex]) < 0)
                {
                    smallestChildIndex = rightChildIndex;
                }

                if (smallestChildIndex == index)
                    break;

                Swap(index, smallestChildIndex);
                index = smallestChildIndex;
            }
        }

        private void Swap(int i, int j)
        {
            T temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;

            itemIndices[heap[i]] = i;
            itemIndices[heap[j]] = j;
        }

        public override string ToString()
        {
            return string.Join(", ", heap);
        }
    }


}
