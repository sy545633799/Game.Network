using Server.Base.Packets;
using Server.Base.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server.Base
{
    /// <summary>
    /// Managed TCP client.
    /// </summary>
    public class NetClient : NetConnection, IDisposable
    {
        private readonly IPEndPoint _ipEndPoint;
        private readonly SocketAsyncEventArgs _socketReceiveArgs;
        private readonly SocketAsyncEventArgs _socketSendArgs;
        private readonly AutoResetEvent _autoConnectEvent;
        private MemoryStream readStream = new MemoryStream();
        private ConcurrentQueue<ClientPacket> userPacketQueur = new ConcurrentQueue<ClientPacket>();
        private Dictionary<int, Action<ClientPacket>> eventDic = new Dictionary<int, Action<ClientPacket>>();
        private Thread businessThread;
        private bool _isDisposed;

        /// <summary>
        /// Gets the <see cref="NetClient"/> connected state.
        /// </summary>
        public bool IsConnected => this.Socket != null && this.Socket.Connected;
        /// <summary>
        /// 
        /// </summary>
        public event Action OnClientConnectedEvent;

        /// <summary>
        /// 
        /// </summary>
        public event Action OnClientDisconnectedEvent;

        /// <summary>
        /// Creates a new <see cref="NetClient"/> instance.
        /// </summary>
        /// <param name="host">Remote host or ip</param>
        /// <param name="port">Remote port</param>
        /// <param name="bufferSize">Buffer size</param>
        public NetClient(string host, int port, int bufferSize)
        {
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            this._ipEndPoint = NetUtils.CreateIpEndPoint(host, port);
            this._socketSendArgs = NetUtils.CreateSocketAsync(this.Socket, -1, this.IO_Completed);
            this._socketReceiveArgs = NetUtils.CreateSocketAsync(this, bufferSize, this.IO_Completed);
            _autoConnectEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Connect to the remote host.
        /// </summary>
        public void Connect()
        {
            if (this.IsConnected)
                throw new InvalidOperationException("Client is already connected to remote.");

            var connectSocket = NetUtils.CreateSocketAsync(this.Socket, -1, this.IO_Completed);
            connectSocket.RemoteEndPoint = this._ipEndPoint;

            this.Socket.ConnectAsync(connectSocket);
            _autoConnectEvent.WaitOne();
            SocketError errorCode = connectSocket.SocketError;
            if (errorCode != SocketError.Success)
                throw new SocketException((Int32)errorCode);

            if (!this.Socket.ReceiveAsync(this._socketReceiveArgs))
                this.ProcessReceive(this._socketReceiveArgs);
        }

        /// <summary>
        /// Disconnects the <see cref="NetClient"/>.
        /// </summary>
        public void Disconnect()
        {
            if (this.IsConnected)
            {
                this.Socket.Close();
            }
        }


        /// <summary>
        /// Triggered when the client is connected to the remote end point.
        /// </summary>
        protected void OnConnected()
        {
            businessThread = new Thread(BusinessQueue);
            businessThread.IsBackground = true;
            businessThread.Start();
            OnClientConnectedEvent?.Invoke();
        }

        /// <summary>
        /// 业务线程
        /// </summary>
        private void BusinessQueue()
        {
            while (true)
            {
                ClientPacket userPacket = null;
                if (userPacketQueur.TryDequeue(out userPacket))
                {
                    if (userPacket != null)
                    {
                        Action<ClientPacket> @event = null;
                        if (eventDic.TryGetValue(userPacket.msgid, out @event))
                        {
                            @event(userPacket);
                        }
                    }
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="event"></param>
        public void AddReceiveEvent(int msgid, Action<ClientPacket> @event)
        {
            if (eventDic.ContainsKey(msgid))
            {
                Warning("添加重复消息号--->" + msgid);
                return;
            }
          
            eventDic[msgid] = @event;
        }

        /// <summary>
        /// Triggered when the client is disconnected from the remote end point.
        /// </summary>
        protected  void OnDisconnected()
        {
            OnClientDisconnectedEvent?.Invoke();
        }


        /// <summary>
        /// Process receieve.
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                readStream.Write(e.Buffer, e.Offset, e.BytesTransferred);
                ClientPacket userPacket = null;
                while (readStream.Length > headSize)
                {
                    if (userPacket == null)
                    {
                        userPacket = new ClientPacket();
                        userPacket.client = this;
                        readStream.Position = 0;
                        readStream.Read(headDataOnce, 0, headSize);
                        readStream.Position = readStream.Length;
                        userPacket.size =  BitConverter.ToUInt32(headDataOnce, 4);
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
                        userPacketQueur.Enqueue(userPacket);
                        userPacket = null;
                    }
                }
                if (!Socket.ReceiveAsync(e))
                    this.ProcessReceive(e);
            }
        }

        /// <summary>
        /// Triggered when a <see cref="SocketAsyncEventArgs"/> async operation is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    this.OnConnected();
                    _autoConnectEvent.Set();
                    break;
                case SocketAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    this.OnDisconnected();
                    break;
            }

        }

        /// <summary>
        /// Dispose the <see cref="NetClient"/> instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!this._isDisposed)
            {
                this.Disconnect();
            }

            this._isDisposed = true;

            base.Dispose(disposing);
        }
    }
}