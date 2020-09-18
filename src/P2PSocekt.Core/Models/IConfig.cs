using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Models
{
    public interface IConfig
    {
        bool IsExistConfig();
        BaseConfig LoadFromFile();
        BaseConfig LoadFromString(string data);
        bool SaveItem<T>(T item);
        bool RemoveItem<T>(T item);
        object ParseToObject(string handlerName, string str);
    }
}
