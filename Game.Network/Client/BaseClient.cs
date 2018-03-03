using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Game.Network
{
    public class BaseClient
    {
        protected DynamicBufferManager Handler;
        public DateTime ConnectDateTime { get; protected set; }
        public DateTime ActiveDateTime { get; protected set; }
        public SocketAsyncEventArgs sendEventArgs { protected set; get; }
        public SocketAsyncEventArgs receiveEventArgs { protected set; get; }
        public Socket Socket { get; set; }
        /// <summary>
        /// 获取是否已连接。
        /// </summary>
        public bool IsConnected { get { return Socket != null && Socket.Connected; } }
        private Queue<byte[]> sendQueue = new Queue<byte[]>();
        private bool isSending;

        #region 事件

        ///// <summary>
        ///// 断开完成时引发事件。
        ///// </summary>
        public event EventHandler<SocketAsyncEventArgs> DisconnectCompleted;
        /// <summary>
        /// 发生错误时引发的事件
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> ServerShutDown;
        /// <summary>
        /// 客户端发生错误时引发的事件
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> ClientInternetError;
        ///// <summary>
        ///// 接收完成时引发事件。
        ///// </summary>
        public event EventHandler<byte[]> ReceiveCompleted;
        ///// <summary>
        ///// 发送完成时引发事件。
        ///// </summary>
        public event EventHandler<byte[]> SendCompleted;

        #endregion

        public BaseClient()
        {
            Handler = new DynamicBufferManager();

            receiveEventArgs = new SocketAsyncEventArgs();
            receiveEventArgs.Completed += IO_Completed;
            receiveEventArgs.UserToken = this;
            receiveEventArgs.SetBuffer(new byte[1024], 0, 1024);

            sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.Completed += IO_Completed;
            sendEventArgs.UserToken = this;
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            try
            {
                lock (this)
                {
                    if (asyncEventArgs.LastOperation == SocketAsyncOperation.Receive)
                        ProcessReceive(asyncEventArgs);
                    else if (asyncEventArgs.LastOperation == SocketAsyncOperation.Send)
                        ProcessSend(asyncEventArgs);
                    else
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (Exception e)
            {
                CloseClientSocket(asyncEventArgs);
            }
        }

        public void ReceiveAsync()
        {
            try
            {
                //用户连接对象 开启异步数据接收
                bool result = Socket.ReceiveAsync(receiveEventArgs);
                //异步事件是否挂起
                if (!result)
                {
                    lock (Socket)
                    {
                        ProcessReceive(receiveEventArgs);
                    }
                }
            }
            catch (Exception e)
            {
                CloseClientSocket(receiveEventArgs);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            if (Socket == null)
                return;
            ActiveDateTime = DateTime.Now;
            if (receiveEventArgs.SocketError == SocketError.Success)
            {
                if (receiveEventArgs.BytesTransferred > 0)
                {
                    Handler.ProcessRead(receiveEventArgs.Buffer, receiveEventArgs.BytesTransferred, ReceiveCompleted, this);
                    
                    ReceiveAsync();
                }
                else
                    CloseClientSocket(receiveEventArgs);
            }
            else
                CloseClientSocket(receiveEventArgs);
        }

        public void SendAsync(byte[] buffer)
        {
            if (!IsConnected) return;
            sendQueue.Enqueue(buffer);
            if (!isSending)
            {
                isSending = true;
                OnSending();
            }
        }

        private void OnSending()
        {
            try
            {
                if (sendQueue.Count == 0 || !IsConnected)
                {
                    isSending = false;
                    return;
                }
                byte[] buffer = sendQueue.Dequeue();
                //设置消息发送异步对象的发送数据缓冲区数据
                byte[] message = Handler.ProcessWrite(buffer);
                sendEventArgs.SetBuffer(message, 0, message.Length);
                //开启异步发送
                bool result = Socket.SendAsync(sendEventArgs);
                //是否挂起
                if (!result)
                    ProcessSend(sendEventArgs);
            }
            catch (Exception e)
            {
                CloseClientSocket(sendEventArgs);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendCompleted?.Invoke(this, null);
                OnSending();
            }
            else
                CloseClientSocket(e);
        }

        protected void CloseClientSocket(SocketAsyncEventArgs e)
        {
            Close(() => 
            {
                if (e.SocketError == SocketError.ConnectionReset)
                {
                    if (this is GameClient)
                    {
                        ServerShutDown?.Invoke(this, e);
                        Console.WriteLine("服务器主动断开");
                    }
                    else
                        Console.WriteLine("客户端异常断开");
                }
                if (e.SocketError == SocketError.ConnectionAborted)
                {
                    Console.WriteLine("网络断开");
                    ClientInternetError?.Invoke(this, e);
                }
                if (e.SocketError == SocketError.Success)
                    Console.WriteLine("客户端正常断开");
                DisconnectCompleted?.Invoke(this, e);
            });
        }

        public void Close(Action callback = null)
        {
            if (Socket == null)
                return;
            try
            {
                if (IsConnected)
                    Socket.Shutdown(SocketShutdown.Both);
                if (Socket != null)
                {
                    Socket.Close();
                    if (Socket != null) Socket.Dispose();
                    Socket = null; //释放引用，并清理缓存，包括释放协议对象等资源
                }
                sendQueue.Clear();
                isSending =false;
                callback?.Invoke();
                
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

    }
}
