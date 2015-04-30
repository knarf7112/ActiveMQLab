
namespace AsyncMultiSocket.Handlers.State.Entities
{
    
    /// <summary>
    /// 用來Mapping DataRow的銀行資料物件(poco)
    /// </summary>
    public class NETWORK_MSG
    {
        public delegate void ChangeInfoCode(string infoCode);
        public event ChangeInfoCode OnChangeInfoCode;
        private string _info_code;
        /// <summary>
        /// 銀行代碼(長度:3)
        /// </summary>
        public string BANK_CODE { get; set; }
        /// <summary>
        /// 連線狀態(長度:1) :(連線)0/1(斷線)
        /// </summary>
        public string STATUS { get; set; }
        /// <summary>
        /// (長度:3)071(Sign_On)/072(Sign_Off)/301(Echo Test)
        /// </summary>
        public virtual string INFO_CODE 
        { 
            get 
            { 
                return this._info_code; 
            } 
            set 
            {
                if (OnChangeInfoCode != null)
                {
                    OnChangeInfoCode.Invoke(value);
                }
                this._info_code = value;
            } 
        }
        /// <summary>
        /// 備註(長度:50):目前是寫銀行名稱
        /// </summary>
        public string REMARK { get; set; }
        /// <summary>
        /// 最後更新時間(長度:14)yyyyMMddhhmmss
        /// </summary>
        public string MOD_TIME { get; set; }

        public override string ToString()
        {
            return string.Format("BankCode:{0}, Status:{1}, Info_code:{2}, Ramark:{3}, Mod_Time:{4}", BANK_CODE, STATUS, INFO_CODE, REMARK, MOD_TIME);
        }
    }

    public enum BankMessageType
    {
        AutoLoadAuth = 0100,
        AutoLoadAuthReversal = 0420, 
        SubstituteAuth = 0120,
        CardLost = 0302,
        SignOnOff = 0800
    }
}
