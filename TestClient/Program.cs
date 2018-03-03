using System;
using System.Net.Sockets;
using Google.Protobuf;
using Game.Network;
using Model;

namespace TestClient
{
    class Program
    {
        private static GameClient client;

        static void Main(string[] args)
        {
            client = new GameClient();
            client.connectSucess += connectCompleted;
            client.connectError += connectError;
            client.ReceiveCompleted += ReceiveCompleted;
            client.ConnectAsync("127.0.0.1", 6650);

            Console.ReadKey();
        }

        private static void connectError(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine("连接失败");
        }

        private static void ReceiveCompleted(object sender, byte[] message)
        {
            Message msg = new Message();
            msg.MergeFrom(message);
            Console.WriteLine(msg.ToString());
        }

        private static void connectCompleted(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine("连接成功");
            Message data = new Message();
            data.Name = "张三";
            data.Age = 18;
            data.Gender = "妖";
            data.Job = 1;
            byte[] msg = data.ToByteArray();
            //测试同步发送
            for (int i = 0; i < 10; i++)
                client.Send(msg);
        }

    }
}
