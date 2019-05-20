using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Models;

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
                            }
                        }
                        else if (mode == 2)
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
