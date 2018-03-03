using NLog;
using Server.Base.Helper;
using Server.Base.Log;
using System;
using System.Net.Sockets;
namespace Server.Base
{
    /// <summary>
    /// Represents a network connection.
    /// </summary>
    public abstract class NetConnection : IDisposable
    {

        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly StackInfoDecorater decorater = new StackInfoDecorater();

        public virtual string Decorate(string message)
        {
            if (this.decorater == null)
            {
                return message;
            }
            return this.decorater.Decorate(message);
        }

        public void Trace(string message)
        {
            this.logger.Trace(this.Decorate(message));
        }

        public void Warning(string message)
        {
            this.logger.Warn(this.Decorate(message));
        }

        public void Info(string message)
        {
            this.logger.Info(this.Decorate(message));
        }

        public void Debug(string message)
        {
            this.logger.Debug(this.Decorate(message));
        }

        public void Error(System.Exception exception)
        {
            logger.Error(exception);
        }


        public void Error(string message)
        {
            this.logger.Error(this.Decorate(message));
        }
        private bool _disposedValue;
        /// <summary>
        /// 包头长度
        /// </summary>
        protected const int headSize = 8;

        protected byte[] headDataOnce = new byte[headSize];
        private Socket _socket;
        public System.Net.IPEndPoint iPEndPoint
        {
            private set;
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        public Socket Socket
        {
            get
            {
                return _socket;
            }
            internal set
            {
                _socket = value;
                if(_socket != null)
                iPEndPoint = _socket.RemoteEndPoint as System.Net.IPEndPoint;
            }
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="msgid">消息id</param>
        /// <param name="dataList">数据</param>
        public void Send(int msgid, byte[] dataList)
        {
            byte[] sendList = BuildPackage(msgid, dataList);
            Send(sendList);
        }


        /// <summary>
        /// 组包
        /// </summary>
        /// <param name="msgid">消息id</param>
        /// <param name="dataList">数据</param>
        public byte[] BuildPackage(int msgid, byte[] dataList)
        {
            uint countAll = (uint)(dataList.Length + headSize);
            byte[] sendList = new byte[countAll];
            sendList[0] = (byte)msgid;
            sendList[1] = (byte)(msgid >> 8);
            sendList[2] = (byte)(msgid >> 16);
            sendList[3] = (byte)(msgid >> 24);
            sendList[4] = (byte)countAll;
            sendList[5] = (byte)(countAll >> 8);
            sendList[6] = (byte)(countAll >> 16);
            sendList[7] = (byte)(countAll >> 24);
            System.Array.Copy(dataList, 0, sendList, 8, dataList.Length);
            return sendList;
        }

        /// <summary>
        /// 直接发送数据
        /// </summary>
        /// <param name="dataList">数据包</param>
        public void Send(byte[] dataList)
        {
            if (Socket == null)
            {
                return;
            }
            if (!Socket.Connected)
                return;
            try
            {
                Socket.Send(dataList);
            }
            catch (Exception ex)
            {

                Error(ex.Message);
            }
            
        }

        /// <summary>
        /// 强制断开连接
        /// </summary>
        public void ForceClose()
        {
            Debug("强制断开连接");
            if (Socket != null)
            {
                Socket.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    this.Socket.Dispose();
                    this.Socket = null;
                }

                _disposedValue = true;
            }
        }
    }
}
