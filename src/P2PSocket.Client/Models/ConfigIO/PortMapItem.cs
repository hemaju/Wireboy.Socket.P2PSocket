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
    [ConfigIOAttr("[PortMapItem]")]
    public class PortMapItem : IConfigIO
    {
        public List<LogInfo> MessageList = new List<LogInfo>();
        ConfigCenter config = null;
        public PortMapItem(ConfigCenter config)
        {
            this.config = config;
        }

        public void ReadConfig(string text)
        {
            int centerSplitIndexOf = text.IndexOf("->");
            string localStr = text.Substring(0, centerSplitIndexOf);
            string remoteStr = text.Substring(centerSplitIndexOf + 2);

            string[] localStrList = localStr.Split(':');
            string localIp = localStrList.Length >= 2 ? localStrList[0] : "";
            string portStr = localStrList.Length >= 2 ? localStrList[1] : localStr;

            int port = 0;
            if (int.TryParse(portStr, out port) && port > 0)
            {
                if (!config.PortMapList.Any(t => t.LocalPort == port))
                {
                    Core.Models.PortMapItem item = new Core.Models.PortMapItem();

                    int typeIndex = remoteStr.IndexOf("@");
                    if (typeIndex >= 0)
                    {
                        try
                        {
                            item.P2PType = Convert.ToInt32(remoteStr.Substring(0, typeIndex));
                            remoteStr = remoteStr.Substring(typeIndex + 1);
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"【PortMapItem配置项】读取失败：无效的配置项 - {text}");
                            return;
                        }
                    }
                    else
                    {
                        item.P2PType = 0;
                    }
                    string[] remoteStrList = remoteStr.Split(':');
                    item.LocalPort = port;
                    item.LocalAddress = localIp;
                    if (remoteStrList[0].StartsWith("[") && remoteStrList[0].EndsWith("]"))
                    {
                        item.MapType = PortMapType.servername;
                        item.RemoteAddress = remoteStrList[0].Substring(1, remoteStrList[0].Length - 2);
                    }
                    else
                    {
                        item.MapType = PortMapType.ip;
                        item.RemoteAddress = remoteStrList[0];
                    }
                    item.RemotePort = Convert.ToInt32(remoteStrList[1]);
                    config.PortMapList.Add(item);
                    LogDebug($"【PortMapItem配置项】读取成功：{item.LocalAddress}{(item.LocalAddress == "" ? "" : ":")}{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}");
                }
                else
                {
                    LogWarning($"【PortMapItem配置项】读取失败：端口{port}已存在映射配置项");
                }
            }
            else
            {
                LogWarning($"【PortMapItem配置项】读取失败：无效的配置项 - {text}");
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
