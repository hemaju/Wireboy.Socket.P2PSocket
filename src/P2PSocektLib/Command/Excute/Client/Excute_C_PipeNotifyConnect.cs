using P2PSocektLib.Enum;
using P2PSocektLib.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command.Excute.Client
{
    [ClientExcute(RequestEnum.管道_通知双方握手)]
    internal class Excute_C_PipeNotifyConnect : IClientExcute
    {
        public async Task Handle(P2PClient client, P2PConnect conn, byte[] data, uint token)
        {
            ApiModel_NotifyCreatePipe? model = BaseService.BytesToModel<ApiModel_NotifyCreatePipe>(data);
            if (model == null)
            {
                throw new ArgumentException("参数错误");
            }
            // 建立与服务器的连接
            P2PConnect newTcp = new P2PConnect(NetworkType.Tcp);
            newTcp.Connect(client.Host, client.Port);
            // 建立管道
            P2PPipe pipe = new P2PPipe("Server", newTcp);
            _ = pipe.Open();
            // 发送创建消息
            Request_C_Service bus = new Request_C_Service();
            await bus.CreatePipe(conn.SendData, new ApiModel_CreatePipe(model.PipeToken) { });
            // 加入管道列表(这个管道是外部传入的，不支持本地使用）
            client.InPipeList.Add(pipe);
        }
    }
}
