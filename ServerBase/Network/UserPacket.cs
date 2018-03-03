using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Base.Packets
{
    /// <summary>
    /// 接受到的包
    /// </summary>
    public class UserPacket
    {
        /// <summary>
        /// 发送这个包的用户
        /// </summary>
        public NetUser netUser;
        /// <summary>
        /// 接受到这个包的服务器
        /// </summary>
        public NetServer server;
        /// <summary>
        /// 包长度
        /// </summary>
        public uint size;
        /// <summary>
        /// 消息号
        /// </summary>
        public int msgid;
        /// <summary>
        /// 内容
        /// </summary>
        public byte[] msgData;
    }

    /// <summary>
    /// 接受到的包
    /// </summary>
    public class ClientPacket
    {
        /// <summary>
        /// 接受到这个包的服务器
        /// </summary>
        public NetClient client;
        /// <summary>
        /// 包长度
        /// </summary>
        public uint size;
        /// <summary>
        /// 消息号
        /// </summary>
        public int msgid;
        /// <summary>
        /// 内容
        /// </summary>
        public byte[] msgData;
    }

    /// <summary>
    /// 接受到的包
    /// </summary>
    public class APIProvidePacket
    {
        /// <summary>
        /// 接收到的消息
        /// </summary>
        public string ReqMessage;
        /// <summary>
        /// 内容
        /// </summary>
        public string msgData;
    }
}
