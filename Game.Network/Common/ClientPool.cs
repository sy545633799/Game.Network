using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Network
{
    public class ClientPool<T>
    {
        private Stack<T> m_pool;

        public ClientPool(int capacity)
        {
            m_pool = new Stack<T>(capacity);
        }

        public void Push(T item)
        {
            if (item == null)
            {
                throw new ArgumentException("Items added to a AsyncSocketUserToken cannot be null");
            }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        public T Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        public int Count
        {
            get { return m_pool.Count; }
        }
    }
}
