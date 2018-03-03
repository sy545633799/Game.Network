using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Network
{
    public class DynamicBufferManager
    {
        private int headSize = sizeof(int);
        private List<byte> readCache = new List<byte>();

        public void ProcessRead(byte[] buffer, int count, EventHandler<byte[]> callback, BaseClient userToken) //接收异步事件返回的数据，用于对数据进行缓存和分包
        {
            byte[] byteArray = new byte[count];
            Buffer.BlockCopy(buffer, 0, byteArray, 0, count);
            readCache.AddRange(byteArray);
            while (true)
            {
                if (readCache.Count < headSize) break;
                byte[] tmpArray = readCache.ToArray();
                int messageLenth = BitConverter.ToInt32(tmpArray, 0);
                if (readCache.Count - headSize >= messageLenth)
                {
                    byteArray = new byte[messageLenth];
                    Buffer.BlockCopy(tmpArray, headSize, byteArray, 0, messageLenth);
                    callback(this, byteArray);
                    readCache.RemoveRange(0, messageLenth + headSize);
                }
                else break;
            }
        }

        public byte[] ProcessWrite(byte[] dataBytes)
        {
            int dataLength = dataBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(dataLength);
            byte[] newBytes = lengthBytes.Concat(dataBytes).ToArray();
            return newBytes;
        }

    }
}
