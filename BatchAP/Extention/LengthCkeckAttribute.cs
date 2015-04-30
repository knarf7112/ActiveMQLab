using System;

namespace AsyncMultiSocket.Handlers.State.Entities
{
    /// <summary>
    /// 用來設定POCO的字串長度
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class LengthCkeckAttribute : Attribute
    {
        /// <summary>
        /// 自訂的固定長度
        /// </summary>
        public int FixLength { get; set; }
    }
}
