using P2PSocektLib.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command.Excute.Server
{
    [ServerExcute(RequestEnum.客户端认证, Role.Unknow)]
    internal class Excute_S_Login : IServerExcute
    {
        public async Task Handle(P2PServer server, P2PConnect conn, byte[] data, uint token)
        {
            ApiModel_Login? model = BaseService.BytesToModel<ApiModel_Login>(data);
            if (model == null)
            {
                conn.Close();
            }
            else
            {
                ApiModel_Login_R res = new ApiModel_Login_R();
                res.LoginToken = server.NewClientToken();
                res.ClientCode = DateTime.Now.ToString("客户端A");
                await server.Bus_Response.Login(conn.SendData, token, res);
            }
        }
    }
}
