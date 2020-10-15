using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public interface IP2PSocketPlug
    {
        string GetPlugName();
        void Init();
    }
}
