using System;
using System.Collections;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        public class LimitedQueue
        {
            private Queue<string> Queue;
            private int MaxItems;

            public LimitedQueue(int maxItems)
            {
                MaxItems = maxItems;
                Queue = new Queue<string>();
            }
            public void Enqueue(string item)
            {
                Queue.Enqueue(item);
                if (Queue.Count > MaxItems)
                    Queue.Dequeue();
            }

            public string Dump()
            {
                return string.Join(Environment.NewLine, Queue);
            }
        }
    }
}
