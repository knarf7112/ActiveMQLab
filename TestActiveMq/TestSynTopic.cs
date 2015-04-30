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
    public class TestSynTopic
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestSynTopic));      
        //
        const string TOPIC_NAME = "TestTopic";
        const string BROKER = "tcp://localhost:61616";
        const string CONSUMER_ID = "test.topic.subscriber";
        SynTopicSubscriber subscriber;

        [SetUp]
        public void SetUp()
        {            
                    
                this.subscriber = new SynTopicSubscriber( BROKER, TOPIC_NAME );
                
                // Add event
                this.subscriber.OnMessageReceived += 
                (
                    message =>
                    {
                        log.Info(this.subscriber.ConsumerId + ": " + message.Text);
                        log.Debug(message.NMSTimestamp.ToLocalTime());
                    }
                );
                this.subscriber.ConsumerId = CONSUMER_ID + "#1";
                //subscriber.Start();
        }

        [Test]
        public void TestPublish()
        {           
            using ( IMessageSender publisher = new TopicPublisher( BROKER, TOPIC_NAME))
            {
                publisher.Start();
                for (int i = 0; i < 5; i++)
                {
                    publisher.SendMessage( "This is an unit test #" + (i+1) );
                    //Thread.Sleep(1000);
                }
            }            
        }

        [Test]
        public void TestConsum()
        {
            this.subscriber.Start();
            log.Debug("receive message...");
            while (this.subscriber.Process()) { }
            log.Debug("exit from receive....");
            this.subscriber.Stop();
        }

        [TearDown]
        public void TearDown()
        {
            //Thread.Sleep(1000);
            this.subscriber.Dispose();
        }
    }    
}
