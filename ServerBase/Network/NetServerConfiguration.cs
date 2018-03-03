using Server.Base.Utils;
using System.Net;

namespace Server.Base.Server
{
    /// <summary>
    /// Provide properties to configuration a <see cref="NetServer"/>.
    /// </summary>
    public sealed class NetServerConfiguration
    {
        private readonly NetServer _server;
        private int _port;
        private int _backlog;
        private int _bufferSize;
        private int _maximumNumberOfConnections;
        private string _host;
        private string _outHost;

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int Port
        {
            get { return this._port; }
            set { this.SetValue(ref this._port, value); }
        }


        public string OutHost
        {
            get
            {
                return this._outHost;
            }
            set
            {
                this.SetValue(ref this._outHost, value);
            }
        }


        /// <summary>
        /// Gets or sets the host address.
        /// </summary>
        public string Host
        {
            get { return this._host; }
            set { this.SetValue(ref this._host, value); }
        }

        /// <summary>
        /// Gets or sets the listening backlog.
        /// </summary>
        public int Backlog
        {
            get { return this._backlog; }
            set { this.SetValue(ref this._backlog, value); }
        }

        /// <summary>
        /// Gets or sets the maximum number of simultaneous connections on the server.
        /// </summary>
        public int MaximumNumberOfConnections
        {
            get { return this._maximumNumberOfConnections; }
            set { this.SetValue(ref this._maximumNumberOfConnections, value); }
        }

        /// <summary>
        /// Gets or sets the buffer size.
        /// </summary>
        public int BufferSize
        {
            get { return this._bufferSize; }
            set { this.SetValue(ref this._bufferSize, value); }
        }

        /// <summary>
        /// Gets the listening address.
        /// </summary>
        internal IPAddress Address => NetUtils.GetIpAddress(this._host);

        /// <summary>
        /// Creates a new <see cref="NetServerConfiguration"/>.
        /// </summary>
        public NetServerConfiguration()
            : this(null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="NetServerConfiguration"/>.
        /// </summary>
        /// <param name="server">Server reference</param>
        internal NetServerConfiguration(NetServer server)
        {
            this._server = server;
            this.Port = 0;
            this.Host = null;
            this.MaximumNumberOfConnections = ushort.MaxValue;
            this.BufferSize = 4096;
        }

        /// <summary>
        /// Set the value of a property passed as reference.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="container"></param>
        /// <param name="value"></param>
        private void SetValue<T>(ref T container, T value)
        {
            if (this._server != null && this._server.IsRunning)
                throw new System.Exception("Cannot change configuration once the server is running.");

            if (!Equals(container, value))
                container = value;
        }
    }
}
