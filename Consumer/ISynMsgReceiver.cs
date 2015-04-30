using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ActiveMqLab.Consumer
{
    public delegate void SynMessageReceivedDelegate<T>(T message);

    public interface ISynMsgReceiver : IDisposable
    {
        void Start();
        bool Process();
        void Stop();
    }
}
