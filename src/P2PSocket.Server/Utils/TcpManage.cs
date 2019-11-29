using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Utils
{
    public class TcpManage
    {
        Dictionary<string, P2PStack<DateTime>> TcpConnectTimeHis = new Dictionary<string, P2PStack<DateTime>>();
        int MaxCount = 0;
        public TcpManage(int maxCount)
        {
            MaxCount = maxCount;
        }
        public void AddTcp(string tcpAddress)
        {
            if (TcpConnectTimeHis.ContainsKey(tcpAddress))
            {
                TcpConnectTimeHis[tcpAddress].Push(DateTime.Now);
            }
            else
            {
                TcpConnectTimeHis.Add(tcpAddress, new P2PStack<DateTime>(MaxCount, DateTime.Now));
            }
        }
        public bool IsAllowConnect(string tcpAddress)
        {
            if (TcpConnectTimeHis.ContainsKey(tcpAddress))
            {
                P2PStack<DateTime> stackItem = TcpConnectTimeHis[tcpAddress];
                if (stackItem.Count == stackItem.MaxLength)
                {
                    //20分钟内，连接次数超过上限则拒绝连接
                    if (stackItem.First().AddMinutes(10) > DateTime.Now)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
