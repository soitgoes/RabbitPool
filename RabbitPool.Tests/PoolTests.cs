using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using RabbitMQ.Client;
using Xunit;

namespace RabbitPool.Tests
{
    public class PoolTests
    {
        [Fact]
        public void ShouldUpdateChannelCountIfModelIsDisposed()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 5, 10);
            var model = pool.GetChannel();
            var model2 = pool.GetChannel();
            var model3 = pool.GetChannel();
            
            model3.Dispose();
            Assert.Equal(2u, pool.ChannelCount);
        }

        [Fact]
        public void ShouldUpdateChannelCountIfConnectionDisposed()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 1, 10);
            var model = pool.GetChannel();
            var model2 = pool.GetChannel();
            var model3 = pool.GetChannel();
            var oldestConnection = pool.Connections.LastOrDefault();
            oldestConnection.Dispose();
            Assert.Equal(2u, pool.ConnectionCount);
        }

        [Fact]
        public void ShouldUpdateConnectionCountIfConnectionDisposed()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 3, 10);
            9.Times(() => pool.GetChannel()); 
            var oldestConnection = pool.Connections.LastOrDefault();
            oldestConnection?.Dispose();
            Assert.Equal(2u, pool.ConnectionCount);
        }

        [Fact]
        public void ShouldCreateNewConnectionIfModelCountExceeded()
        { 
            var pool = new PoolManager(new RabbitConnectionOptions(), 3, 10);
            9.Times(() => pool.GetChannel()); 
            Assert.Equal(3u, pool.ConnectionCount);
        }
 
        [Fact]
        public void ShouldDisposeAllConnectionsIfMaxConnectionExceeded()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 3, 3);
            9.Times(() => pool.GetChannel()); 
            Assert.Equal(1u, pool.ConnectionCount);
        }

        [Fact]
        public void ShouldWorkOnLargeNumber()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 5, 100);
            200.Times(() => pool.GetChannel());
            Assert.Equal(40,  pool.ConnectionCount);
        }
    }
}
