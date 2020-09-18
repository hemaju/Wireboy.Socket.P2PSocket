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

        /// <summary>
        /// 从配置文件中加载配置
        /// </summary>
        /// <returns></returns>
        public BaseConfig LoadFromFile()
        {

            IFileManager fileManager = EasyInject.Get<IFileManager>();
            return LoadFromString(fileManager.ReadAll(IFileManager.Config));
        }

        /// <summary>
        /// 从指定字符串中加载配置
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public BaseConfig LoadFromString(string data)
        {
            if (string.IsNullOrEmpty(data)) throw new Exception("LoadFromString参数为为空");
            AppConfig config = new AppConfig();
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(config);
            IConfigIO instance = null;
            ReadToEnd(data, reader =>
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

        /// <summary>
        /// 获取配置文件解析处理器
        /// </summary>
        /// <param name="config">配置实例</param>
        /// <returns></returns>
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

        /// <summary>
        /// 读取配置文件内容
        /// </summary>
        /// <param name="doReadFunc">实际读取内容的方法</param>
        private void ReadToEnd(Action<StreamReader> doReadFunc)
        {
            IFileManager fileManager = EasyInject.Get<IFileManager>();
            ReadToEnd(fileManager.ReadAll(IFileManager.Config), doReadFunc);
        }

        /// <summary>
        /// 从指定字符串读取配置
        /// </summary>
        /// <param name="doReadFunc">实际读取内容的方法</param>
        private void ReadToEnd(string content, Action<StreamReader> doReadFunc)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                StreamReader reader = new StreamReader(ms);
                while (!reader.EndOfStream)
                {
                    doReadFunc(reader);
                }
            }
        }

        /// <summary>
        /// 循环集合覆盖写入配置文件
        /// </summary>
        /// <param name="list">要写入的内容集合</param>
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

        /// <summary>
        /// 保存指定配置项
        /// </summary>
        /// <typeparam name="T">配置项类型</typeparam>
        /// <param name="item">要保存的配置项</param>
        /// <returns></returns>
        public bool SaveItem<T>(T item)
        {
            //处理器字典<处理器名称,处理器实例>
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(new AppConfig());
            //当前处理器实例
            IConfigIO instance = null;
            //配置文件内容缓存
            List<string> configTemp = new List<string>();
            //传入的配置是否已存在于配置文件
            bool isMatched = false;
            //分析当前配置文件内容
            ReadToEnd(reader =>
            {
                string lineStr = reader.ReadLine().Trim();
                /*
                 * 条件一：注释内容
                 * 条件二：处理器切换标识
                 * 条件三：没有处理器则不处理内容
                 */
                if (lineStr.StartsWith("#") || handleDictionary.ContainsKey(lineStr) || instance == null)
                {
                    //只有切换处理器时需要处理数据
                    if (handleDictionary.ContainsKey(lineStr))
                        instance = handleDictionary[lineStr];
                }
                else
                {
                    //匹配成功时，运行的方法
                    Action matchedFunc = () =>
                    {
                        configTemp.Add(instance.GetItemString(item));
                        isMatched = true;
                    };
                    object tItem = instance.ReadConfig(lineStr);
                    //匹配端口映射项
                    if (MatchPortMapItem(tItem as PortMapItem, item as PortMapItem, matchedFunc)) { };
                }
                if (isMatched && !reader.EndOfStream)
                    //如果已经找到了指定配置，则不再解析后面的数据
                    configTemp.Add(reader.ReadToEnd());
                else
                    //未匹配上的数据，将内容原样存入缓存
                    configTemp.Add(lineStr);
            });
            if (!isMatched)
            {
                if (item as PortMapItem != null)
                    SavePortMapItem(item as PortMapItem, configTemp, handleDictionary);
                else
                    throw new NotSupportedException($"未找到合适的处理器用于写入配置文件 type:{item.GetType()}");
            }
            else
            {
                //匹配上了，将数据保存到文件
                ForeachWrite(configTemp);
            }
            return true;
        }
        public bool RemoveItem<T>(T item)
        {//处理器字典<处理器名称,处理器实例>
            Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList(new AppConfig());
            //当前处理器实例
            IConfigIO instance = null;
            //配置文件内容缓存
            List<string> configTemp = new List<string>();
            //传入的配置是否已存在于配置文件
            bool isMatched = false;
            //分析当前配置文件内容
            ReadToEnd(reader =>
            {
                string lineStr = reader.ReadLine().Trim();
                /*
                 * 条件一：注释内容
                 * 条件二：处理器切换标识
                 * 条件三：没有处理器则不处理内容
                 */
                if (lineStr.StartsWith("#") || handleDictionary.ContainsKey(lineStr) || instance == null)
                {
                    //只有切换处理器时需要处理数据
                    if (handleDictionary.ContainsKey(lineStr))
                        instance = handleDictionary[lineStr];
                }
                else
                {
                    object tItem = instance.ReadConfig(lineStr);
                    //匹配端口映射项
                    isMatched = MatchPortMapItem(tItem as PortMapItem, item as PortMapItem);
                }
                if (isMatched && !reader.EndOfStream)
                    //如果已经找到了指定配置，则不再解析后面的数据
                    configTemp.Add(reader.ReadToEnd());
                else
                    //未匹配上的数据，将内容原样存入缓存
                    configTemp.Add(lineStr);
            });
            if (!isMatched)
            {
                if (item as PortMapItem != null)
                    SavePortMapItem(item as PortMapItem, configTemp, handleDictionary);
            }
            else
            {
                //匹配上了，将数据保存到文件
                ForeachWrite(configTemp);
            }
            return true;
        }

        /// <summary>
        /// 匹配端口映射配置（端口相同即匹配成功）
        /// </summary>
        /// <param name="tItem">配置文件项</param>
        /// <param name="item">用户提供的项</param>
        /// <param name="matchedFunc">匹配成功时执行的方法</param>
        /// <returns></returns>
        private bool MatchPortMapItem(PortMapItem tItem, PortMapItem item, Action matchedFunc = null)
        {
            if (tItem != null && tItem.LocalPort == item.LocalPort)
            {
                matchedFunc?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 保存端口映射配置到配置文件
        /// </summary>
        /// <param name="item">要保存的配置项</param>
        /// <param name="configTemp">配置文件内容缓存</param>
        /// <param name="handleDictionary">配置内容解析处理器实例</param>
        private void SavePortMapItem(PortMapItem item, List<string> configTemp, Dictionary<string, IConfigIO> handleDictionary)
        {
            //端口映射 解析处理器名称
            string handlerName = "[PortMapItem]";
            //解析处理器实例
            IConfigIO handler;
            if (handleDictionary.ContainsKey(handlerName))
                handler = handleDictionary[handlerName];
            else
                throw new NotSupportedException($"未找到对应的处理器 {handlerName}");
            int rIndex = configTemp.IndexOf(handlerName);
            if (rIndex >= 0)
            {
                //找到相应位置，并插值
                configTemp.Insert(rIndex + 1, handler.GetItemString(item));
            }
            else
            {
                //没有找到相应节点，添加区块到末尾
                configTemp.Add(handlerName);
                configTemp.Add(handler.GetItemString(item));
            }
            //覆盖配置文件
            ForeachWrite(configTemp);
        }

        /// <summary>
        /// 将指定字符串解析为指定对象
        /// </summary>
        /// <param name="handlerName">处理器名称</param>
        /// <param name="str">被解析的字符串</param>
        /// <returns></returns>
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
