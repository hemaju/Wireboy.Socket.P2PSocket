using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models.Receive;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0211)]
    public class Cmd_0x0211 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        ClientCenter clientCenter = EasyInject.Get<ClientCenter>();
        public Cmd_0x0211(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            bool ret = true;
            LogUtils.Trace($"开始处理消息：0x0211");
            string token = BinaryUtils.ReadString(m_data);
            bool isSuccess = true;
            string msg = "";
            if (m_data.PeekChar() >= 0)
            {
                isSuccess = BinaryUtils.ReadBool(m_data);
                msg = BinaryUtils.ReadString(m_data);
            }
            if (isSuccess)
            {
                if (clientCenter.WaiteConnetctTcp.ContainsKey(token))
                {
                    P2PTcpClient client = clientCenter.WaiteConnetctTcp[token];
                    clientCenter.WaiteConnetctTcp.Remove(token);
                    client.IsAuth = m_tcpClient.IsAuth = true;
                    client.ToClient = m_tcpClient;
                    m_tcpClient.ToClient = client;
                    LogUtils.Debug($"命令：0x0211 已绑定内网穿透（2端）通道 {client.RemoteEndPoint}->{m_tcpClient.RemoteEndPoint}");
                    //监听client
                    EasyOp.Do(() => Global_Func.ListenTcp<Packet_0x0212>(client), ex =>
                    {
                        LogUtils.Debug($"命令：0x0211 绑定内网穿透（2端）通道失败，目标Tcp连接已断开");
                        ret = false;
                    });
                }
                else
                {
                    LogUtils.Debug($"命令：0x0211 绑定内网穿透（2端）通道失败，绑定超时");
                    ret = false;
                }
            }
            else
            {
                //失败消息是客户端与服务端的通讯tcp发送的，不能关闭tcp连接
                LogUtils.Debug($"命令：0x0211 From 客户端：{msg}  token:{token}");
                if (clientCenter.WaiteConnetctTcp.ContainsKey(token))
                {
                    //关闭源tcp
                    P2PTcpClient client = clientCenter.WaiteConnetctTcp[token];
                    EasyOp.Do(() => {
                        client?.SafeClose();
                    });
                    clientCenter.WaiteConnetctTcp.Remove(token);
                }
                ret = true;
            }

            return ret;
        }
    }
}
