using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Extends
{
    public static class BytesEx
    {
        public static String ToStringUnicode(this byte[] data)
        {
            return Encoding.Unicode.GetString(data);
        }
    }
}
