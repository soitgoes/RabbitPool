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
        private int channelCount = 0;
        private List<IConnection> connections = new List<IConnection>();
        private readonly int maxConnections;
        private readonly int maxChannelsPerConnection;
        private readonly ConnectionFactory factory;
        
        
        public IEnumerable<IConnection> Connections => connections;
        public int ChannelCount => channelCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">Connection details for RabbitMQ</param>
        /// <param name="maxConnections">The maximum number of connections allowed by the pool</param>
        /// <param name="maxChannelsPerConnection">The maximum number of channels/models per connection</param>
        public PoolManager(RabbitConnectionOptions connection, int maxConnections=25, int maxChannelsPerConnection=500)
        {
            this.maxConnections = maxConnections;
            this.maxChannelsPerConnection = maxChannelsPerConnection;
            factory = new ConnectionFactory
            {
                HostName = connection.HostName,
                UserName = connection.UserName,
                Password = connection.Password,
                Port = connection.Port,
                Ssl = connection.SslOption,
                ContinuationTimeout = connection.ContinuationTimeout,
                AutomaticRecoveryEnabled = false
            };
        }

        public int ConnectionCount { get { return connections?.Count ?? 0; } }
       
        private IConnection StartConnection()
        {
            channelCount = 0;
            return factory.CreateConnection();
        }

        /// <summary>
        /// Gets a new channel spawning a new connection if the connectionPool has reached the maxConnections for the last channel
        /// </summary>
        /// <returns></returns>
        public IModel GetChannel()
        {
            //if there is no connection create one.
            if (connections.Count == 0) connections.Insert(0, StartConnection());
            if (connections.First().IsOpen)
            {

                if (channelCount >= maxChannelsPerConnection)
                {
                    if (connections.Count >= maxConnections)
                    {
                        //Prune closed connections and reset pointer
                        PruneClosedConnections();
                        //If still greater than maxConnections then close one
                        if (connections.Count >= maxConnections)
                        {
                            var lastConnection = connections.Last();
                            lastConnection.Close();
                            connections.Remove(lastConnection);
                        }
                    }
                    connections.Insert(0, StartConnection());
                }
                var model = connections.First().CreateModel();
                channelCount++;
                return model;
            }
            else
            {
                connections.Remove(connections.First());
                return GetChannel();
            }

        }

        private void PruneClosedConnections()
        {
            var toBeRemoved = new List<IConnection>();
            foreach (var connection in connections)
            {
                if (!connection.IsOpen)
                {
                    connection.Dispose();
                    toBeRemoved.Add(connection);
                }
            }

            foreach (var item in toBeRemoved)
            {
                connections.Remove(item);
            }
            
        }
    }
}
