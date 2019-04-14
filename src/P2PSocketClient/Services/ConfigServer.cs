using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Wireboy.Socket.P2PClient.Models;
using System.IO;

namespace Wireboy.Socket.P2PClient
{
    public static class ConfigServer
    {
        /// <summary>
        /// 配置文件名
        /// </summary>
        public const string ConfigFile = "Settings.ini";
        /// <summary>
        /// 日志文件名
        /// </summary>
        public const string LogFile = "P2PSocketClient.log";
        /// <summary>
        /// 通用配置
        /// </summary>
        public static ApplicationConfig AppSettings { set; get; } = new ApplicationConfig();
        /// <summary>
        /// http服务配置
        /// </summary>
        public static List<HttpModel> HttpSettings { set; get; } = new List<HttpModel>();
        /// <summary>
        /// http对象缓存
        /// </summary>
        private static HttpModel m_lastReadModel = null;
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="file"></param>
        public static void LoadFromFile()
        {
            if (File.Exists(ConfigFile))
            {
                HttpSettings.Clear();
                //0表示未知，1表示Common属性，2表示http服务属性
                int RecordMode = 1;
                StreamReader fileStream = new StreamReader(ConfigFile);
                List<PropertyInfo> commonPropList = GetPropertyInfos(AppSettings.GetType());
                List<PropertyInfo> httpPropList = GetPropertyInfos(typeof(HttpModel));
                while (!fileStream.EndOfStream)
                {
                    string lineStr = fileStream.ReadLine().Trim();
                    if (!(String.IsNullOrEmpty(lineStr) || lineStr.StartsWith("#")))
                    {
                        bool isSignal = false;
                        if (lineStr == "[Common]")
                        {
                            RecordMode = 1;
                            isSignal = true;
                        }
                        else if (lineStr == "[HttpServer]")
                        {
                            RecordMode = 2;
                            isSignal = true;
                        }
                        if (isSignal)
                        {
                            InsertHttmodelAndCleartemp();
                            if (RecordMode == 2)
                                m_lastReadModel = new HttpModel();
                        }
                        else
                        {
                            string[] lineSplit = lineStr.Split('=');
                            if (lineSplit.Length > 1)
                            {
                                string fieldName = lineSplit[0];
                                string value = lineSplit[1];
                                if (RecordMode == 1)
                                {
                                    ReadCommonSetting(fieldName, value, commonPropList);
                                }
                                else if (RecordMode == 2)
                                {
                                    ReadHttpSetting(fieldName, value, httpPropList);
                                }
                            }
                        }
                    }
                }
                InsertHttmodelAndCleartemp();
                fileStream.Close();
            }
            SaveToFile();
        }

        public static void ReadCommonSetting(string fieldName, string value, List<PropertyInfo> commonPropList)
        {
            PropertyInfo property = commonPropList.Where(t => t.Name == fieldName).FirstOrDefault();
            if (property != null)
            {
                try
                {
                    if (property.PropertyType.BaseType == typeof(Enum))
                    {
                        if (property.PropertyType == typeof(LogLevel))
                        {
                            LogLevel enumValue = ((LogLevel[])Enum.GetValues(property.PropertyType)).Where(t => t.ToString() == value).FirstOrDefault();
                            property.SetValue(AppSettings, enumValue);
                        }
                        else
                        {
                            throw new Exception(string.Format("未配置{0}枚举的转换", property.PropertyType));
                        }
                    }
                    else
                    {
                        property.SetValue(AppSettings, Convert.ChangeType(value, property.PropertyType));
                    }
                }
                catch { }
            }
        }
        public static void ReadHttpSetting(string fieldName, string value, List<PropertyInfo> httpPropList)
        {
            PropertyInfo property = httpPropList.Where(t => t.Name == fieldName).FirstOrDefault();
            if (property != null)
            {
                try
                {
                    property.SetValue(m_lastReadModel, Convert.ChangeType(value, property.PropertyType));
                }
                catch { }
            }
        }
        /// <summary>
        /// 将记录插入配置并清除缓存
        /// </summary>
        public static void InsertHttmodelAndCleartemp()
        {

            if (m_lastReadModel != null)
            {
                HttpSettings.Add(m_lastReadModel);
            }
            m_lastReadModel = null;
        }
        /// <summary>
        /// 保存配置文件
        /// </summary>
        public static void SaveToFile()
        {
            StreamWriter fileStream = new StreamWriter(ConfigFile, false);
            WriteCommonSetting(fileStream);
            fileStream.WriteLine();
            fileStream.WriteLine();
            WriteHttpSetting(fileStream);
            fileStream.Close();
        }
        /// <summary>
        /// 保存通用设置
        /// </summary>
        /// <param name="fileStream">文件流</param>
        public static void WriteCommonSetting(StreamWriter fileStream)
        {
            List<PropertyInfo> properties = GetPropertyInfos(AppSettings.GetType());
            fileStream.WriteLine("#基础设置");
            fileStream.WriteLine("[Common]");
            WriteProperties(fileStream, properties, AppSettings);
        }
        /// <summary>
        /// 保存http服务设置
        /// </summary>
        /// <param name="fileStream">文件流</param>
        public static void WriteHttpSetting(StreamWriter fileStream)
        {
            List<PropertyInfo> properties = GetPropertyInfos(typeof(HttpModel));
            bool hasRemark = true;
            List<HttpModel> itemList = HttpSettings;
            foreach (HttpModel item in itemList)
            {
                fileStream.WriteLine("#Http服务设置");
                fileStream.WriteLine("[HttpServer]");
                WriteProperties(fileStream, properties, item, hasRemark);
                fileStream.WriteLine();
                hasRemark = false;
            }
        }
        /// <summary>
        /// 获取指定类有ConfigField标记的属性
        /// </summary>
        /// <param name="type">类的类型</param>
        /// <returns></returns>
        public static List<PropertyInfo> GetPropertyInfos(Type type)
        {
            return type.GetProperties().Where(t => t.CustomAttributes.Where(p => p.AttributeType == typeof(ConfigField)).Count() > 0).ToList();
        }

        /// <summary>
        /// 将ConfigField标记的属性数据写入文件
        /// </summary>
        /// <param name="fileStream">文件流</param>
        /// <param name="properties">属性集合</param>
        public static void WriteProperties(StreamWriter fileStream, List<PropertyInfo> properties, object obj, bool hasRemark = true)
        {
            foreach (PropertyInfo property in properties)
            {
                ConfigField data = (ConfigField)property.GetCustomAttribute(typeof(ConfigField));
                if (hasRemark)
                    fileStream.WriteLine("#{0}", data.Remark);
                object value = property.GetValue(obj);
                fileStream.WriteLine("{0}={1}", property.Name, value);
            }
        }
    }
}
