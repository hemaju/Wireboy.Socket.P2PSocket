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
                        if (lineStr == "[Common]")
                        {
                            mode = "Common";
                        }
                        else if (lineStr == "[PortMapItem]")
                        {
                            mode = "PortMapItem";
                        }
                        else if (mode == "Common")
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
                                            }
                                        }
                                        break;
                                    case "clientname":
                                        {
                                            Global.ClientName = splitStr[1];
                                        }
                                        break;
                                    case "allowport":
                                        {
                                            string[] portList = splitStr[1].Split(',');
                                            foreach (string portStr in portList)
                                            {
                                                int port = 0;
                                                if (int.TryParse(portStr, out port) && !Global.AllowPort.Contains(port))
                                                {
                                                    Global.AllowPort.Add(port);
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else if (mode == "PortMapItem")
                        {
                            string[] splitStr = lineStr.Split(new char[2] { '-', '>' });
                            int port = 0;
                            if (int.TryParse(splitStr[0], out port) && port > 0)
                            {
                                if (!Global.PortMapList.Any(t => t.LocalPort == port))
                                {
                                    string[] remoteStr = splitStr[2].Split(':');
                                    PortMapItem item = new PortMapItem();
                                    item.LocalPort = port;
                                    if (remoteStr[0].StartsWith("[") && remoteStr[0].EndsWith("]"))
                                    {
                                        item.MapType = PortMapType.servername;
                                        item.RemoteAddress = remoteStr[0].Substring(1, remoteStr[0].Length - 2);
                                    }
                                    else
                                    {
                                        item.MapType = PortMapType.ip;
                                        item.RemoteAddress = remoteStr[0];
                                    }
                                    item.RemotePort = Convert.ToInt32(remoteStr[1]);
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
