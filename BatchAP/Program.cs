using System;
using System.Threading;

namespace BatchAP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("開始執行Batch...");
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

            Thread.Sleep(10);
            //Console.ReadKey();
            batchAP.Dispose();
            //ts.Stop();
        }
    }
}
