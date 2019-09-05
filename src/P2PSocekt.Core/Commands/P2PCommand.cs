using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Commands
{
    public abstract class P2PCommand : IDisposable
    {
        protected string Error { set; get; }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public abstract bool Excute();
        public string GetExcuteError()
        {
            return Error;
        }
        public virtual bool IsMatch(byte[] data)
        {
            return true;
        }
    }
}
