using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Models;
using P2PSocket.Server.Models;

namespace P2PSocket.Server.Utils
{
    public static class ConfigUtils
    {
        public static bool IsExistConfig()
        {
            return File.Exists(Global.ConfigFile);
        }
        public static void LoadFromFile()
        {
            int mode = 0;
            using (StreamReader fs = new StreamReader(Global.ConfigFile))
            {
                while (!fs.EndOfStream)
                {
                    string lineStr = fs.ReadLine().Trim();
                    if (lineStr.Length > 0 && !lineStr.StartsWith("#"))
                    {
                        if (lineStr == "[Common]")
                        {
                            mode = 1;
                        }
                        else if (lineStr == "[PortMapItem]")
                        {
                            mode = 2;
                        }
                        else if (mode == 1)
                        {
                            string[] splitStr = lineStr.Split('=');
                            switch (splitStr[0].ToLower())
                            {
                                case "port":
                                    {
                                        Global.LocalPort = Convert.ToInt32(splitStr[1]);
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
                                case "allowclient":
                                    {
                                        string[] clientItems = splitStr[1].Split(',');
                                        foreach(string clientItem in clientItems)
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
                                                continue;
                                            }
                                            Global.ClientAuthList.Add(item);
                                        }
                                    }
                                    break;
                            }
                        }
                        else if (mode == 2)
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
