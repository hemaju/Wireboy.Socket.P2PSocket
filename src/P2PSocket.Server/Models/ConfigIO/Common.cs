using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using P2PSocket.Core.Utils;

namespace P2PSocket.Server.Models.ConfigIO
{
    [ConfigIOAttr("[Common]")]
    public class Common : IConfigIO
    {
        public List<LogInfo> MessageList = new List<LogInfo>();
        private Dictionary<string, MethodInfo> MethodDic = new Dictionary<string, MethodInfo>();
        ConfigCenter config = null;
        public Common(ConfigCenter config)
        {
            this.config = config;
            var methods = GetType().GetMethods().Where(t => t.GetCustomAttribute<ConfigMethodAttr>() != null);
            foreach (MethodInfo method in methods)
            {
                string configName = method.GetCustomAttribute<ConfigMethodAttr>().Name.ToUpper();
                if (MethodDic.ContainsKey(configName))
                {
                    MethodDic[configName] = method;
                }
                else
                {
                    MethodDic.Add(configName, method);
                }
            }
        }

        public void ReadConfig(string text)
        {
            int start = text.IndexOf('=');
            if (start > 0 && start < text.Length - 1)
            {
                string name = text.Substring(0, start).Trim().ToUpper();
                if (MethodDic.ContainsKey(name))
                {
                    try
                    {
                        MethodDic[name.ToUpper()].Invoke(this, new object[] { text.Substring(start + 1).Trim() });
                        LogDebug($"【Common配置项】读取成功：{name}");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"【Common配置项】读取失败：{ex.Message}");
                    }
                    return;
                }
            }
            LogWarning($"【Common配置项】未识别的配置项:\"{text}\"{Environment.NewLine}请参考https://github.com/bobowire/Wireboy.Socket.P2PSocket/wiki");
        }

        protected void LogDebug(string msg)
        {
            MessageList.Add(new LogInfo() { LogLevel = LogLevel.Debug, Msg = msg, Time = DateTime.Now });
        }
        protected void LogWarning(string msg)
        {
            MessageList.Add(new LogInfo() { LogLevel = LogLevel.Warning, Msg = msg, Time = DateTime.Now });
        }
        public void WriteLog()
        {
            foreach (LogInfo logInfo in MessageList)
            {
                LogUtils.WriteLine(logInfo);
            }
        }
        [ConfigMethodAttr("Port")]
        public void Read01(string data)
        {
            if (int.TryParse(data, out int port))
            {
                config.LocalPort = port;
            }
            else
            {
                throw new ArgumentException("Port格式错误，请参考https://github.com/bobowire/Wireboy.Socket.P2PSocket/wiki");
            }
        }
        [ConfigMethodAttr("LogLevel")]
        public void Read02(string data)
        {
            string levelName = data.ToLower();
            switch (levelName)
            {
                case "debug": LogUtils.Instance.LogLevel = LogLevel.Debug; break;
                case "error": LogUtils.Instance.LogLevel = LogLevel.Error; break;
                case "info": LogUtils.Instance.LogLevel = LogLevel.Info; break;
                case "none": LogUtils.Instance.LogLevel = LogLevel.None; break;
                case "warning": LogUtils.Instance.LogLevel = LogLevel.Warning; break;
                default: throw new ArgumentException("LogLevel格式错误，请参考https://github.com/bobowire/Wireboy.Socket.P2PSocket/wiki");
            }
        }
        [ConfigMethodAttr("AllowClient")]
        public void Read03(string data)
        {
            string[] clientItems = data.Split(',');
            foreach (string clientItem in clientItems)
            {
                ClientItem item = new ClientItem();
                string[] authItem = clientItem.Split(':');
                if (authItem.Length == 1)
                {
                    item.ClientName = authItem[0];
                }
                else if (authItem.Length == 2)
                {
                    item.ClientName = authItem[0];
                    item.AuthCode = authItem[1];
                }
                else
                {
                    throw new ArgumentException($"AllowClient格式错误，错误内容：\"{clientItem}\"请参考https://github.com/bobowire/Wireboy.Socket.P2PSocket/wiki");
                }
                config.ClientAuthList.Add(item);
            }
        }
    }
}
