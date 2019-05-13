using P2PSocket.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class CommandFlag : Attribute
    {
        public P2PCommandType CommandType { get; }
        public CommandFlag(P2PCommandType commandType)
        {
            CommandType = commandType;
        }
    }
}
