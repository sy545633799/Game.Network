using Server.Base.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
namespace Server.Base
{
    /// <summary>
    /// 
    /// </summary>
    public class NetUser : NetConnection
    {
        private Dictionary<System.Type, UserComponent> userComponentDic = new Dictionary<System.Type, UserComponent>();
        private MemoryStream readStream = new MemoryStream();
        internal NetServer netServer;
        private UserPacket userPacket;
        public int Id;
        internal NetUser()
        {

        }

        internal void Write(SocketAsyncEventArgs e)
        {
            try
            {
                readStream.Write(e.Buffer, e.Offset, e.BytesTransferred);
                while (readStream.Length >= headSize)
                {
                    if (userPacket == null)
                    {
                        userPacket = new UserPacket();
                        userPacket.server = netServer;
                        userPacket.netUser = this;
                        readStream.Position = 0;
                        readStream.Read(headDataOnce, 0, headSize);
                        readStream.Position = readStream.Length;
                        userPacket.size = BitConverter.ToUInt32(headDataOnce, 4);
                        userPacket.msgid = BitConverter.ToInt32(headDataOnce, 0);
                    }
                    if (userPacket.size <= readStream.Length)
                    {
                        userPacket.msgData = new byte[userPacket.size - headSize];
                        readStream.Position = headSize;
                        readStream.Read(userPacket.msgData, 0, userPacket.msgData.Length);
                        byte[] datalist = new byte[readStream.Length - readStream.Position];
                        readStream.Read(datalist, 0, datalist.Length);
                        readStream.SetLength(0);
                        readStream.Write(datalist, 0, datalist.Length);
                        UserPacket userPacket1 = userPacket;
                        userPacket = null;
                        netServer.AddPacket(userPacket1);
                    }
                }
            }
            catch (Exception ex)
            {

                Debug(ex.Message + "  " + ex.StackTrace + "  " + userPacket.size + " " + headSize);
            }
        }

        /// <summary>
        /// 添加用户组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T AddUserComponent<T>() where T : UserComponent, new()
        {
            System.Type componentName = typeof(T);
            T t = null;
            if (!userComponentDic.ContainsKey(componentName))
            {
                t = new T();
                t.netUser = this;
                t.OnAdd();
                userComponentDic[componentName] = t;
            }
            else
            {
                UserComponent userComponent = null;
                if (userComponentDic.TryGetValue(componentName, out userComponent))
                {
                    t = userComponent as T;
                }
            }

            return t;
        }

        /// <summary>
        /// 获得用户组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns></returns>
        public T GetUserComponent<T>() where T : UserComponent
        {
            var name = typeof(T);
            UserComponent userComponent = null;
            userComponentDic.TryGetValue(name, out userComponent);
            return userComponent as T;
        }

        /// <summary>
        /// 移除一个组件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveUserComponent<T>() where T : UserComponent
        {
            var name = typeof(T);
            UserComponent userComponent = null;
            userComponentDic.TryGetValue(name, out userComponent);
            if (userComponentDic.Remove(name))
            {
                if (userComponent != null)
                {
                    userComponent.OnRemove();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            foreach (var item in userComponentDic)
            {
                if (item.Value != null)
                    item.Value.OnRemove();
            }
            userComponentDic.Clear();
            readStream.Dispose();
            readStream = null;
            base.Dispose(disposing);
        }
    }
}
