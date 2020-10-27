using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Extends
{
    public static class ConcurrentDictionaryEx
    {
        public static bool Remove<T1, T2>(this ConcurrentDictionary<T1, T2> dic, T1 key)
        {
            return dic.TryRemove(key, out _);
        }
        public static bool Add<T1, T2>(this ConcurrentDictionary<T1, T2> dic, T1 key, T2 value)
        {
            return dic.TryAdd(key, value);
        }
    }
}
