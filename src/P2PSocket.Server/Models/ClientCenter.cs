using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server
{
    public class ClientCenter
    {
        AppCenter appCenter = EasyInject.Get<AppCenter>();
        public ClientCenter()
        {
        }
        /// <summary>
        ///     等待中的tcp连接
        /// </summary>
        public Dictionary<string, P2PTcpClient> WaiteConnetctTcp = new Dictionary<string, P2PTcpClient>();
        /// <summary>
        ///     客户端的tcp映射<服务名,tcp>
        /// </summary>
        public Dictionary<string, P2PTcpItem> TcpMap = new Dictionary<string, P2PTcpItem>();


        public string GetClientName(string macAddress)
        {
            string clientName = "";
            if(appCenter.Config.MacAddressMap.ContainsKey(macAddress))
            {
                clientName = appCenter.Config.MacAddressMap[macAddress];
            }
            else
            {
                clientName = appCenter.Config.RegisterMacAddress(macAddress);
            }
            return clientName;
        }
    }
}
