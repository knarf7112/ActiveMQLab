using ActiveMqLab.Consumer;
using ActiveMqLab.Producer;
using BatchAP.POCO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BatchAP
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Batch batchAP = new Batch();
            batchAP.InitRegisterSignON_OFF();
            batchAP.RunBatch();
            //string brokerURL = System.Configuration.ConfigurationManager.AppSettings["BrokerURL"];
            
            //----------------------------------------------------------------
            //TopicPublisher tp = new TopicPublisher(brokerURL, "t1");
            //tp.Start();

            //tp.SendMessage<string>("Test1");
            

            //----------------------------------------------------------------

            //TopicSubscriber ts = new TopicSubscriber(brokerURL, "TestPOCO")
            //{
            //    ConsumerId = "TopicSubscriber1"
            //};
            //ts.OnMessageReceived += ts_OnMessageReceived;
            //ts.Start();

            
            Console.ReadKey();
            batchAP.Dispose();
            //ts.Stop();
        }
    }
}
