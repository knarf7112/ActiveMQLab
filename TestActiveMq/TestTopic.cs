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
    public class TestTopic
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestTopic));      
        private List<IMessageReceiver> subscribers;
        //
        const string TOPIC_NAME = "TestTopic";
        const string BROKER = "tcp://localhost:61616";
        const string CONSUMER_ID = "test.topic.subscriber";

        [SetUp]
        public void SetUp()
        {            
            this.subscribers = new List<IMessageReceiver>();
            //
            for (int i = 0; i < 4; i++)
            {              
                TopicSubscriber subscriber = new TopicSubscriber( BROKER, TOPIC_NAME );
                
                // Add event
                subscriber.OnMessageReceived += 
                (
                    message => log.Info( subscriber.ConsumerId + ": " + message)
                );
                subscriber.ConsumerId = CONSUMER_ID + "#" + (i + 1);
                subscriber.Start();
                // Add to Subscribers
                subscribers.Add(subscriber);
            }            
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
                    Thread.Sleep(1000);
                }
            }            
        }

        //[Test]
        //public void TestConsum()
        //{
            
        //    foreach ( TopicSubscriber ts in this.subscribers )
        //    {
        //        log.Debug("Start topic subscriber with Consumer ID: " + ts.ConsumerId + "....");
        //        ts.Start();
        //    }
        //    Thread.Sleep(10000);
        //}

        [TearDown]
        public void TearDown()
        {
            //Thread.Sleep(1000);
            this.subscribers.ForEach( sub => { sub.Stop(); } );
        }
    }    
}
