using System;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.ActiveMQ;
using ActiveMqLab.Consumer;
using ActiveMqLab.Producer;

using Common.Logging;

namespace ActiveMqLab.Producer
{
    public class QueueSender : IMessageSender
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(QueueSender));
        //
        private bool disposed = true;
        private IQueue queue;
        private ISession session;
        private IConnection conn;
        //
        public string BrokerURL { get; set; }
        public string Destination { get; set; }
        public IMessageProducer Producer { get; private set; }

        public QueueSender()
        {
            this.disposed = true;
        }

        public QueueSender( string brokerURL, string destination )
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
                this.queue = new ActiveMQQueue(this.Destination);
                this.Producer = this.session.CreateProducer(this.queue);
                this.disposed = false;
            }
        }       
        
        public void SendMessage<T>(T message)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
            ITextMessage textMessage = this.Producer.CreateTextMessage(); //new ActiveMQTextMessage();
            textMessage.Text = message as string;
            this.Producer.Send( textMessage );            
        }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            log.Debug("Run Dispose...");
            if (!this.disposed)
            {
                this.Producer.Close();
                this.Producer.Dispose();
                this.queue.Dispose();
                this.session.Close();
                this.session.Dispose();
                this.conn.Close();
                this.conn.Dispose();
                this.disposed = true;
            }
        }
    }
}
