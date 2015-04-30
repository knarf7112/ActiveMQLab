using System;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Common.Logging;

namespace ActiveMqLab.Consumer
{
    public class TopicSubscriber : IMessageReceiver
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TopicSubscriber));
        //
        private IConnection conn;
        private ISession session;
        private ITopic topic;        
        private bool disposed = false;
        private IMessageConsumer Consumer;
        //
        public string BrokerURL { get; set; }
        public string Destination { get; set; }        
        public string ConsumerId { get; set; }

        public event MessageReceivedDelegate<string> OnMessageReceived;

        //public event MessageReceivedDelegate<T> OnMessageReceived2;
        //
        public TopicSubscriber()
        {
            this.disposed = true;
        }

        public TopicSubscriber( string brokerURL, string destination )
        {
            this.BrokerURL = brokerURL;
            this.Destination = destination;
            this.disposed = true;
        }
        //
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
                if (!string.IsNullOrEmpty(this.ConsumerId))
                {
                    this.conn.ClientId = this.ConsumerId;
                }
                this.conn.Start();
                this.session = conn.CreateSession();
                this.topic = new ActiveMQTopic(this.Destination);
                if (!string.IsNullOrEmpty(this.ConsumerId))
                {
                    this.Consumer = this.session.CreateDurableConsumer(this.topic, this.ConsumerId, null, false);
                }
                else
                {
                    this.Consumer = this.session.CreateConsumer(this.topic);
                }
                this.Consumer.Listener +=
                (
                    message =>
                    {
                        ITextMessage textMessage = message as ITextMessage;
                        //IObjectMessage textMessage = message as IObjectMessage;//接收傳送Object
                        if (textMessage == null)
                        {
                            throw new InvalidCastException("接收端轉型失敗");
                        }
                        if (OnMessageReceived != null)
                        {
                            log.Debug(this.ConsumerId + " Run OnMessageReceived: " + textMessage);
                            OnMessageReceived(textMessage.Text);
                        }
                        //if (OnMessageReceived2 != null)
                        //{
                        //    object receiveObj = textMessage.Body as object;
                        //    if (receiveObj == null)
                        //    {
                        //        receiveObj = default(T);
                        //    }
                        //    OnMessageReceived2.Invoke((T)receiveObj);
                        //}
                    }
                );
                this.disposed = false;
            }
        }

        public void Dispose()
        {
            if ( !this.disposed )
            {
                this.Consumer.Close();
                this.Consumer.Dispose();
                this.topic.Dispose();
                this.session.Close();
                this.session.Dispose();
                this.conn.Close();
                this.conn.Dispose();
                this.disposed = true;
            }
        }
        public void Stop() 
        {
            if (!this.disposed)
            {
                this.Dispose();
            }
        }
        
    }
}
