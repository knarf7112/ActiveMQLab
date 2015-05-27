using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//
using ActiveMqLab.Consumer;
using ActiveMqLab.Producer;
using Common.Logging;
using AsyncMultiSocket.Handlers.State.Entities;
using OL_Autoload_Lib;
using System.Data;
using ALCommon;
using Newtonsoft.Json;
using System.Reflection;
using BatchAP.POCO;
using System.Xml;
using System.IO;
using OL_Autoload_Lib.Controller.Common.Record;
using System.Text.RegularExpressions;
namespace BatchAP
{
    public class Batch : IDisposable
    {
        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(Batch));
        private object cLock = new object();
        //待辦事項
        private Queue<string> waitForWork;
        private Queue<string> failed;
        #region Properties
        /// <summary>
        /// 銀行目前狀態列表
        /// </summary>
        public IDictionary<string, NETWORK_MSG> DicBanks { get; set; }

        /// <summary>
        /// 銀行狀態的訂閱者集合
        /// </summary>
        public IDictionary<string, TopicSubscriber> DicTopicSubscriber { get; set; }

        /// <summary>
        /// 載入Xml資源檔設定產生連線BankAgent的設定(Key)和訂閱者與發佈者(Value)的集合
        /// </summary>
        public IDictionary<BankAgent, MQPublisherAndSubsciber> DicBankAgents { get; set; }
        #endregion


        #region Constructor
        public Batch()
        {
            this.waitForWork = new Queue<string>();
            this.failed = new Queue<string>();
            this.DicBanks = new Dictionary<string, NETWORK_MSG>();
            this.DicTopicSubscriber = new Dictionary<string, TopicSubscriber>();
            this.DicBankAgents = new Dictionary<BankAgent, MQPublisherAndSubsciber>();
        }
        #endregion

        #region 1.初始化銀行的連線狀態並訂閱銀行狀態改變
        /// <summary>
        /// 初始化靜態集合DicBanks,從DB取得銀行最後更新狀態
        /// </summary>
        private bool InitDicBanks()
        {
            AL_DBModule obDB = null;
            try
            {
                log.Debug("開始初始化銀行狀態列表");
                #region 1.取得資料物件列表
                //建立DB模組
                obDB = new AL_DBModule();
                //建立Network_Msg模組
                Network_Msg network_Msg_Module = new Network_Msg();
                //開啟連線
                obDB.OpenConnection();
                //從DB取得銀行連線狀態的table
                DataTable dt = network_Msg_Module.Get_NetMsgAll(obDB);
                //建立對應用的字典集合key=物件屬性名稱/value=Row的Column名稱
                //Dictionary<string,string> dic = new Dictionary<string,string>();

                //dic.Add("","BANK_CODE");
                //dic.Add("","STATUS");
                //dic.Add("","INFO_CODE");
                ////dic.Add("","REMARK");
                //dic.Add("","MOD_TIME");

                //將DataTable Mapping成List<NETWORK_MSG>資料物件列表
                IList<NETWORK_MSG> bankStatus = Extensions.ToList<NETWORK_MSG>(dt);
                #endregion

                #region 2.初始化DicBanks內容
                foreach (NETWORK_MSG item in bankStatus)
                {
                    string key = item.BANK_CODE.PadLeft(4, '0');//key = 銀行代碼(3碼->4碼),Value = NETWORK_MSG Object
                    this.DicBanks.Add(key, item);
                    log.Debug(">> DicBanks[" + key + "] => \n" + item.ToString());
                }
                //表示已初始化完畢
                return true;
                #endregion
            }
            catch (Exception ex)
            {
                log.Error(">> [AbsClientRequestHandler][InitDicBanks] Error:" + ex.ToString());
                return false;
            }
            finally
            {
                if (obDB != null)
                    obDB.CloseConnection();//關閉連線
            }
        }

