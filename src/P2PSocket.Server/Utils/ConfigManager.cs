using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace P2PSocket.Server.Utils
{
    public interface IServerConfig : IConfig
    {
        void SaveMacAddress(BaseConfig configCenter);
    }
    public class ConfigManager : IServerConfig
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
            config = DoLoadConfig(config);
            config = DoLoadMacAddress(config);
            return config;
        }

        private AppConfig DoLoadConfig(AppConfig config)
        {

            IFileManager fileManager = EasyInject.Get<IFileManager>();
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(config);
            IConfigIO instance = null;
            fileManager.ReadLine(IFileManager.Config, lineData =>
            {
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

        private AppConfig DoLoadMacAddress(AppConfig config)
        {
            IFileManager fileManager = EasyInject.Get<IFileManager>();
            if (fileManager.IsExist(FileManeger.MacAdress))
            {
                fileManager.ReadLine(FileManeger.MacAdress, lineData =>
                {
                    string lineStr = lineData.Trim();
                    string[] oneData = lineStr.Split(' ').Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
                    if (oneData.Length == 2)
                    {
                        config.MacAddressMap.Add(oneData[0], oneData[1]);
                    }
                });
            }
            return config;
        }
        object macLock = new object();
        public void SaveMacAddress(BaseConfig configIn)
        {
            AppConfig config = configIn as AppConfig;
            lock (macLock)
            {
                IFileManager fileManager = EasyInject.Get<IFileManager>();
                Dictionary<string, string> macMap = config.MacAddressMap;
                fileManager.ForeachWrite(FileManeger.MacAdress, func =>
                {
                    foreach (string mac in macMap.Keys)
                    {
                        func($"{mac} {macMap[mac]}");
                    }
                });

            }
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
                retDic.Add(type.GetCustomAttribute<ConfigIOAttr>().Name, Activator.CreateInstance(type, new object[] { config }) as IConfigIO);
            }
            return retDic;
        }
    }
}
