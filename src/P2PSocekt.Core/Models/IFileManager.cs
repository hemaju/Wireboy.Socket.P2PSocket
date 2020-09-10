using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public abstract class IFileManager
    {
        public const string Config = "Config";
        public const string Log = "Log";
        public abstract bool IsExist(string fileType);

        public abstract bool Create(string fileType);
        public abstract string ReadAll(string fileType);
        public abstract void ReadLine(string fileType, Action<string> func);
        public abstract void WriteAll(string fileType, string text, bool isAppend = true);
        public abstract void ForeachWrite(string fileType, Action<Action<string>> func, bool isAppend = true);
    }
}
