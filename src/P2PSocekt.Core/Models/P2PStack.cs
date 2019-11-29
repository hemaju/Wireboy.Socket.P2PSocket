using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class P2PStack<T>
    {
        T[] DataList;
        int NextIndex = 0;
        public int MaxLength => DataList.Length;
        public int Count
        {
            private set;
            get;
        } = 0;
        public P2PStack(int maxLength)
        {
            DataList = new T[maxLength];
        }
        public P2PStack(int maxLength,T obj)
        {
            DataList = new T[maxLength];
            DataList[0] = obj;
        }

        public void Push(T obj)
        {
            DataList[NextIndex % DataList.Length] = obj;
            NextIndex = ++NextIndex % DataList.Length;
            if(DataList.Length > Count)
            {
                Count++;
            }
        }
        public T Pop()
        {
            if (Count > 0)
            {
                NextIndex--;
                Count--;
                return DataList[NextIndex];
            }
            else
                return default(T);
        }

        public T First()
        {
            if (Count == DataList.Length)
            {
                return DataList[NextIndex % DataList.Length];
            }
            else
            {
                return DataList[0];
            }
        }
    }
}
