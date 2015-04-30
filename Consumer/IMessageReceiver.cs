using System;

namespace ActiveMqLab.Consumer
{
    public delegate void MessageReceivedDelegate<T>(T message);

    public interface IMessageReceiver : IDisposable
    {
        void Start();
        void Stop();
    }
}
