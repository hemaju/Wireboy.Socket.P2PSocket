using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Commands
{
    public abstract class P2PCommand 
    {
        protected string Error { set; get; }
        public abstract bool Excute();
        public string GetExcuteError()
        {
            return Error;
        }
    }
}
