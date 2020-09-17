using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using P2PSocket.Core.Utils;
using P2PSocket.Core.Enums;
using CModel = P2PSocket.Core.Models;

namespace P2PSocket.Server.Models.ConfigIO
{
    [ConfigIOAttr("[PortMapItem]")]
    public class PortMapItem : IConfigIO
    {
        public List<LogInfo> MessageList = new List<LogInfo>();
        AppConfig config = null;
        public PortMapItem(AppConfig config)
        {
            this.config = config;
        }

        public object ReadConfig(string text)
        {
            CModel.PortMapItem item = new CModel.PortMapItem();
            EasyOp.Do(() =>
            {
                string curText = text;
                if (ReadLocalIp(ref curText, ref item)
                    && ReadLocalPort(ref curText, ref item)
                    && ReadP2PMode(ref curText, ref item)
                    && ReadRemoteIp(ref curText, ref item)
                    && ReadRemotePort(ref curText, ref item))
                {
                    if (!config.PortMapList.Any(t => t.LocalPort == item.LocalPort))
                    {
                        config.PortMapList.Add(item);
                        LogDebug($"【PortMapItem配置项】读取成功：{item.LocalAddress}{(item.LocalAddress == "" ? "" : ":")}{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
                    }
                    else
                    {
                        item = null;
                        LogWarning($"【PortMapItem配置项】读取失败：端口{item.LocalPort}已存在映射配置项");
                    }
                }
                else
                {
                    item = null;
                    LogWarning($"【PortMapItem配置项】读取失败：无效的配置项 - {text}");
                }
            }, e =>
            {
                item = null;
                LogWarning($"【PortMapItem配置项】读取失败：无效的配置项 - {text}");
            });
            return item;
        }

        protected bool ReadLocalIp(ref string data, ref CModel.PortMapItem item)
        {
            data = data.Trim();
            int ipEndIndex = data.IndexOf(':');
            if (ipEndIndex < data.IndexOf("->"))
            {
                item.LocalAddress = data.Substring(0, ipEndIndex);
                data = data.Remove(0, ipEndIndex + 1);
            }
            return true;
        }

        protected bool ReadLocalPort(ref string data, ref CModel.PortMapItem item)
        {
            data = data.Trim();
            int portEndIndex = data.IndexOf("->");
            if (portEndIndex > 0)
            {
                int localPort;
                if (int.TryParse(data.Substring(0, portEndIndex), out localPort))
                {
                    item.LocalPort = localPort;
                    data = data.Remove(0, portEndIndex + 2);
                    return true;
                }
            }
            return false;
        }

        protected bool ReadP2PMode(ref string data, ref CModel.PortMapItem item)
        {
            data = data.Trim();
            int modeEndIndex = data.IndexOf("@[");
            if (modeEndIndex > 0)
            {
                int mode;
                if (int.TryParse(data.Substring(0, modeEndIndex), out mode))
                {
                    item.P2PType = mode;
                    data = data.Remove(0, modeEndIndex + 1);
                }
            }
            return true;
        }

        protected bool ReadRemoteIp(ref string data, ref CModel.PortMapItem item)
        {
            data = data.Trim();
            int remoteIpEndIndex = data.IndexOf(":");
            if (remoteIpEndIndex > 0)
            {
                string str = data.Substring(0, remoteIpEndIndex);
                if (str.StartsWith("[") && str.EndsWith("]"))
                {
                    item.MapType = PortMapType.servername;
                    item.RemoteAddress = str.Substring(1, str.Length - 2);
                }
                else
                {
                    item.MapType = PortMapType.ip;
                    item.RemoteAddress = str;
                }
                data = data.Remove(0, remoteIpEndIndex + 1);
                return true;
            }
            return false;
        }

        protected bool ReadRemotePort(ref string data, ref CModel.PortMapItem item)
        {
            data = data.Trim();
            int port;
            if (int.TryParse(data, out port))
            {
                item.RemotePort = port;
                return true;
            }
            return false;
        }

        public string GetItemString<T>(T item)
        {
            CModel.PortMapItem cItem = item as CModel.PortMapItem;
            if (cItem != null)
            {
                return (string.IsNullOrWhiteSpace(cItem.LocalAddress) ? "" : $"{cItem.LocalAddress}:")
                    + ($"{cItem.LocalPort}->")
                    + ((cItem.P2PType == 0 || cItem.MapType == PortMapType.ip) ? "" : $"{cItem.P2PType}@")
                    + (cItem.MapType == PortMapType.ip ? $"{cItem.RemoteAddress}" : $"[{cItem.RemoteAddress}]")
                    + ($":{cItem.RemotePort}");
            }
            else
            {
                throw new NotSupportedException($"不支持的类型{item.GetType().FullName}");
            }
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
    }
}
