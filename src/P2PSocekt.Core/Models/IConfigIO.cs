using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public interface IConfigIO
    {
        object ReadConfig(string text);

        string GetItemString<T>(T item);
        void WriteLog();
    }
}
