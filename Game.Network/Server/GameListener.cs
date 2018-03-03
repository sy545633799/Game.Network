using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Game.Network
{
    public class GameListener
    {
        private Socket listenSocket;

        private int m_numConnections; //最大支持连接个数
        private Semaphore m_maxNumberAcceptedClients; //限制访问接收连接的线程数，用来控制最大并发数
        private ClientPool<UserToken> m_UserTokenPool;

        public int SocketTimeOutMS { get; set; }

        //心跳与断线重连
        //private DaemonThread m_daemonThread;

        /// <summary>
        /// 接收完成时引发事件。
        /// </summary>
        public event EventHandler<byte[]> ReceiveCompleted;
        /// <summary>
        /// 发送完成时引发事件。
        /// </summary>
        public event EventHandler<byte[]> SendCompleted;
        /// <summary>
        /// 接受客户完成时引发事件。
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> AcceptCompleted;
        /// <summary>
        /// 客户断开完成时引发事件。
        /// </summary>
        public event EventHandler<SocketAsyncEventArgs> DisconnectCompleted;
        /// <summary>
        /// 发生错误时引发的事件
        /// </summary>
        public event EventHandler<Exception> OnClientError;

        public GameListener(int numConnections)
        {
            m_numConnections = numConnections;

            m_UserTokenPool = new ClientPool<UserToken>(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);

            for (int i = 0; i < m_numConnections; i++) //按照连接数建立读写对象
            {
                UserToken userToken = new UserToken();
                m_UserTokenPool.Push(userToken);
            }
        }

        public GameListener()
        {
        }

        public void Start(int port)
        {
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, port);
            listenSocket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(iPEndPoint);
            listenSocket.Listen(m_numConnections);

            try
            {
                StartAccept(null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
           
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
                acceptEventArgs.AcceptSocket = null; //释放上次绑定的Socket，等待下一个Socket连接

            m_maxNumberAcceptedClients.WaitOne(); //获取信号量

            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArgs);
            //如果没有挂起
            if (!willRaiseEvent)
                ProcessAccept(acceptEventArgs);
            
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                ProcessAccept(acceptEventArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            UserToken userToken = m_UserTokenPool.Pop();
            userToken.Socket = acceptEventArgs.AcceptSocket;
            acceptEventArgs.UserToken = userToken;
            AcceptCompleted?.Invoke(this, acceptEventArgs);
            //添加事件
            userToken.ReceiveCompleted += client_ReceiveCompleted;
            userToken.SendCompleted += client_SendCompleted;
            userToken.DisconnectCompleted += client_DisconnectCompleted;
            //开始接收数据
            userToken.ReceiveAsync();
            //把当前异步事件释放，等待下次客户端连接
            StartAccept(acceptEventArgs);

        }

        //客户端断开连接
        private void client_DisconnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            Game.Network.UserToken userToken = e.UserToken as UserToken;
            //移除事件
            userToken.DisconnectCompleted -= client_DisconnectCompleted;
            userToken.ReceiveCompleted -= client_ReceiveCompleted;
            userToken.SendCompleted -= client_SendCompleted;
            DisconnectCompleted?.Invoke(this, e);
            //移除客户端
            m_UserTokenPool.Push(userToken);
            //增加一个信号量
            m_maxNumberAcceptedClients.Release();
        }

        //收到客户端发送的数据
        private void client_ReceiveCompleted(object sender, byte[] e)
        {
            ReceiveCompleted?.Invoke(this, e);
        }

        //向客户端发送数据完成
        private void client_SendCompleted(object sender, byte[] e)
        {
            SendCompleted?.Invoke(this, e);
        }

        private void client_OnError(object sender, Exception e)
        {
            OnClientError?.Invoke(this, e);
        }
    }
}
