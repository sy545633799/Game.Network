using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Network
{
    public class NetPackege
    {
        /// <summary>
        /// 这个包的关联客户端
        /// </summary>
        public BaseClient client;
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
}
