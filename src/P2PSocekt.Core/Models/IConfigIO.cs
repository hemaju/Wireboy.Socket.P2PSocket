using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public interface IConfigIO
    {
        void ReadConfig(string text);
        void WriteLog();
    }
}
