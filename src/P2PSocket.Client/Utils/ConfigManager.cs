using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Models;
using System.Reflection;
using P2PSocket.Core.Utils;
using P2PSocket.Core.Extends;

namespace P2PSocket.Client.Utils
{
    public class ConfigManager : IConfig
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
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(config);
            IConfigIO instance = null;

            ReadToEnd(reader =>
            {
                string lineStr = reader.ReadLine().Trim();
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

        private void ReadToEnd(Action<StreamReader> doReadFunc)
        {
            IFileManager fileManager = EasyInject.Get<IFileManager>();
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(fileManager.ReadAll(IFileManager.Config))))
            {
                StreamReader reader = new StreamReader(ms);
                while (!reader.EndOfStream)
                {
                    doReadFunc(reader);
                }
            }
        }

        private void ForeachWrite(List<string> list)
        {
            IFileManager fileManager = EasyInject.Get<IFileManager>();
            fileManager.ForeachWrite(IFileManager.Config, func =>
            {
                foreach (string lineStr in list)
                {
                    func(lineStr);
                }
            }, false);
        }

        public bool SaveItem<T>(T itemIn)
        {
            PortMapItem item = itemIn as PortMapItem;
            if (item != null)
            {
                Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(new AppConfig());
                IConfigIO instance = null;
                List<string> configTemp = new List<string>();
                bool isNew = true;
                ReadToEnd(reader =>
                {
                    string lineStr = reader.ReadLine().Trim();
                    if (lineStr.StartsWith("#"))
                    {
                        configTemp.Add(lineStr);
                    }
                    else if (handleDictionary.ContainsKey(lineStr))
                    {
                        instance = handleDictionary[lineStr];
                        configTemp.Add(lineStr);
                    }
                    else if (instance == null)
                    {
                        configTemp.Add(lineStr);
                    }
                    else
                    {
                        PortMapItem tItem = instance.ReadConfig(lineStr) as PortMapItem;
                        if (tItem != null && tItem.LocalPort == item.LocalPort)
                        {
                            isNew = false;
                            configTemp.Add(instance.GetItemString(item));
                        }
                        else
                        {
                            configTemp.Add(lineStr);
                        }
                    }
                });

                if (isNew)
                {
                    string handlerName = "[PortMapItem]";
                    IConfigIO handler = handleDictionary[handlerName];
                    if (handleDictionary.ContainsKey(handlerName))
                        handler = handleDictionary[handlerName];
                    else
                        throw new NotSupportedException($"未找到对应的处理器 {handlerName}");
                    int rIndex = configTemp.IndexOf(handlerName);
                    if (rIndex >= 0)
                    {
                        configTemp.Insert(rIndex + 1, handler.GetItemString(item));
                    }
                    else
                    {
                        configTemp.Add(handlerName);
                        configTemp.Add(handler.GetItemString(item));
                    }
                    ForeachWrite(configTemp);
                }
                return true;
            }
            else
                throw new NotSupportedException($"未找到合适的处理器用于写入配置文件 type:{item.GetType()}");
        }

        public object ParseToObject(string handlerName, string str)
        {
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(new AppConfig());
            if (handleDictionary.ContainsKey(handlerName))
            {
                IConfigIO handler = handleDictionary[handlerName];
                return handler.ReadConfig(str);
            }
            else
            {
                throw new NotSupportedException($"未找到对应的处理器 {handlerName}");
            }
        }
    }
}
