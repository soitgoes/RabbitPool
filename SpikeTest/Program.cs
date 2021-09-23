using System;
using RabbitPool;

namespace SpikeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new RabbitConnectionOptions
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
            var poolManager = new PoolManager(options, 25, 10);
            for (var i =0; i < 200; i++)
            {
                var model = poolManager.GetChannel();


            }

            Console.ReadLine();
        }
    }
}
