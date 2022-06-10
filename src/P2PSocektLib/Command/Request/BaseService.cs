using P2PSocektLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    internal class BaseService
    {
        internal Utils_Uint_AsyncTask<byte[]> TaskUtil = new Utils_Uint_AsyncTask<byte[]>();
        /// <summary>
        /// Request计数锁
        /// </summary>
        object token_block = new object();
        uint _token = 0;
        public uint GetNewToken()
        {
            uint ret = 0;
            lock (token_block)
            {
                _token++;
                ret = _token;
                if (_token == uint.MaxValue)
                    _token = 0;
            }
            return ret;
        }
        /// <summary>
        /// 序列化Model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public static byte[] ModelToBytes<T>(T model)
        {
            return JsonSerializer.SerializeToUtf8Bytes(model);
        }

        /// <summary>
        /// 反序列化Model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T? BytesToModel<T>(byte[] data)
        {
            return JsonSerializer.Deserialize<T>(data);
        }
    }
}
