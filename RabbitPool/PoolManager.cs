using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace RabbitPool
{
    /// <summary>
    /// Connection Pool management for RabbitMQ.
    /// Ensures that the numberof connections and the number of connections per queue is not exceeded.
    /// If connections are exceeded then connections are pruned of non-open connections and if needed
    /// the oldest connection is disposed of to make room for the new.
    /// </summary>
    public class PoolManager
    {
        private int _channelCount;
        private readonly List<IConnection> _connections = new();
        private readonly int _maxConnections;
        private readonly int _maxChannelsPerConnection;
        private readonly ConnectionFactory _factory;
        
        public IEnumerable<IConnection> Connections => _connections;
        public int ChannelCount => _channelCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">Connection details for RabbitMQ</param>
        /// <param name="maxConnections">The maximum number of connections allowed by the pool</param>
        /// <param name="maxChannelsPerConnection">The maximum number of channels/models per connection</param>
        public PoolManager(RabbitConnectionOptions connection, int maxConnections=25, int maxChannelsPerConnection=500):this( new ConnectionFactory
        {
            HostName = connection.HostName,
            UserName = connection.UserName,
            Password = connection.Password,
            Port = connection.Port,
            Ssl = connection.SslOption ?? new SslOption{Enabled = false},
            ContinuationTimeout = connection.ContinuationTimeout,
            AutomaticRecoveryEnabled = false
        }, maxConnections, maxChannelsPerConnection)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory">ConnectionFactory in case additional options are needed</param>
        /// <param name="maxConnections">The maximum number of connections allowed by the pool</param>
        /// <param name="maxChannelsPerConnection">The maximum number of channels/models per connection</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public PoolManager(ConnectionFactory factory, int maxConnections = 25, int maxChannelsPerConnection = 500)
        {
            _factory = factory;
            _maxConnections = maxConnections;
            _maxChannelsPerConnection = maxChannelsPerConnection;
        }

        public int ConnectionCount => _connections?.Count ?? 0;

        private IConnection StartConnection()
        {
            _channelCount = 0;
            var conn = _factory.CreateConnection();
            conn.ConnectionShutdown += (s, e) =>
            {
                _connections.Remove(conn);
            };
            return conn;
        }

        /// <summary>
        /// Gets a new channel spawning a new connection if the connectionPool has reached the maxConnections for the last channel
        /// </summary>
        /// <returns></returns>
        public IModel GetChannel()
        {
            //if there is no connection create one.
            if (_connections.Count == 0) _connections.Insert(0, StartConnection());
            if (_connections.First().IsOpen)
            {
                if (_channelCount >= _maxChannelsPerConnection)
                {
                    if (_connections.Count >= _maxConnections)
                    {
                        //If still greater than maxConnections then close one
                        if (_connections.Count >= _maxConnections)
                        {
                            var lastConnection = _connections.Last();
                            lastConnection.Close();
                            _connections.Remove(lastConnection);
                        }
                    }
                    _connections.Insert(0, StartConnection());
                }
                var model = _connections.First().CreateModel();
                model.ModelShutdown += (sender, args) => _channelCount--;
                _channelCount++;
                return model;
            }
            _connections.Remove(_connections.First());
            return GetChannel();
        }

        private void PruneClosedConnections()
        {
            var toBeRemoved = new List<IConnection>();
            foreach (var connection in _connections)
            {
                if (!connection.IsOpen)
                {
                    connection.Dispose();
                    toBeRemoved.Add(connection);
                }
            }
            foreach (var item in toBeRemoved)
                _connections.Remove(item);
        }
    }
}
