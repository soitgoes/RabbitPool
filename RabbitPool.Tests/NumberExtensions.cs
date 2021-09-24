using System;

namespace RabbitPool.Tests
{
    public static class NumberExtensions
    {
        public static void Times(this int times, Action action)
        {
            for (int i = 0; i < times; i++)
            {
                action.Invoke();
            }
        }
    }
}