using Server.Base.Packets;
using Server.Base.Server;
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
    /// Creates a new TCP managed server.
    /// </summary>
    public class NetServer : NetConnection, IDisposable
    {
        private static readonly string AllInterfaces = "0.0.0.0";
        private readonly ConcurrentDictionary<int, NetUser> _clients = new ConcurrentDictionary<int, NetUser>();
        private Dictionary<int, Action<UserPacket>> eventDic = new Dictionary<int, Action<UserPacket>>();
        private bool _isDisposed;

        /// <summary>
        /// Gets the <see cref="NetServer"/> configuration
        /// </summary>
        public NetServerConfiguration Configuration { get; }

        /// <summary>
        /// 获得运行状态
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public event Action<NetUser> OnClientConnectedEvent;
        /// <summary>
        /// 
        /// </summary>
        public event Action<NetUser> OnClientDisconnectedEvent;
        /// <summary>
        /// 
        /// </summary>
        public event Action<UserPacket> OnUserPacketEvent;
        /// <summary>
        /// 
        /// </summary>
        public byte[] IPPointByteList { get; private set; }

        public IPEndPoint myPoint { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public NetServer()
        {
            this.Configuration = new NetServerConfiguration(this);
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Destroys the <see cref="NetServer"/> instance.
        /// </summary>
        ~NetServer()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="event"></param>
        public void AddReceiveEvent(int msgid, System.Action<UserPacket> @event)
        {
            if (eventDic.ContainsKey(msgid))
            {
                Warning("添加重复消息号--->" + msgid);
                return;
            }
            eventDic[msgid] = @event;
        }

        /// <summary>
        /// Initialize and start the server.
        /// </summary>
        public void Start()
        {
            if (this.IsRunning)
                throw new InvalidOperationException("Server is already running.");

            if (this.Configuration.Port <= 0)
                throw new Exception($"{this.Configuration.Port} is not a valid port.");

            var address = this.Configuration.Host == AllInterfaces || Configuration.Host == null ? IPAddress.Any : this.Configuration.Address;
            if (address == null)
                throw new Exception($"Invalid host : {this.Configuration.Host}");
            myPoint = new IPEndPoint(address, this.Configuration.Port);
            this.Socket.Bind(myPoint);
            this.Socket.Listen(this.Configuration.Backlog);
            Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            this.StartAccept(NetUtils.CreateSocketAsync(null, -1, this.IO_Completed));
            this.IsRunning = true;
            Debug("服务器监听-->" + myPoint.ToString() + "<-----");
            IPPointByteList = myPoint.Address.GetAddressBytes();
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            if (this.IsRunning)
            {
                this.IsRunning = false;
            }
        }

        /// <summary>
        /// Disconnects the client from this server.
        /// </summary>
        /// <param name="clientId">Client unique Id</param>
        public void DisconnectClient(int clientId)
        {
            if (!this._clients.ContainsKey(clientId))
                Debug(clientId + "不存在");

            if (this._clients.TryRemove(clientId, out NetUser removedClient))
            {
                this.OnClientDisconnected(removedClient);
                removedClient.Dispose();
            }
        }

        /// <summary>
        /// Triggered when a new client is connected to the server.
        /// </summary>
        /// <param name="connection"></param>
        private void OnClientConnected(NetUser connection)
        {
            try
            {
                OnClientConnectedEvent?.Invoke(connection);
            }
            catch (Exception ex)
            {

                Debug(ex.Message + " -- " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Triggered when a client disconnects from the server.
        /// </summary>
        /// <param name="connection"></param>
        private void OnClientDisconnected(NetUser connection)
        {
            OnClientDisconnectedEvent?.Invoke(connection);
        }

        /// <summary>
        /// Triggered when an error occurs on the server.
        /// </summary>
        /// <param name="exception">Exception</param>
        protected void OnError(Exception exception)
        {
            Error(exception.Message);
        }

        /// <summary>
        /// Starts the accept connection async operation.
        /// </summary>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket != null)
                e.AcceptSocket = null;

            if (!this.Socket.AcceptAsync(e))
                this.ProcessAccept(e);
        }

        /// <summary>
        /// Process the accept connection async operation.
        /// </summary>
        /// <param name="e"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    // SocketAsyncEventArgs readArgs = this._readPool.Pop();
                    //if(readArgs == null)

                    SocketAsyncEventArgs readArgs = NetUtils.CreateSocketAsync(null, this.Configuration.BufferSize, this.IO_Completed);

                    var client = new NetUser()
                    {
                        netServer = this,
                        Socket = e.AcceptSocket,
                        Id = e.AcceptSocket.RemoteEndPoint.GetHashCode(),
                    };
                    if (!this._clients.TryAdd(client.Id, client))
                        throw new Exception($"Client {client.Id} already exists in client list.");
                    Debug("上线-->" + e.AcceptSocket.RemoteEndPoint.ToString());
                    this.OnClientConnected(client);
                    readArgs.UserToken = client;
                    if (!e.AcceptSocket.ReceiveAsync(readArgs))
                        this.ProcessReceive(readArgs);

                }
            }
            catch (Exception exception)
            {
                Debug(exception.Message + "  " + exception.StackTrace);
                this.OnError(exception);
            }
            finally
            {
                this.StartAccept(e);
            }
        }

        /// <summary>
        /// Process the send async operation.
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {

        }

        internal void AddPacket(UserPacket userPacket)
        {
            if (userPacket != null)
            {
                System.Action<UserPacket> @event = null;

                if (eventDic.TryGetValue(userPacket.msgid, out @event))
                {
                    @event(userPacket);
                }
                else
                {
                    if (OnUserPacketEvent != null)
                    {
                        OnUserPacketEvent(userPacket);
                    }
                }
            }
        }

        /// <summary>
        /// Process receieve.
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                var connection = e.UserToken as NetUser;
                try
                {
                    connection.Write(e);
                }
                catch (Exception ex)
                {

                    Debug(ex.Message + "  " + ex.StackTrace);
                }
                finally
                {
                    if (!connection.Socket.ReceiveAsync(e))
                        this.ProcessReceive(e);
                }
            }
            else
                this.CloseConnection(e);
        }

        /// <summary>
        /// Close the connection.
        /// </summary>
        /// <param name="e"></param>
        private void CloseConnection(SocketAsyncEventArgs e)
        {
            try
            {
                var connection = e.UserToken as NetUser;
                Debug("下线-->  " + connection.iPEndPoint);
                DisconnectClient(connection.Id);
            }
            catch (Exception ex)
            {

                Debug(ex.Message + "" + ex.StackTrace);
            }
        }


        /// <summary>
        /// Triggered when a <see cref="SocketAsyncEventArgs"/> async operation is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    this.ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    this.ProcessSend(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    break;
                default:
                    Debug(e.LastOperation + "  :  Unexpected SocketAsyncOperation.");
                    break;
            }
        }

        /// <summary>
        /// Dispose the <see cref="NetServer"/> resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!this._isDisposed)
            {
                foreach (var client in this._clients)
                    client.Value.Dispose();
                this._clients.Clear();
                this._isDisposed = true;
            }
            else
                throw new ObjectDisposedException(nameof(NetServer));
            base.Dispose(disposing);
        }
    }
}
