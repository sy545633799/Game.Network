using System.Net;
using System.Linq;
using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Server.Base.Utils
{
    internal static class NetUtils
    {
        public static IPAddress GetIpAddress(string ipOrHost)
        {
            var host = Dns.GetHostAddressesAsync(ipOrHost).Result.First().ToString();

            return IPAddress.TryParse(host, out IPAddress address) ? address : null;
        }

        public static IPEndPoint CreateIpEndPoint(string ipOrHost, int port)
        {
            IPAddress address = GetIpAddress(ipOrHost);

            if (address == null)
                throw new Exception($"Invalid host or ip address: {ipOrHost}.");
            if (port <= 0)
                throw new Exception($"Invalid port: {port}");

            return new IPEndPoint(address, port);
        }

        public static byte[] GetPacketBuffer(byte[] bufferSource, int offset, int size)
        {
            var buffer = new byte[size];

            Buffer.BlockCopy(bufferSource, offset, buffer, 0, size);

            return buffer;
        }

        public static SocketAsyncEventArgs CreateSocketAsync(object userToken, int bufferSize, EventHandler<SocketAsyncEventArgs> completedAction)
        {
            var socketAsync = new SocketAsyncEventArgs
            {
                UserToken = userToken
            };

            socketAsync.Completed += completedAction;
            if (bufferSize > 0)
                socketAsync.SetBuffer(new byte[bufferSize], 0, bufferSize);

            return socketAsync;
        }

        public static string[] GetAddressIPs()
        {
            //获取本地的IP地址
            List<string> addressIPs = new List<string>();
            foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (address.AddressFamily.ToString() == "InterNetwork")
                {
                    addressIPs.Add(address.ToString());
                }
            }
            return addressIPs.ToArray();
        }
    }
}
