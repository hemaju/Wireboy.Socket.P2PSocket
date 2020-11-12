using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace P2PSocket.Client
{
    public class P2PResult
    {
        public P2PTcpClient Tcp { set; get; }
        public int P2PType { set; get; }
        public bool IsError { set; get; } = false;

        private string errorMsg;

        public object block { set; get; } = new object();
        public string ErrorMsg
        {
            get
            {
                return errorMsg;
            }
            set
            {
                errorMsg = value;
                IsError = true;
            }
        }
        public void PulseBlock()
        {
            Monitor.Enter(block);
            Monitor.Pulse(block);
            Monitor.Exit(block);
        }
    }
}
