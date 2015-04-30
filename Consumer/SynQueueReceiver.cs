using System;
using Common.Logging;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;

namespace ActiveMqLab.Consumer
{
    public class SynQueueReceiver : ISynMsgReceiver
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SynQueueReceiver));
        //
        private IConnection conn;
        private ISession session;
        private IQueue queue;
        private bool disposed = false;
        private IMessageConsumer Consumer;
        //
        public string Destination { get; set; }
        public string BrokerURL { get; set; }
        public string ConsumerId { get; set; }
        //
        public event SynMessageReceivedDelegate<ITextMessage> OnMessageReceived; 
        //
        public SynQueueReceiver()
        {
            this.disposed = true;
        }

        public SynQueueReceiver( string brokerURL, string destination )
        {
            this.BrokerURL = brokerURL;
            this.Destination = destination;
            this.disposed = true;
        }      

        public void Start()
        {
            if (null == this.BrokerURL || null == this.Destination)
            {
                throw new Exception("Parameter Error...");
            }
            if (this.disposed)
            {
                IConnectionFactory connectionFactory = new ConnectionFactory(this.BrokerURL);
                this.conn = connectionFactory.CreateConnection();
                this.conn.Start();
                this.session = conn.CreateSession();
                //
                this.queue = new ActiveMQQueue(this.Destination);
                //
                this.Consumer = this.session.CreateConsumer(this.queue);
                //this.Consumer.Listener +=
                //(
                //    message =>
                //    {
                //        ITextMessage textMessage = message as ITextMessage;
                //        if (textMessage == null)
                //        {
                //            throw new InvalidCastException();
                //        }
                //        if (OnMessageReceived != null)
                //        {
                //            OnMessageReceived(textMessage);
                //        }
                //    }
                //);
                this.disposed = false;
            }
        }

        public bool Process()
        {           
            IMessage message = this.Consumer.Receive( TimeSpan.FromSeconds(1));
            log.Debug(message);
            if (null != message)
            {
                this.OnMessageReceived((ITextMessage)message);
                return true;
            }
            return false;
        }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Consumer.Close();
                this.Consumer.Dispose();
                this.session.Close();
                this.session.Dispose();
                this.conn.Close();
                this.conn.Dispose();
                this.disposed = true;
            }
        } 
    }
}
