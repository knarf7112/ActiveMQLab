using System;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Common.Logging;

namespace ActiveMqLab.Consumer
{
    public class SynTopicSubscriber : ISynMsgReceiver
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SynTopicSubscriber));
        //連線元件
        private IConnection conn;
        //每個連線元件內的Session設定
        private ISession session;
        //
        private ITopic topic;        
        private bool disposed = false;
        private IMessageConsumer Consumer;
        //
        public string BrokerURL { get; set; }
        public string Destination { get; set; }        
        public string ConsumerId { get; set; }

        public event SynMessageReceivedDelegate<ITextMessage> OnMessageReceived;
        //
        public SynTopicSubscriber()
        {
            this.disposed = true;
        }

        public SynTopicSubscriber( string brokerURL, string destination )
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
                    this.conn.ClientId = this.ConsumerId;//給予此連線一個ID
                }
                this.conn.Start();
                this.session = conn.CreateSession();
                //將此ID註冊到ActiveMQ上,以後發送訊息會確定此ID是否收到,若沒有收到,則保留資料直到此ID收到才會銷毀資料
                this.topic = new ActiveMQTopic(this.Destination);
                if (!string.IsNullOrEmpty(this.ConsumerId))
                {
                    this.Consumer = this.session.CreateDurableConsumer(this.topic, this.ConsumerId, null, false);
                }
                else
                {
                    this.Consumer = this.session.CreateConsumer(this.topic);
                }
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
                //            //log.Debug(this.ConsumerId + " Run OnMessageReceived: " + textMessage);
                //            OnMessageReceived(textMessage.Text);
                //        }
                //    }
                //);
                this.disposed = false;
            }
        }

        /// <summary>
        /// 收取ActiveMQ上的資料
        /// </summary>
        /// <returns>T(有資料)/F(沒有資料)</returns>
        public bool Process()
        {
            IMessage message = this.Consumer.Receive(TimeSpan.FromMilliseconds(500));
            log.Debug(message);
            //message has data
            if (null != message)
            {
                //Invoke Event
                this.OnMessageReceived((ITextMessage)message);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Destory MQ Component
        /// </summary>
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
