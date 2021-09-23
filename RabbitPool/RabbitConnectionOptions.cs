using System;
using RabbitMQ.Client;

namespace RabbitPool
{
    public class RabbitConnectionOptions
    {
        public string HostName = "localhost";
        public string UserName = "guest";
        public string Password = "guest";
        public int Port = 5672;
        public TimeSpan ContinuationTimeout = TimeSpan.FromSeconds(10);
        public SslOption SslOption;
      
    }
}
