using ActiveMqLab.Consumer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BatchAP.POCO
{
    /// <summary>
    /// 存放銀行設定檔的POCO
    /// </summary>
    public class BankAgent
    {
        /// <summary>
        /// 銀行代碼
        /// </summary>
        public string BankCode { get; set; }

        /// <summary>
        /// 銀行電文Message Type
        /// </summary>
        public string MessageType { get; set; }
        /// <summary>
        /// MQ的資料目的地
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// 要連向BankAgent的IP
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// 要連向BankAgent的Port
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 連線時送出逾時(ms)
        /// </summary>
        public int SendTimeout { get; set; }
        /// <summary>
        /// 連線時接收逾時(ms)
        /// </summary>
        public int ReceiveTimeout { get; set; }

    }
}
