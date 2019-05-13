using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace P2PSocket.Core.Extends
{
    public static class StringEx
    {
        public static byte[] ToBytes(this string str)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(str);
            return bytes;
        }
    }
}
