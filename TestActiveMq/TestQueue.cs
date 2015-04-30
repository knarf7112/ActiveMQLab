using System;
using System.Threading;
using NUnit.Framework;
using Common.Logging;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using ActiveMqLab.Consumer;
using ActiveMqLab.Producer;

using System.Collections.Generic;

namespace TestActiveMq
{    
    [TestFixture]
    public class TestQueue
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestQueue));
        //
        const string QUEUE_NAME = "TestQueue";
        const string BROKER = "tcp://localhost:61616";
        const string CONSUMER_ID = "test.queue.receiver";
        private List<IMessageReceiver> receivers;
        private QueueSender sender;

        [SetUp]
        public void SetUp()
        {            
            receivers = new List<IMessageReceiver>();
            //
            for (int i = 0; i < 1; i++)
            {               
                log.Debug("Start queue receiver with Consumer ID: " + CONSUMER_ID + "#" + (i+1) );
                var receiver = new QueueReceiver( BROKER, QUEUE_NAME );
                receiver.OnMessageReceived += 
                ( 
                    message =>
                    {
                        log.Info( receiver.ConsumerId + ": " + message.Text);
                        log.Debug( message.NMSTimestamp.ToLocalTime() );
                    }
                );
                /*
                if (i == 1)
                {
                    receiver.OnMessageReceived += (
                        message =>
                        {
                            log.Debug(receiver.ConsumerId + "[2nd]: " + message);
                        }
                    );
                }
                */
                receiver.ConsumerId = CONSUMER_ID + "#" + (i + 1);
                receiver.Start();
                
                receivers.Add(receiver);
            }

        }

        [Test]
        public void TestSender()
        {
            log.Debug("New QueueSender...");          
            //
            this.sender = new QueueSender();
            this.sender.BrokerURL = BROKER;
            this.sender.Destination = QUEUE_NAME;
            this.sender.Start();
            log.Debug("Send Messages...");
            for (int i = 0; i < 20; i++)
            {
                this.sender.SendMessage( string.Format("Message #{0}", i+1) );
            }
        }


        [TearDown]
        public void TearDown()
        {
            Thread.Sleep(1000);
            this.sender.Stop();
            this.receivers.ForEach(r => r.Stop());      
        }
    }
}
