using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace RabbitPool
{
    /// <summary>
    /// Connection Pool management for RabbitMQ.
    /// Ensures that the number of connections and the number of connections per queue is not exceeded.
    /// If connections are exceeded then connections are pruned of non-open connections and if needed
    /// the oldest connection is disposed of to make room for the new.
    /// </summary>
    public class PoolManager : IDisposable
    {
        private readonly SafeList<IConnection> _connections = new();
        private readonly uint _maxChannelsPerConnection;
        private readonly ConnectionFactory _factory;
        private uint _channelCount =0u;
        private uint _totalChannelCount = 0u;
        private uint _connectionCount=0u;
        private readonly uint _maxConnections=0u;

        public uint ChannelCount => _totalChannelCount;
        public uint ConnectionCount => _connectionCount;

        public IEnumerable<IConnection> Connections => _connections;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">Connection details for RabbitMQ</param>
        /// <param name="maxChannelsPerConnection">The maximum number of channels/models per connection</param>
        /// <param name="maxConnections">The maximum number of connections before cycling</param>
        public PoolManager(RabbitConnectionOptions connection,  uint maxChannelsPerConnection=500,uint maxConnections=25):this( new ConnectionFactory
        {
            HostName = connection.HostName,
            UserName = connection.UserName,
            Password = connection.Password,
            Port = connection.Port,
            Ssl = connection.SslOption ?? new SslOption{Enabled = false},
            ContinuationTimeout = connection.ContinuationTimeout,
            AutomaticRecoveryEnabled = false
        }, maxChannelsPerConnection,maxConnections)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory">ConnectionFactory in case additional options are needed</param>
        /// <param name="maxChannelsPerConnection">The maximum number of channels/models per connection</param>
        /// <param name="maxConnections">The maximum number of connections before cycling</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public PoolManager(ConnectionFactory factory, uint maxChannelsPerConnection = 500, uint maxConnections = 25)
        {
            _factory = factory;
            _maxConnections = maxConnections;
            _maxChannelsPerConnection = maxChannelsPerConnection;
        }

        private  IConnection StartConnection()
        {
            var conn = _factory.CreateConnection();
            conn.ConnectionShutdown += (s, e) =>
            {
                _connectionCount--;
            };
            _connections.Enqueue(conn);
            _connectionCount++;
            _channelCount = 0;
            return conn;
        }

        /// <summary>
        /// Gets a new channel spawning a new connection if the connectionPool has reached the maxConnections for the last channel
        /// </summary>
        /// <returns></returns>
        public IModel GetChannel()
        {
            if (_connectionCount >= _maxConnections)
            {
                //if we get here let's just close all the connections that aren't running.
                //If we do get here we have something wrong with our app that needs addressing but we should pick that up during monitoring
                //and in the meantime I don't want to fail fatally
                foreach (var connToClose in _connections)
                {
                    try
                    {
                        connToClose.Close();
                    }
                    catch (Exception)
                    {
                        //eat it we were throwing it out anyways
                    }
                }
                _connections.Empty();
                _channelCount = 0;
                _connectionCount = 0;
                _totalChannelCount = 0;
            }
            var conn = _connections.IsEmpty() || _channelCount >= _maxChannelsPerConnection ? StartConnection() : _connections.Tail();
            if (!conn.IsOpen) conn = StartConnection();
            var model = conn.CreateModel();
            model.ModelShutdown += (s, e) =>
            {
                //Can i find and eliminate the connection if it doesn't have any channels, Nope
                _totalChannelCount--;
            };
            _channelCount++;
            _totalChannelCount++;
            return model;
        }

        public void Dispose()
        {
            foreach (var conn in _connections)
                conn.Close();
            _connections.Empty();
        }
    }
}
