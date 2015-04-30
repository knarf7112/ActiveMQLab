using System;
using System.Threading;
using System.Collections.Generic;
//
using NUnit.Framework;
//
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using ActiveMqLab.Consumer;
using ActiveMqLab.Producer;
//
using Spring.Context;
using Spring.Context.Support;
using Common.Logging;

namespace TestActiveMq
{
    [TestFixture]
    public class TestTopicSpring
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestTopicSpring));
        private List<IMessageReceiver> subscribers;
        private IMessageSender publisher;
        //        
        private IApplicationContext ctx;

        [SetUp]
        public void SetUp()
        {
            this.ctx = ContextRegistry.GetContext();
            this.publisher = ctx["topicPublisher"] as IMessageSender;
            this.subscribers = new List<IMessageReceiver>();
            //
            for (int i = 0; i < 4; i++)
            {
                TopicSubscriber subscriber = ctx["topicSubscriber"] as TopicSubscriber;
                subscriber.ConsumerId = subscriber.ConsumerId + (i + 1);
                // Add event
                subscriber.OnMessageReceived +=
                (
                    message => log.Info(subscriber.ConsumerId + ": " + message)
                );
                subscriber.Start();
                log.Debug("Start topic subscriber with Consumer ID: [" + subscriber.ConsumerId + "]");
                subscribers.Add(subscriber);
             }
        }

        [Test]
        public void TestPublish()
        {
            this.publisher.Start();
            for (int i = 0; i < 5; i++)
            {
                this.publisher.SendMessage( "This is an unit test #" + (i + 1) );
                Thread.Sleep(1000);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Thread.Sleep(1000);
            this.subscribers.ForEach(sub => { sub.Dispose(); });
            this.publisher.Stop();
        }
    }
}
