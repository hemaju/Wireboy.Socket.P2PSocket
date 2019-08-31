using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public interface IObjectToString
    {
        string ToString();
        void ToObject(string data);
    }
}
