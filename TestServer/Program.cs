using System;
using Model;
using Game.Network;
using Google.Protobuf;
using System.Net.Sockets;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            GameListener listener = new GameListener(10);
            listener.AcceptCompleted += AcceptCompleted;
            listener.ReceiveCompleted += ReceiveCompleted;
            listener.DisconnectCompleted += DisconnectCompleted;
            listener.Start(6650);

            Console.ReadKey();
        }
        private static void DisconnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine("客户端断开：" + e.SocketError);
        }

        private static void ReceiveCompleted(object sender, byte[] message)
        {
            Message msg = new Message();
            msg.MergeFrom(message);
            Console.WriteLine("receive:" + msg.ToString());
            token.SendAsync(msg.ToByteArray());
        }

        private static UserToken token;
        private static void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine("客户端连接");
            token = e.UserToken as UserToken;
        }
    }
}
