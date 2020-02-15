using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Utils;

namespace P2PSocket.Client.Models.ConfigIO
{
    [ConfigIOAttr("[Common]")]
    public class Common : IConfigIO
    {
        public List<LogInfo> MessageList = new List<LogInfo>();
        private Dictionary<string, MethodInfo> MethodDic = new Dictionary<string, MethodInfo>();
        public Common()
        {
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
        [ConfigMethodAttr("ServerAddress")]
        public void Read01(string data)
        {
            string[] ipStr = data.Split(':');
            if (ipStr.Length == 2)
            {
                Global.ServerAddress = ipStr[0];
                Global.ServerPort = Convert.ToInt32(ipStr[1]);
                P2PTcpClient.Proxy.Address.Add(Global.ServerAddress);
            }
            else
            {
                if (string.IsNullOrEmpty(data)) throw new ArgumentException("ServerAddress格式错误，请参考https://github.com/bobowire/Wireboy.Socket.P2PSocket/wiki");
            }
        }
        [ConfigMethodAttr("ClientName")]
        public void Read02(string data)
        {
            Global.ClientName = data;
        }
        [ConfigMethodAttr("AuthCode")]
        public void Read03(string data)
        {
            Global.AuthCode = data;
        }
        [ConfigMethodAttr("AllowPort")]
        public void Read04(string data)
        {
            string[] portList = data.Split(',');
            foreach (string portStr in portList)
            {
                AllowPortItem portItem = new AllowPortItem(portStr);
                Global.AllowPortList.Add(portItem);
            }
        }
        [ConfigMethodAttr("BlackList")]
        public void Read05(string data)
        {
            string[] blackList = data.Split(',');
            foreach (string value in blackList)
            {
                Global.BlackClients.Add(value);
            }
        }
        [ConfigMethodAttr("LogLevel")]
        public void Read06(string data)
        {
            string levelName = data.ToLower();
            switch (levelName)
            {
                case "debug": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Debug; break;
                case "error": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Error; break;
                case "info": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Info; break;
                case "none": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.None; break;
                case "warning": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Warning; break;
                case "fatal": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Fatal; break;
                case "trace": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Trace; break;
                default: throw new ArgumentException("LogLevel格式错误，请参考https://github.com/bobowire/Wireboy.Socket.P2PSocket/wiki"); 
            }
        }
        [ConfigMethodAttr("Proxy_Ip")]
        public void Read07(string data)
        {
            string[] portList = data.Split(':');
            if (portList.Length == 3)
            {
                P2PTcpClient.Proxy.ProxyType = portList[0];
                P2PTcpClient.Proxy.IP = portList[1];
                P2PTcpClient.Proxy.Port = Convert.ToInt32(portList[2]);
            }
            else
            {
                throw new ArgumentException("Proxy_Ip格式错误，请参考https://github.com/bobowire/Wireboy.Socket.P2PSocket/wiki");
            }
        }
        [ConfigMethodAttr("Proxy_UserName")]
        public void Read08(string data)
        {
            P2PTcpClient.Proxy.UserName = data;
        }
        [ConfigMethodAttr("Proxy_Password")]
        public void Read09(string data)
        {
            P2PTcpClient.Proxy.Password = data;
        }
    }
}
