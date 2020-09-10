using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using P2PSocket.Server.Models.Receive;
using P2PSocket.Server.Models.Send;
using System.Threading;
using P2PSocket.Server.Models;
using P2PSocket.Core.Utils;
using System.Linq;
using P2PSocket.Server.Utils;
using System.Threading.Tasks;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0201)]
    public class Cmd_0x0201 : P2PCommand
    {
        static Dictionary<string, int> p2pTypeDict = new Dictionary<string, int>();
        ClientCenter clientCenter = EasyInject.Get<ClientCenter>();
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        AppCenter appCenter = EasyInject.Get<AppCenter>();
        public Cmd_0x0201(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            LogUtils.Trace($"开始处理消息：0x0201");
            int step = BinaryUtils.ReadInt(m_data);
            //是否第一步创建
            if (step == 1)
            {
                //token,servername,port
                string token = BinaryUtils.ReadString(m_data);
                string clientName = BinaryUtils.ReadString(m_data);
                int clientPort = BinaryUtils.ReadInt(m_data);
                int p2pType = BinaryUtils.ReadInt(m_data);
                if (p2pTypeDict.ContainsKey(token))
                    p2pTypeDict[token] = p2pType;
                else
                    p2pTypeDict.Add(token, p2pType);
                P2PStart_ServerTransfer(token, clientName, clientPort, p2pType);

            }
            else if (step == 3)
            {
                string clientName = BinaryUtils.ReadString(m_data);
                m_tcpClient.ClientName = clientName;
                string token = BinaryUtils.ReadString(m_data);
                if (clientCenter.WaiteConnetctTcp.ContainsKey(token))
                {
                    if (p2pTypeDict.ContainsKey(token) && p2pTypeDict[token] == 1)
                    {
                        P2PBind_DirectConnect(token);
                    }
                    else
                    {
                        P2PBind_ServerTransfer(token);
                    }
                    p2pTypeDict.Remove(token);
                }
                else
                {
                    clientCenter.WaiteConnetctTcp.Add(token, m_tcpClient);
                    LogUtils.Debug($"正在等待隧道连接绑定 token:{token}.");
                    EasyInject.Get<AppCenter>().StartNewTask(() =>
                    {
                        Thread.Sleep(appCenter.Config.P2PWaitConnectTime);
                        if (clientCenter.WaiteConnetctTcp.ContainsKey(token))
                        {
                            LogUtils.Debug($"等待隧道连接绑定已超时  token:{token}.");
                            EasyOp.Do(() => m_tcpClient.SafeClose());
                            clientCenter.WaiteConnetctTcp.Remove(token);
                            p2pTypeDict.Remove(token);
                        }
                    });
                }
            }
            return true;
        }

        public void P2PStart_ServerTransfer(string token, string clientName, int clientPort, int p2pType)
        {
            P2PTcpItem item = null;
            if (clientCenter.TcpMap.ContainsKey(clientName))
            {
                item = clientCenter.TcpMap[clientName];
            }
            if (item != null && item.TcpClient.Connected)
            {
                if (item.BlackClients.Contains(m_tcpClient.ClientName))
                {
                    Send_0x0201_Failure sendPacket = new Send_0x0201_Failure($"客户端{clientName}已被加入黑名单");
                    LogUtils.Warning($"建立隧道失败，客户端{clientName}已被加入黑名单");
                    EasyOp.Do(() => m_tcpClient.BeginSend(sendPacket.PackData()));
                }
                else if (item.AllowPorts.Any(t => t.Match(clientPort, m_tcpClient.ClientName)))
                {
                    LogUtils.Debug($"通知客户端开始建立中转模式隧道 token{token}");
                    Send_0x0201_Success sendDPacket = new Send_0x0201_Success(token, clientPort, p2pType);
                    Send_0x0201_Success sendSPacket = new Send_0x0201_Success(token, p2pType);
                    clientCenter.TcpMap[clientName].TcpClient.BeginSend(sendDPacket.PackData());
                    EasyOp.Do(() => m_tcpClient.BeginSend(sendSPacket.PackData()));
                }
                else
                {
                    Send_0x0201_Failure sendPacket = new Send_0x0201_Failure($"未获得授权，无法建立隧道，端口{clientPort}");
                    LogUtils.Debug($"未获得授权，无法建立隧道，端口{clientPort}");
                    EasyOp.Do(() => m_tcpClient.BeginSend(sendPacket.PackData()));
                }
            }
            else
            {
                //发送客户端未在线
                LogUtils.Debug($"【P2P】客户端{clientName}不在线.");
                Send_0x0201_Failure sendPacket = new Send_0x0201_Failure($"客户端{clientName}不在线");
                EasyOp.Do(() => m_tcpClient.BeginSend(sendPacket.PackData()));
            }
        }
        public void P2PBind_ServerTransfer(string token)
        {
            P2PTcpClient client = clientCenter.WaiteConnetctTcp[token];
            clientCenter.WaiteConnetctTcp.Remove(token);
            client.IsAuth = m_tcpClient.IsAuth = true;
            client.ToClient = m_tcpClient;
            m_tcpClient.ToClient = client;
            Send_0x0201_Success sendPacket = new Send_0x0201_Success(4);
            EasyOp.Do(() => client.BeginSend(sendPacket.PackData()));
            EasyOp.Do(() => m_tcpClient.BeginSend(sendPacket.PackData()));
            LogUtils.Debug($"成功建立中转模式隧道 token:{token}");
        }

        public void P2PBind_DirectConnect(string token)
        {
            P2PTcpClient clientA = clientCenter.WaiteConnetctTcp[token];
            P2PTcpClient clientB = m_tcpClient;

            Send_0x0201_Success sendPacketA = new Send_0x0201_Success(14);
            sendPacketA.WriteDirectData(clientB.Client.RemoteEndPoint.ToString(), token);
            Send_0x0201_Success sendPacketB = new Send_0x0201_Success(14);
            sendPacketB.WriteDirectData(clientA.Client.RemoteEndPoint.ToString(), token);

            EasyOp.Do(() => clientA.BeginSend(sendPacketA.PackData()));
            EasyOp.Do(() => clientB.BeginSend(sendPacketB.PackData()));

            EasyOp.Do(() => clientA?.SafeClose());
            EasyOp.Do(() => clientB?.SafeClose());
        }
    }
}
