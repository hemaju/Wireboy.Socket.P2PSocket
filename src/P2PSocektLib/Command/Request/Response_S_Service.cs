using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    internal class Response_S_Service : BaseService
    {
        /// <summary>
        /// 登录（匿名）
        /// </summary>
        /// <param name="sendFunc">[方法]发送数据</param>
        /// <param name="model">登录的数据</param>
        /// <returns></returns>
        public async Task Login(Func<byte[], int, Task> sendFunc, uint token, ApiModel_Login_R model)
        {
            byte[] data = ModelToBytes(model);
            byte[] packData = CmdPacket.PackOne(data, token, RequestEnum.客户端认证, false);
            await sendFunc(packData, packData.Length);
        }
    }
}
