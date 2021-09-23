using System;
using System.Linq;
using Xunit;

namespace RabbitPool.Tests
{
    public class PoolTests
    {
        [Fact]
        public void ShouldEnforceConnectionLimitByReplacingOldConnections()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 10, 1);
            for (var i = 0; i < 12; i++)
            {
                var model = pool.GetChannel();
              
            }
            Assert.Equal(10, pool.Connections.Count());
        }

        [Fact]
        public void ShouldCreateNewConnectionWhenChannelsExceeded()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 5, 10);
            for (var i = 0; i < 11; i++)
            {
                var model = pool.GetChannel();
 
            }
            Assert.Equal(2, pool.ConnectionCount);
        }


        [Fact]
        public void ShouldEliminateClosedConnectionsOnGetChannel()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 5, 2);
            for (var i = 0; i < 6; i++)
                pool.GetChannel();
            pool.Connections.First().Close();
            pool.GetChannel();
            Assert.Equal(2, pool.ConnectionCount);
        }

        [Fact]
        public void ShouldFreeUpChannelsForShutdownChannels()
        {
            var pool = new PoolManager(new RabbitConnectionOptions(), 5, 2);
            var model = pool.GetChannel();
            var model2 = pool.GetChannel();
            model.Dispose();
            Assert.Equal(1, pool.ChannelCount);
        }
    }
}
