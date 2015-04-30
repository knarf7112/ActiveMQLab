using System;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.ActiveMQ;
using Common.Logging;

namespace ActiveMqLab.Producer
{
    public class TopicPublisher : IMessageSender
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TopicPublisher));  
        private bool disposed = false;
        private ITopic topic;
        private ISession session;
        private IConnection conn;
        //
        public string BrokerURL { get; set; }
        public string Destination { get;  set; }
        public IMessageProducer Producer { get; set; }
        //
        public TopicPublisher()
        {
            this.disposed = true;
        }

        public TopicPublisher( string brokerURL, string destination )
        {
            this.BrokerURL = brokerURL;
            this.Destination = destination;
            this.disposed = true;
        } 

        public void SendMessage<T>( T message ) 
        {
            if( this.disposed ) 
            {
                throw new ObjectDisposedException( this.GetType().Name );
            }
            //發佈物件
            //IObjectMessage objectMessage = this.Producer.CreateObjectMessage(message as Object);
            //this.Producer.Send(objectMessage);

            //發佈字串
            ITextMessage textMessage = this.Producer.CreateTextMessage(message as string);
            this.Producer.Send(textMessage);
        }

        public void Dispose()
        {
            log.Debug("Run Dispose...");
            if (!this.disposed)
            {
                this.Producer.Close();
                this.Producer.Dispose();
                this.topic.Dispose();
                this.session.Close();
                this.session.Dispose();
                this.conn.Close();
                this.conn.Dispose();
                this.disposed = true;
            }
        }
        public void Start() 
        {
            if (null == this.BrokerURL || null == this.Destination)
            {
                throw new Exception("Parameter Error....");
            }
            if (this.disposed)
            {
                IConnectionFactory connectionFactory = new ConnectionFactory(this.BrokerURL);
                this.conn = connectionFactory.CreateConnection();
                this.conn.Start();
                this.session = conn.CreateSession();
                this.topic = new ActiveMQTopic( this.Destination );
                this.Producer = this.session.CreateProducer(this.topic);
                this.Producer.DeliveryMode = MsgDeliveryMode.Persistent;//設定模式使其可存放到內建的Storange(為了使有註冊ID的Subscriber[還未取過之前發佈的資料]後來連上也可取得之前有發佈的資料),也可設定轉存到其他地方的DB
                this.disposed = false;
            }
        }

        public void Stop() 
        {
            this.Dispose();
        }
    }
}
