using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Models;
using System.Reflection;
using P2PSocket.Core.Utils;

namespace P2PSocket.Client.Utils
{
    public class ConfigManager: IConfig
    {
        AppCenter appCenter { set; get; }
        public ConfigManager()
        {
            appCenter = EasyInject.Get<AppCenter>();
        }
        public bool IsExistConfig()
        {
            return File.Exists(appCenter.ConfigFile);
        }
        public BaseConfig LoadFromFile()
        {
            AppConfig config = new AppConfig();
            IFileManager fileManager = EasyInject.Get<IFileManager>();
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(config);
            IConfigIO instance = null;
            fileManager.ReadLine(IFileManager.Config, lineData => {
                string lineStr = lineData.Trim();
                if (lineStr.Length > 0 && !lineStr.StartsWith("#"))
                {
                    if (handleDictionary.ContainsKey(lineStr))
                        instance = handleDictionary[lineStr];
                    else
                        instance?.ReadConfig(lineStr);
                }
            });
            foreach (string key in handleDictionary.Keys)
            {
                handleDictionary[key].WriteLog();
            }
            return config;
        }


        public BaseConfig LoadFromString(string data)
        {
            AppConfig config = new AppConfig();
            if (string.IsNullOrEmpty(data)) throw new Exception("LoadFromString参数为为空");
            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter sw = new StreamWriter(ms);
                sw.Write(data);
                sw.Flush();
                StreamReader sr = new StreamReader(ms);
                sr.BaseStream.Position = 0;
                config = DoLoadFromStream(sr);
                sw.Close();
                ms.Close();
            }
            return config;
        }
        private AppConfig DoLoadFromStream(StreamReader fs)
        {
            AppConfig config = new AppConfig();
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

        public void SaveToFile()
        {

        }
        private Dictionary<string, IConfigIO> GetConfigIOInstanceList(AppConfig config)
        {
            Dictionary<string, IConfigIO> retDic = new Dictionary<string, IConfigIO>();
            Type[] configIOList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ConfigIOAttr>() != null).ToArray();
            foreach (Type type in configIOList)
            {
                retDic.Add(type.GetCustomAttribute<ConfigIOAttr>().Name, Activator.CreateInstance(type,new object[] { config }) as IConfigIO);
            }
            return retDic;
        }
    }
}
