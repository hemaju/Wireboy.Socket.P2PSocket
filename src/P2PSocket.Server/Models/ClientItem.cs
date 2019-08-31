using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Server.Models
{
    public class ClientItem
    {
        public string ClientName { set; get; }
        public string AuthCode { set; get; } = string.Empty;
        public bool Match(string clientName,string authCode)
        {
            return ClientName == clientName && AuthCode == authCode;
        }
    }
}
