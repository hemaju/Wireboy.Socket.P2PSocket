using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    internal class Request_S_Service : BaseService
    {
        #region 服务端 有token
        public async Task NotifyCreatePipe(Func<byte[], int, Task> sendFunc, ApiModel_NotifyCreatePipe model)
        {
            byte[] data = ModelToBytes(model);
            byte[] packData = CmdPacket.PackOne(data, 0, RequestEnum.管道_通知双方握手);
            await sendFunc(packData, packData.Length);
        }
        #endregion
    }
}
