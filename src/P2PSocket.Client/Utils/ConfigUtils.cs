using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using P2PSocket.Core.Models;
using System.Reflection;

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
            using (StreamReader fs = new StreamReader(Global.ConfigFile))
            {
                Dictionary<string, IConfigIO> handleDictionary = GetConfigIOInstanceList();
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
            }
        }

        public static void SaveToFile()
        {

        }
        public static Dictionary<string, IConfigIO> GetConfigIOInstanceList()
        {
            Dictionary<string, IConfigIO> retDic = new Dictionary<string, IConfigIO>();
            Type[] configIOList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ConfigIOAttr>() != null).ToArray();
            foreach (Type type in configIOList)
            {
                retDic.Add(type.GetCustomAttribute<ConfigIOAttr>().Name, Activator.CreateInstance(type) as IConfigIO);
            }
            return retDic;
        }
    }
}
