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
    public class TestSyncQueue
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestSyncQueue));
        //
        const string QUEUE_NAME = "TestQueue";
        const string BROKER = "tcp://localhost:61616";
        const string CONSUMER_ID = "test.queue.receiver";
        private SynQueueReceiver receiver;
        private QueueSender sender;

        [SetUp]
        public void SetUp()
        {            
            
                log.Debug("Start queue receiver with Consumer ID: " + CONSUMER_ID + "#1"  );
                this.receiver = new SynQueueReceiver( BROKER, QUEUE_NAME );
                this.receiver.OnMessageReceived += 
                ( 
                    message =>
                    {
                        log.Info( this.receiver.ConsumerId + ": " + message.Text);
                        log.Debug( message.NMSTimestamp.ToLocalTime() );
                    }
                );
                
                this.receiver.ConsumerId = CONSUMER_ID + "#1";
                this.receiver.Start();
            

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
            //log.Debug("receive message...");
            //while (this.receiver.Process())
            //{ }
            //log.Debug("exit from receive....");
        }

        [Test]
        public void TestReceive()
        {            
            log.Debug("receive message...");
            while (this.receiver.Process())
            { }
            log.Debug("exit from receive....");
        }

        [TearDown]
        public void TearDown()
        {
            //Thread.Sleep(1000);
            this.sender.Stop();
            this.receiver.Stop();      
        }
    }
}
