using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class AllowPortItem : IObjectToString
    {
        public AllowPortItem(string data)
        {
            ToObject(data);
        }

        public AllowPortItem()
        {
        }

        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public List<string> AllowClients { get; } = new List<string>();

        /// <summary>
        ///     0-0:test01|test02|test03
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void ToObject(string data)
        {
            int centerSplit = data.IndexOf(':');
            if (centerSplit > -1)
            {
                //  有客户端限制
                string portRange = data.Substring(0, centerSplit);
                ParsePort(portRange);
                string clientRange = data.Substring(centerSplit+1);
                ParseClient(clientRange);
            }
            else
            {
                //  无客户端限制
                ParsePort(data);
            }
        }

        protected void ParsePort(string data)
        {
            string[] portList = data.Split('-');
            if (portList.Length == 1)
            {
                // 指定端口
                MinValue = MaxValue = Convert.ToInt32(portList[0]);
            }
            else if (portList.Length == 2)
            {
                //  端口范围
                MinValue = Convert.ToInt32(portList[0]);
                MaxValue = Convert.ToInt32(portList[1]);
            }
        }

        protected void ParseClient(string data)
        {
            string[] clients = data.Split('|');
            AllowClients.AddRange(clients);
        }

        public bool Match(int port, string clientName)
        {
            bool ret = false;
            if ((0 == MinValue && 0 == MaxValue) || (MinValue <= port && MaxValue >= port))
                if (AllowClients.Count == 0 || AllowClients.Contains(clientName))
                    ret = true;
            return ret;
        }

        public override string ToString()
        {
            string retStr = "";
            if (AllowClients.Count > 0)
                retStr = string.Format($"{MinValue}-{MaxValue}:{string.Join("|", AllowClients)}");
            else
                retStr = string.Format($"{MinValue}-{MaxValue}");
            return retStr;
        }
    }
}
