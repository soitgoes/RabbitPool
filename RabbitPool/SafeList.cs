using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RabbitPool
{
    /// <summary>
    /// Queue == FIFO   Stack == LIFO
    /// LIFO if under capacity FIFO if over capacity
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SafeList<T> : IEnumerable<T>
    {
        private List<T> List = new();
        private readonly object hold = new();
        /// <summary>
        /// Enquees to the end of the list
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            lock (hold)
            {
                List.Add(item);
            }
        }

        public bool IsEmpty()
        {
            lock (hold)
            {
                return List.Count == 0;
            }
        }

        public T Head()
        {
            lock (hold)
            {
                return List.FirstOrDefault();
            }
        }
        /// <summary>
        /// Pops off the head of the list
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            lock (hold)
            {
                var head = List.FirstOrDefault();
                List.Remove(head);
                return head;
            }
        }

        public uint Count
        {
            get
            {
                lock (hold)
                {
                    return (uint)List.Count;
                }
            }
        }

        public T Tail()
        {
            return List.LastOrDefault();
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (hold)
            {
                return List.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (hold)
            {
                return List.GetEnumerator();
            }
        }

        public void Empty()
        {
            lock (hold)
            {
                List = new List<T>();
            }
        }
    }
    
    
}