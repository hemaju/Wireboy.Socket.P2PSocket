using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Models;

namespace P2PSocket.Client.Utils
{
    public static class ConfigUtils
    {
        public static bool IsExistConfig()
        {
            return File.Exists(Global.ConfigFile);
        }
        public static void LoadFromFile()
        {
            string mode = "None";
            using (StreamReader fs = new StreamReader(Global.ConfigFile))
            {
                while (!fs.EndOfStream)
                {
                    string lineStr = fs.ReadLine().Trim();
                    if (lineStr.Length > 0 && !lineStr.StartsWith("#"))
                    {
                        if (lineStr.ToLower() == "[common]")
                        {
                            mode = "common";
                        }
                        else if (lineStr.ToLower() == "[portmapitem]")
                        {
                            mode = "portmapitem";
                        }
                        else if (mode == "common")
                        {
                            string[] splitStr = lineStr.Split('=');
                            if (splitStr.Length == 2)
                            {
                                switch (splitStr[0].ToLower())
                                {
                                    case "serveraddress":
                                        {
                                            string[] ipStr = splitStr[1].Split(':');
                                            if (ipStr.Length == 2)
                                            {
                                                Global.ServerAddress = ipStr[0];
                                                Global.ServerPort = Convert.ToInt32(ipStr[1]);
                                                P2PTcpClient.Proxy.Address.Add(Global.ServerAddress);
                                            }
                                        }
                                        break;
                                    case "clientname":
                                        {
                                            Global.ClientName = splitStr[1];
                                        }
                                        break;
                                    case "authcode":
                                        {
                                            Global.AuthCode = splitStr[1];
                                        }
                                        break;
                                    case "allowport":
                                        {
                                            string[] portList = splitStr[1].Split(',');
                                            foreach (string portStr in portList)
                                            {
                                                AllowPortItem portItem = new AllowPortItem(portStr);
                                                Global.AllowPortList.Add(portItem);
                                            }
                                        };
                                        break;
                                    case "blacklist":
                                        {
                                            string[] blackList = splitStr[1].Split(',');
                                            foreach (string value in blackList)
                                            {
                                                Global.BlackClients.Add(value);
                                            }
                                        }
                                        break;
                                    case "loglevel":
                                        {
                                            string levelName = splitStr[1].ToLower();
                                            switch (levelName)
                                            {
                                                case "debug": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Debug; break;
                                                case "error": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Error; break;
                                                case "info": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Info; break;
                                                case "none": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.None; break;
                                                case "warning": LogUtils.Instance.LogLevel = Core.Utils.LogLevel.Warning; break;
                                                default: LogUtils.Instance.LogLevel = Core.Utils.LogLevel.None; break;
                                            }
                                        }
                                        break;
                                    case "proxy_ip":
                                        {
                                            string[] portList = splitStr[1].Split(':');
                                            if (portList.Length == 3)
                                            {
                                                P2PTcpClient.Proxy.ProxyType = portList[0];
                                                P2PTcpClient.Proxy.IP = portList[1];
                                                P2PTcpClient.Proxy.Port = Convert.ToInt32(portList[2]);
                                            }
                                        }
                                        break;
                                    case "proxy_username":
                                        {
                                            P2PTcpClient.Proxy.UserName = splitStr[1];
                                        }
                                        break;
                                    case "proxy_password":
                                        {
                                            P2PTcpClient.Proxy.Password = splitStr[1];
                                        }
                                        break;
                                }
                            }
                        }
                        else if (mode == "portmapitem")
                        {
                            int centerSplitIndexOf = lineStr.IndexOf("->");
                            string localStr = lineStr.Substring(0, centerSplitIndexOf);
                            string remoteStr = lineStr.Substring(centerSplitIndexOf + 2);

                            string[] localStrList = localStr.Split(':');
                            string localIp = localStrList.Length >= 2 ? localStrList[0] : "";
                            string portStr = localStrList.Length >= 2 ? localStrList[1] : localStr;

                            int port = 0;
                            if (int.TryParse(portStr, out port) && port > 0)
                            {
                                if (!Global.PortMapList.Any(t => t.LocalPort == port))
                                {
                                    PortMapItem item = new PortMapItem();
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
                                    Global.PortMapList.Add(item);
                                }
                            }
                        }
                    }

                }
            }
        }

        public static void SaveToFile()
        {

        }
    }
}