        /// <summary>
        /// 初始化所有銀行狀態列表並增加一個字典用來訂閱發佈到ActiveMQ的銀行狀態改變(key: 0000(銀行代碼) + 0800(格式) , value:銀行狀態字串)
        /// </summary>
        public void InitRegisterSignON_OFF()
        {
            try
            {
                #region 1.檢查銀行狀態列表
                //init Bank Dictionary
                if (!this.InitDicBanks())
                {
                    throw new Exception("初始化銀行狀態列表失敗");
                }
                #endregion

                #region 2.檢查並註冊ActiveMQ上的某個服務通道並記錄到集合內
                log.Debug("開始檢查銀行狀態訂閱者集合的註冊狀況");
                //取得MQ的URL
                string brokerURL = System.Configuration.ConfigurationManager.AppSettings["BrokerURL"];
                foreach (string item in this.DicBanks.Keys)
                {
                    string destination = item + "0800";// 0000(銀行代碼) + 0800(SignOn/Off)
                    if (!this.DicTopicSubscriber.ContainsKey(destination))
                    {
                        //subscriber設定URL,destination和委派的方法
                        TopicSubscriber subscriber = RegisterTopicSubscriber(brokerURL, destination, SignOn_Off_Event);

                        //加入訂閱者集合內
                        this.DicTopicSubscriber.Add(destination, subscriber);
                    }
                    else
                    {
                        //此destination存在於DicTopicSubscriber集合內
                        //檢查此物件的註冊數量
                        TopicSubscriber ts = this.DicTopicSubscriber[destination];//取得物件
                        int eventCount = this.CheckMessageReceivedDelegateCount(ts);//取得OnMessageReceived的註冊數量
                        //沒有註冊過事件
                        if (eventCount == 0)
                        {
                            ts.OnMessageReceived += SignOn_Off_Event;
                        }
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                log.Error(">> [AbsClientRequestHandler][InitRegisterSignON_OFF] Error:" + ex.ToString());
                throw new Exception("[AbsClientRequestHandler][InitRegisterSignON_OFF] Error:" + ex.ToString());
            }
        }

        /// <summary>
        /// 設定SignOn/Off事件要做的事
        /// </summary>
        /// <returns>invoke method</returns>
        private void SignOn_Off_Event(string receiveString)
        {
            //設定SignOn/Off事件要做的事
            try
            {
                lock (cLock)
                {
                    //接收發佈者傳來的Json物件
                    AutoloadRqt_NetMsg receiveObj = JsonConvert.DeserializeObject<AutoloadRqt_NetMsg>(receiveString);

                    string bankCo = receiveObj.BANK_CODE.PadLeft(4, '0');
                    NETWORK_MSG bankSignMsg = this.DicBanks[bankCo];
                    //表示銀行可連線
                    if (receiveObj.INFO_CODE == "071")
                    {
                        //將銀行物件內的狀態改變,key=銀行代號
                        bankSignMsg.STATUS = "0";
                    }
                    //表示銀行不可連線
                    else if (receiveObj.INFO_CODE == "072")
                    {
                        //將銀行物件內的狀態改變
                        bankSignMsg.STATUS = "1";
                    }
                    bankSignMsg.INFO_CODE = receiveObj.INFO_CODE;
                    bankSignMsg.MOD_TIME = receiveObj.TRANS_DATETIME;
                    log.Debug(">> [" + this.DicBanks[bankCo].BANK_CODE + "] Bank Status:" + this.DicBanks[bankCo].STATUS + " Changed at:" + this.DicBanks[bankCo].MOD_TIME);
                }
            }
            catch (Exception ex)
            {
                log.Error(">> [SignOn_Off_Event]Subscriber Failed:" + ex.ToString());
            }
        }

        /// <summary>
        /// 檢查註冊TopicSubscriber的OnMessageReceived事件被註冊的數量
        /// </summary>
        /// <param name="topicSubscriber">實作的TopicSubscriber物件</param>
        /// <returns>被註冊的數量</returns>
        private int CheckMessageReceivedDelegateCount(TopicSubscriber topicSubscriber)
        {
            try
            {
                MessageReceivedDelegate<string> dele = this.GetObjectDele<TopicSubscriber, MessageReceivedDelegate<string>>(topicSubscriber, "OnMessageReceived");
                return dele.GetInvocationList().Length;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Register and Start TopicSubscriber
        /// </summary>
        /// <param name="brokerURL">ActiveMQ URL</param>
        /// <param name="destination">ActiveMQ Column Name</param>
        /// <param name="dele">delegate object</param>
        /// <returns>TopicSubscriber(Registered and Started )</returns>
        private TopicSubscriber RegisterTopicSubscriber(string brokerURL, string destination, MessageReceivedDelegate<string> dele)
        {
            TopicSubscriber subscriber = new TopicSubscriber(brokerURL, destination);
            subscriber.Start();
            subscriber.OnMessageReceived += dele;
            return subscriber;
        }

        /// <summary>
        /// 用來檢查物件的註冊事件數量
        /// 利用傳回物件的委派使用GetInvocationList().Length取得註冊事件的數量
        /// </summary>
        /// <typeparam name="T">物件類別</typeparam>
        /// <typeparam name="DeleType">委派型別</typeparam>
        /// <param name="obj">instance的物件</param>
        /// <param name="eventName">事件的名稱</param>
        /// <returns>物件實作的委派</returns>
        private DeleType GetObjectDele<T, DeleType>(T obj, string eventName)
        {
            try
            {
                //取得輸入物件型態
                Type objType = obj.GetType();
                //取得此物件型別的指定field資訊(域資訊),[目前是取某event]
                FieldInfo fieldInfo = objType.GetField(eventName, System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance);
                //從field資訊內找出丟入的instance物件,[丟回委派]
                DeleType objdele = (DeleType)fieldInfo.GetValue(obj);
                return objdele;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 2.初始化DicBankAgents集合並開始交訊MQ上要給銀行的資料
        /// <summary>
        /// 從設定檔取得在MQ上要索取的資料類型(destination:0808+0120)
        /// 以及連向要代送電文的AP存放在dicBankAgents字典檔內
        /// </summary>
        private void InitBankAgentSetting()
        {
            XmlDocument xml = null;
            try
            {
                log.Debug("開始載入Xml資源檔設定與初始化DicBankAgents集合");
                if (this.DicBankAgents.Count == 0)
                {
                    xml = new XmlDocument();
                    string path = AppDomain.CurrentDomain.BaseDirectory;
                    string brokerURL = System.Configuration.ConfigurationManager.AppSettings["BrokerURL"];
                    string consumerId = System.Configuration.ConfigurationManager.AppSettings["ConsumerId"];

                    using (StreamReader sr = new StreamReader(path + "Config\\BankConnectionStatus.xml"))
                    {
                        xml.Load(sr);
                        XmlNodeList nodes = xml.DocumentElement.GetElementsByTagName("BankAgent");
                        foreach (XmlNode node in nodes)
                        {
                            string bankCode = node.Attributes.GetNamedItem("BankCode").Value;
                            string messageType = node.Attributes.GetNamedItem("MessageType").Value;
                            string[] data = node.InnerXml.Split(':');
                            string ip = data[0];
                            int port = Convert.ToInt32(data[1]);
                            int sendTimeout = Convert.ToInt32(data[2]);
                            int receiveTimeout = Convert.ToInt32(data[3]);
                            BankAgent bankAgent = new BankAgent()
                            {
                                BankCode = bankCode,
                                MessageType = messageType,
                                Destination = bankCode + messageType,
                                IP = ip,
                                Port = port,
                                SendTimeout = sendTimeout,
                                ReceiveTimeout = receiveTimeout,
                                
                            };
                            MQPublisherAndSubsciber mqManager = new MQPublisherAndSubsciber()
                            {
                                synTopicSubscriber = new SynTopicSubscriber()
                                {
                                    BrokerURL = brokerURL,
                                    Destination = (bankCode + messageType),
                                    ConsumerId = (consumerId + bankCode + messageType)
                                },
                                topicPublisher = new TopicPublisher()
                                {
                                    BrokerURL = brokerURL,
                                    Destination = (bankCode + messageType),
                                }
                            };
                            mqManager.synTopicSubscriber.OnMessageReceived += synTopicSubscriber_OnMessageReceived;
                            mqManager.synTopicSubscriber.Start();
                            this.DicBankAgents.Add(bankAgent, mqManager);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("[BatchAP][InitBankAgentSetting] Error:" + ex.ToString());
            }
        }

        public void RunBatch()
        {
            
            byte[] receiveBytes = null;
            SocketClient.Domain.SocketClient bankAgent = null;
            AutoloadRqt_2Bank requestToBank = null;
            AutoloadRqt_2Bank responseFormBank = null;
            //建立DB模組
            AL_DBModule obDB = new AL_DBModule();
            //交易log模組
            LogModual logM = new LogModual();
            try
            {
                //初始化DicBankAgents集合
                this.InitBankAgentSetting();

                //開啟連線
                obDB.OpenConnection();
                //--------------------------------------------------------------------------------------------
                //從字典取出每個要傳批次的電文類型
                foreach (BankAgent item in this.DicBankAgents.Keys)
                {
                    log.Debug("開始檢查" + item.BankCode + "銀行的狀態與MQ存放的" + item.MessageType + "資料");

                    #region 1.當銀行狀態是連線的且MQ有傳資料(Ex:0120類型)回來(放在工作Queue)
                    while (this.DicBanks[item.BankCode].STATUS == "0" && this.DicBankAgents[item].synTopicSubscriber.Process())
                    {
                        if (this.waitForWork.Count == 0)
                        {
                            log.Error("待辦工作Queue[waitForWork]內無資料");
                            break;
                        }
                        DateTime dtrqt = DateTime.Now;
                        DateTime dtrsp = DateTime.Now;
                        string requestToBankJsonStr = this.waitForWork.Dequeue();//取得要傳送的字串
                        try
                        {
                            using (bankAgent = new SocketClient.Domain.SocketClient(item.IP, item.Port, item.SendTimeout, item.ReceiveTimeout))
                            {
                                if (bankAgent.ConnectToServer())
                                {
                                    log.Debug("送到" + item.IP + ":" + item.Port + "的資料: " + requestToBankJsonStr);
                                    byte[] sendJsonBytes = Encoding.UTF8.GetBytes(requestToBankJsonStr);
                                    receiveBytes = bankAgent.SendAndReceive(sendJsonBytes);//送出資料並取得回應資料
                                    if (receiveBytes == null)
                                    {
                                        log.Debug("接收資料為null,開始將資料加入異常Queue");
                                        //要用Regex來比對嗎  Performance不知好不好//MESSAGE_TYPE:XXX0 --> XXX1
                                        if (Regex.IsMatch(requestToBankJsonStr, "MESSAGE_TYPE:[0-9]{3}0"))
                                        {
                                            string repeatRequest = Regex.Replace(requestToBankJsonStr, "MESSAGE_TYPE:[0-9]{3}0",
                                                delegate(Match match)
                                                {
                                                    string result = match.Value.Substring(0, match.Value.Length - 1) + "1";
                                                    return result;
                                                });
                                            log.Debug("送出失敗: 修改MESSAGE_TYPE後的JSON =>" + repeatRequest);
                                            //轉型回物件改MESSAGE_TYPE屬性內的值
                                            //AutoloadRqt_2Bank tmp = JsonConvert.DeserializeObject<AutoloadRqt_2Bank>(requestToBankJsonStr);
                                            //tmp.MESSAGE_TYPE = tmp.MESSAGE_TYPE.Substring(0, 3) + "1";//
                                            //string sendFailRequest = JsonConvert.SerializeObject(tmp);
                                            this.failed.Enqueue(repeatRequest);
                                        }
                                        else
                                        {
                                            //MESSAGE_TYPE:XXX1//格式已是重送的SPEC
                                            this.failed.Enqueue(requestToBankJsonStr);//加入異常列表
                                        }
                                        continue;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("連線BankAgent異常:" + ex.StackTrace);
                            log.Error("連線異常,開始將資料加入異常Queue");
                            string repeatRequest = Regex.Replace(requestToBankJsonStr, "MESSAGE_TYPE:[0-9]{3}0",
                                                delegate(Match match)
                                                {
                                                    string result = match.Value.Substring(0, match.Value.Length - 1) + "1";
                                                    return result;
                                                });
                            log.Debug("[Exception]: 修改MESSAGE_TYPE後的JSON =>" + repeatRequest);
                            this.failed.Enqueue(repeatRequest);//加入異常列表
                            continue;
                        }
                        finally
                        {
                            //寫送出的Tans Log
                            //轉型
                            requestToBank = JsonConvert.DeserializeObject<AutoloadRqt_2Bank>(requestToBankJsonStr);
                            dtrsp = DateTime.Now;
                            //給銀行需要12碼,給DB紀錄只能8碼,故金額切掉多餘的碼(剩8碼)
                            requestToBank.AMOUNT = (requestToBank.AMOUNT.Length > 8) ? requestToBank.AMOUNT.Substring(requestToBank.AMOUNT.Length - 8, 8) : requestToBank.AMOUNT;
                            //記錄此次交易
                            log.Debug("[紀錄]給銀行的JSON:" + requestToBankJsonStr);
                            log.Debug("開始寫入Log傳給銀行的Request物件(類型:" + requestToBank.MESSAGE_TYPE + ")");
                            logM.SaveTransLog2Bank(requestToBank, obDB, dtrqt, dtrsp);
                        }
                        //回傳訊息不為空
                        if (receiveBytes != null)
                        {
                            try
                            {
                                //寫接收的Tans Log
                                //轉型
                                string responseFormBankJsonStr = Encoding.UTF8.GetString(receiveBytes);
                                responseFormBank = JsonConvert.DeserializeObject<AutoloadRqt_2Bank>(responseFormBankJsonStr);
                                //記錄此次交易
                                log.Debug("[紀錄]銀行回來的的JSON:" + responseFormBankJsonStr);
                                log.Debug("開始寫入Log銀行回傳的Response物件(類型:" + responseFormBank.MESSAGE_TYPE + ")");
                                //因為銀行回傳無Field 61,所以從給銀行的POCO取
                                responseFormBank.ICC_info = requestToBank.ICC_info;
                                //截掉銀行的代碼(DB只能3碼)-DB可以容許5碼_2015-04-28
                                //rspFromBank.BANK_CODE = rspFromBank.BANK_CODE.Substring((rspFromBank.BANK_CODE.Length - 3), 3);
                                //銀行回應的有12碼,給DB紀錄只能8碼,故金額切掉多餘的碼(剩8碼)
                                responseFormBank.AMOUNT = (responseFormBank.AMOUNT.Length > 8) ? responseFormBank.AMOUNT.Substring(responseFormBank.AMOUNT.Length - 8, 8) : responseFormBank.AMOUNT;
                                dtrsp = DateTime.Now;
                                logM.SaveTransLog2Bank(responseFormBank, obDB, dtrqt, dtrsp);
                            }
                            catch (Exception ex2)
                            {
                                logM.SaveErrorLog(obDB, this.GetType().Name + ex2.Message, "BatchAP");
                                log.Error("[BatchAP][RunBatch] Save Bank Trans Log Failed: " + ex2.StackTrace);
                            }
                        }
                        bankAgent = null;
                    }
                    #endregion

                    #region 2.將異常列表的資料塞回MQ
                    log.Debug("2.開始將異常列表的資料塞回MQ=>筆數:" + this.failed.Count);
                    this.DicBankAgents[item].topicPublisher.Start();
                    int count = 1;
                    while (this.failed.Count > 0)
                    {
                        //從異常Queue取出送到銀行端失敗的Json String
                        string backToMQJsonStr = this.failed.Dequeue();
                        log.Debug("開始發佈第 " + (count++) + " 筆發送失敗的POCO到MQ: " + backToMQJsonStr);
                        //發佈回MQ
                        this.DicBankAgents[item].topicPublisher.SendMessage<string>(backToMQJsonStr);
                    }
                    #endregion

                    #region 3.清空Queue
                    log.Debug("開始檢查並清空" + item.MessageType + "格式的暫存Queue");
                    if (this.waitForWork.Count > 0 || this.failed.Count > 0)
                    {
                        log.Debug("見鬼了...(類型:" + item.MessageType + ")Queue還有東西:\n waitForWork:" + this.waitForWork.Count + "  \nfailed:" + this.failed.Count);
                    }

                    this.waitForWork.Clear();
                    this.failed.Clear();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                log.Error("[BatchAP][RunBatch] Error:" + ex.StackTrace);
            }
            finally
            {
                receiveBytes = null;
                requestToBank = null;
                responseFormBank = null; 
                obDB.CloseConnection();
            }
        }
        #endregion

        #region Event Method
        //收到資料要做的事
        private void synTopicSubscriber_OnMessageReceived(Apache.NMS.ITextMessage message)
        {
            string jsonString = message.Text;
            log.Debug("接收到MQ的訊息: " + jsonString);
            this.waitForWork.Enqueue(jsonString);//塞入Queue
        }
        #endregion

        public void Dispose()
        {
            try
            {
                if (this.DicTopicSubscriber != null && this.DicTopicSubscriber.Count != 0)
                {
                    foreach (string item in this.DicTopicSubscriber.Keys)
                    {
                        this.DicTopicSubscriber[item].OnMessageReceived -= SignOn_Off_Event;
                        this.DicTopicSubscriber[item].Stop();
                    }
                    this.DicTopicSubscriber.Clear();
                    this.DicTopicSubscriber = null;
                }
                if (this.DicBanks != null && this.DicBanks.Count != 0)
                {
                    this.DicBanks.Clear();
                }
                if (this.DicBankAgents != null && this.DicBankAgents.Count > 0)
                {
                    foreach (BankAgent item in this.DicBankAgents.Keys)
                    {
                        //關閉發佈者與訂閱者並釋放資源
                        this.DicBankAgents[item].synTopicSubscriber.OnMessageReceived -= synTopicSubscriber_OnMessageReceived;
                        this.DicBankAgents[item].synTopicSubscriber.Stop();
                        this.DicBankAgents[item].topicPublisher.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("[BatchAP][Dispose] Error:" + ex.StackTrace);
            }
        }
    }
}
