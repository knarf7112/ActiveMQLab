using System;

namespace ActiveMqLab.Producer
{
    public interface IMessageSender : IDisposable
    {
        void SendMessage<T>(T message);
        void Start();
        void Stop();
    }
}
