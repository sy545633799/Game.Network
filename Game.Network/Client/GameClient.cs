using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Game.Network
{
    public class GameClient: BaseClient
    {
        public IPEndPoint iPEndPoint { get; set; }
        ///// <summary>
        ///// 连接完成时引发事件。
        ///// </summary>
        public event EventHandler<SocketAsyncEventArgs> connectSucess;
        /// <summary>
        /// 连接发生错误
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> connectError;

        public GameClient()
            : base()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void ConnectAsync(string ip, int port)
        {
            //判断是否已连接
            if (IsConnected)
                throw new InvalidOperationException("已连接至服务器。");
            else
            {
                iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                StartConnect(null);
            }
        }

        private void StartConnect(SocketAsyncEventArgs e)
        {

            if (e == null)
            {
                e = new SocketAsyncEventArgs() { RemoteEndPoint = iPEndPoint };
                e.Completed += ConnectEventArg_Completed;
                e.UserToken = this;
            }

            bool willRaiseEvent = Socket.ConnectAsync(e);
            //如果没有挂起
            if (!willRaiseEvent)
                ProcessConnect(e);
        }

        private void ConnectEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(e);
        }

        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    connectSucess?.Invoke(this, e);
                    ReceiveAsync();
                }
                else
                    CloseClientSocket(e);
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
        }

        public void Connect(string ip, int port)
        {
            Socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
        }

        public void Send(byte[] buffer)
        {
            try
            {
                Socket.Send(Handler.ProcessWrite(buffer));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// 设置心跳
        /// </summary>
        private void SetHeartBeat()
        {
            //byte[] inValue = new byte[] { 1, 0, 0, 0, 0x20, 0x4e, 0, 0, 0xd0, 0x07, 0, 0 };// 首次探测时间20 秒, 间隔侦测时间2 秒
            byte[] inValue = new byte[] { 1, 0, 0, 0, 0x88, 0x13, 0, 0, 0xd0, 0x07, 0, 0 };// 首次探测时间5 秒, 间隔侦测时间2 秒
            Socket.IOControl(IOControlCode.KeepAliveValues, inValue, null);
        }
    }
}
