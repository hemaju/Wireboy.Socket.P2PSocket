using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    /// <summary>
    /// 管道内的通讯
    /// </summary>
    internal class Request_Pipe_Service : BaseService
    {
        public async Task<ApiModel_Pipe_NotifyCreateConn_R?> NotifyCreateConn(Func<byte[], int, Task> sendFunc, ApiModel_Pipe_NotifyCreateConn model)
        {
            byte[] data = ModelToBytes(model);
            uint token = GetNewToken();
            byte[] packData = CmdPacket.PackOne(data, token, RequestEnum.管道_新建连接);
            byte[] res = await TaskUtil.Wait(token, () => sendFunc(packData, packData.Length));
            return BytesToModel<ApiModel_Pipe_NotifyCreateConn_R>(res);

        }
        public async Task NotifyCloseConn(Func<byte[], int, Task> sendFunc, ApiModel_Pipe_NotifyCloseConn model)
        {
            byte[] data = ModelToBytes(model);
            byte[] packData = CmdPacket.PackOne(data, 0, RequestEnum.管道_断开连接);
            await sendFunc(packData, packData.Length);
        }
    }
}
