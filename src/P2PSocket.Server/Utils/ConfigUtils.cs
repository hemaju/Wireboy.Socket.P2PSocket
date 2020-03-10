using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Models;
using P2PSocket.Server.Models;
using ReadConfig = P2PSocket.Server.Models.ConfigIO;
using System.Reflection;

namespace P2PSocket.Server.Utils
{
    public static class ConfigUtils
    {
        public static bool IsExistConfig()
        {
            return File.Exists(AppCenter.Instance.ConfigFile);
        }
        public static ConfigCenter LoadFromFile()
        {
            ConfigCenter config = new ConfigCenter();
            using (StreamReader fs = new StreamReader(AppCenter.Instance.ConfigFile))
            {
                config = DoLoadConfig(fs);
            }
            return config;
        }

        internal static ConfigCenter DoLoadConfig(StreamReader fs)
        {
            ConfigCenter config = new ConfigCenter();
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(config);
            IConfigIO instance = null;
            while (!fs.EndOfStream)
            {
                string lineStr = fs.ReadLine().Trim();
                if (lineStr.Length > 0 && !lineStr.StartsWith("#"))
                {
                    if (handleDictionary.ContainsKey(lineStr))
                        instance = handleDictionary[lineStr];
                    else
                        instance?.ReadConfig(lineStr);
                }
            }
            foreach (string key in handleDictionary.Keys)
            {
                handleDictionary[key].WriteLog();
            }
            return config;
        }

        public static ConfigCenter LoadFromString(string data)
        {
            ConfigCenter config = new ConfigCenter();
            if (string.IsNullOrEmpty(data)) throw new Exception("LoadFromString参数为为空");
            using (MemoryStream ms = new MemoryStream())
            {
                IConfigIO instance = null;
                StreamWriter sw = new StreamWriter(ms);
                sw.Write(data);
                sw.Flush();
                StreamReader sr = new StreamReader(ms);
                sr.BaseStream.Position = 0;
                config = DoLoadConfig(sr);
                ms.Close();
            }
            return config;
        }

        public static void SaveToFile()
        {

        }
        public static Dictionary<string, IConfigIO> GetConfigIOInstanceList(ConfigCenter config)
        {
            Dictionary<string, IConfigIO> retDic = new Dictionary<string, IConfigIO>();
            Type[] configIOList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ConfigIOAttr>() != null).ToArray();
            foreach (Type type in configIOList)
            {
                retDic.Add(type.GetCustomAttribute<ConfigIOAttr>().Name, Activator.CreateInstance(type, new object[] { config }) as IConfigIO);
            }
            return retDic;
        }
    }
}
