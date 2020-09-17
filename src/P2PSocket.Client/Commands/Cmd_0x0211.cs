using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Utils;
using System.Threading.Tasks;

namespace P2PSocket.Client.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0211)]
    public class Cmd_0x0211 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        TcpCenter tcpCenter = EasyInject.Get<TcpCenter>();
        AppConfig appCenter = EasyInject.Get<AppCenter>().Config;
        BinaryReader data { get; }
        public Cmd_0x0211(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            this.data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            LogUtils.Trace($"开始处理消息：0x0211");
            string token = BinaryUtils.ReadString(data);
            int mapPort = BinaryUtils.ReadInt(data);
            string remoteEndPoint = BinaryUtils.ReadString(data);
            bool isError = true;
            if (appCenter.AllowPortList.Any(t => t.Match(mapPort, m_tcpClient.ClientName)))
            {
                P2PTcpClient portClient = null;
                EasyOp.Do(() => { portClient = new P2PTcpClient("127.0.0.1", mapPort); }, () =>
                {
                    P2PTcpClient serverClient = null;
                    EasyOp.Do(() =>
                    {
                        serverClient = new P2PTcpClient(appCenter.ServerAddress, appCenter.ServerPort);
                    }, () =>
                    {
                        portClient.IsAuth = serverClient.IsAuth = true;
                        portClient.ToClient = serverClient;
                        serverClient.ToClient = portClient;
                        Models.Send.Send_0x0211 sendPacket = new Models.Send.Send_0x0211(token, true, "");
                        LogUtils.Debug($"命令：0x0211 正在绑定内网穿透（2端）通道 {portClient.RemoteEndPoint}->服务器->{remoteEndPoint}{Environment.NewLine}token:{token} ");
                        EasyOp.Do(() =>
                        {
                            serverClient.BeginSend(sendPacket.PackData());
                        }, () =>
                        {
                            EasyOp.Do(() =>
                            {
                                Global_Func.ListenTcp<Models.Receive.Packet_0x0212>(portClient);
                                Global_Func.ListenTcp<Models.Receive.Packet_ToPort>(serverClient);
                                isError = false;
                            }, ex =>
                            {
                                LogUtils.Debug($"命令：0x0211 接收数据发生错误:{Environment.NewLine}{ex}");
                                EasyOp.Do(() => { portClient?.SafeClose(); });
                                EasyOp.Do(() => { serverClient?.SafeClose(); });
                                SendError(token, $"客户端发生异常，{ex.Message}");
                            });
                        }, ex =>
                        {
                            LogUtils.Debug($"命令：0x0211 无法连接服务器:{Environment.NewLine}{ex}");
                            EasyOp.Do(() => { portClient?.SafeClose(); });
                            EasyOp.Do(() => { serverClient?.SafeClose(); });
                            SendError(token, $"向服务端发送数据失败");
                        });
                    }, ex =>
                    {
                        LogUtils.Debug($"命令：0x0211 无法连接服务器:{Environment.NewLine}{ex}");
                        EasyOp.Do(() => { portClient?.SafeClose(); });
                        SendError(token, $"无法建立到服务端的tcp连接");
                    });
                }, ex =>
                {
                    LogUtils.Debug($"命令：0x0211 建立tcp连接[127.0.0.1:{mapPort}]失败:{Environment.NewLine}{ex}");
                    SendError(token, $"目标端口{mapPort}连接失败！");
                });
            }
            else
            {
                LogUtils.Debug($"命令：0x0211 已拒绝服务端连接本地端口[{mapPort}]，不在AllowPort配置项的允许范围内");
                SendError(token, $"无权限访问端口{mapPort}，请配置AllowPort");
            }
            return true;
        }
        private void SendError(string token, string msg)
        {
            EasyOp.Do(() =>
            {
                Models.Send.Send_0x0211 sendPacket = new Models.Send.Send_0x0211(token, false, msg);
                m_tcpClient.BeginSend(sendPacket.PackData());
            });
        }
    }
}
