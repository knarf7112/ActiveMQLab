using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace AsyncMultiSocket.Handlers.State.Entities
{
    public static class Extensions
    {
        /// <summary>
        /// 將DataTable轉換成資料物件列表
        /// </summary>
        /// <typeparam name="T">要轉換的資料物件型別</typeparam>
        /// <param name="table"></param>
        /// <param name="mappings">row的column名稱對應dic[key]存放的字典檔</param>
        /// <returns>資料物件列表</returns>
        public static IList<T> ToList<T>(this DataTable table, IDictionary<string, string> mappings = null) where T : new()
        {
            //defined what attribute to read from class
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            //get all properties from Type T
            //var properties2 = typeof(T).GetProperties(flags).Select(property => new { Name = property.Name, Type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType }).ToList();
            IList<PropertyInfo> properties = typeof(T).GetProperties(flags).ToList();
            IList<T> result = new List<T>();

            foreach (DataRow row in table.Rows)
            {
                var item = CreateItemFromRow<T>(row, properties, mappings);
                result.Add(item);
            }
            return result;
        }
        /// <summary>
        /// 把DataTable的Row列資料Mapping成物件,如果物件名稱和Row的Column名稱不一樣,就使用Dictionary來Mapping
        /// </summary>
        /// <typeparam name="T">要轉換的型別</typeparam>
        /// <param name="row">table的資料列</param>
        /// <param name="properties">物件的屬性List</param>
        /// <param name="mappings">(option)要Mapping的字典</param>
        /// <returns>資料物件</returns>
        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties, IDictionary<string, string> mappings = null) where T : new()
        {
            T item = new T();
            foreach (PropertyInfo property in properties)
            {
                //如果有dictionary就給dic[key]取得value來比對物件的屬性名稱是否相同
                if (mappings != null && mappings.ContainsKey(property.Name))
                {
                    //把row[column]的值設定到物件的屬性裡
                    property.SetValue(item, row[mappings[property.Name]], null);
                }
                else
                {
                    //把row[column]的值設定到物件的屬性裡
                    property.SetValue(item, row[property.Name], null);
                }
            }
            return item;
        }

        /// <summary>
        /// lock用的物件
        /// </summary>
        private static object lockObj = new object();

        /// <summary>
        /// (擴充方法)用來檢查POCO物件內字串屬性的資料長度是否符合Attribute設定的長度
        /// ex: obj.CHeckLength();
        /// 長度不符會拋出Exception
        /// </summary>
        /// <typeparam name="T">要檢查的物件型別</typeparam>
        /// <param name="obj">要檢查的物件</param>
        public static void CHeckLength<T>(this T obj)
        {
            lock (lockObj)
            {
                PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (PropertyInfo property in properties.AsEnumerable())
                {
                    LengthCkeckAttribute attr = Attribute.GetCustomAttribute(property, typeof(LengthCkeckAttribute)) as LengthCkeckAttribute;
                    if (attr != null)
                    {
                        object propertyValue = property.GetValue(obj, null);
                        if (propertyValue is string && !string.IsNullOrEmpty((string)propertyValue))
                        {
                            int valueLength = ((string)propertyValue).Length;
                            if (attr.FixLength != valueLength)
                            {
                                throw new Exception(property.Name + "屬性與設定資料長度不符 => 輸入的資料長度:" + valueLength + " \n不同於屬性設定的長度:" + attr.FixLength);
                            }

                        }
                        else
                        {
                            throw new Exception(property.Name + "屬性:資料型態不為字串或資料為空!!!");
                        }
                    }
                }
            }
        }
    }
}
