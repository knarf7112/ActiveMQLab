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
    public class TestQueueSpring
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TestQueueSpring));
        //        
        private List<IMessageReceiver> receivers;
        private IMessageSender sender;
        //
        private IApplicationContext ctx;

        [SetUp]
        public void SetUp()
        {
            string consumerId = "test.queue.receiver#";
            this.ctx = ContextRegistry.GetContext();
            this.sender = ctx["queueSender"] as IMessageSender;
            receivers = new List<IMessageReceiver>();
            //
            for (int i = 0; i < 4; i++)
            {               
                log.Debug("Start queue receiver with Consumer ID: [" + consumerId + (i+1) + "]" );
                var receiver = ctx["queueReceiver"] as QueueReceiver;
                receiver.ConsumerId = receiver.ConsumerId + (i + 1);
                receiver.OnMessageReceived += 
                ( 
                    message =>
                    {
                        log.Info( receiver.ConsumerId + ": " + message);
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

                receiver.Start();
                receivers.Add(receiver);
            }

        }

        [Test]
        public void TestSender()
        {            
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
