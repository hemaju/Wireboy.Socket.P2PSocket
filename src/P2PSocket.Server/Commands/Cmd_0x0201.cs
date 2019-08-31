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

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0201)]
    public class Cmd_0x0201 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0201(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            int step = BinaryUtils.ReadInt(m_data);
            //是否第一步创建
            if (step == 1)
            {
                //token,servername,port
                string token = BinaryUtils.ReadString(m_data);
                string clientName = BinaryUtils.ReadString(m_data);
                int clientPort = BinaryUtils.ReadInt(m_data);

                P2PTcpItem item = null;
                if (Global.TcpMap.ContainsKey(clientName))
                {
                    item = Global.TcpMap[clientName];
                }
                if (item != null && item.TcpClient.Connected)
                {
                    if (item.BlackClients.Contains(m_tcpClient.ClientName))
                    {
                        Send_0x0201_Failure sendPacket = new Send_0x0201_Failure($"客户端{clientName}已被加入黑名单");
                        LogUtils.Warning($"【P2P】客户端{clientName}已被加入黑名单");
                        m_tcpClient.Client.Send(sendPacket.PackData());
                    }
                    else if (item.AllowPorts.Any(t => t.Match(clientPort,m_tcpClient.ClientName)))
                    {
                        LogUtils.Debug("【P2P】等待Tcp连接，进行绑定");
                        Send_0x0201_Success sendDPacket = new Send_0x0201_Success(token, clientPort);
                        Send_0x0201_Success sendSPacket = new Send_0x0201_Success(token);
                        Global.TcpMap[clientName].TcpClient.Client.Send(sendDPacket.PackData());
                        m_tcpClient.Client.Send(sendSPacket.PackData());
                    }
                    else
                    {
                        Send_0x0201_Failure sendPacket = new Send_0x0201_Failure($"没有权限，端口{clientPort}");
                        LogUtils.Debug($"【P2P】没有权限，端口{clientPort}");
                        m_tcpClient.Client.Send(sendPacket.PackData());
                    }
                }
                else
                {
                    //发送客户端未在线
                    LogUtils.Debug($"【P2P】客户端{clientName}不在线.");
                    Send_0x0201_Failure sendPacket = new Send_0x0201_Failure($"客户端{clientName}不在线");
                    m_tcpClient.Client.Send(sendPacket.PackData());
                }
            }
            else if (step == 3)
            {
                string clientName = BinaryUtils.ReadString(m_data);
                m_tcpClient.ClientName = clientName;
                string token = BinaryUtils.ReadString(m_data);
                if (Global.WaiteConnetctTcp.ContainsKey(token))
                {
                    LogUtils.Debug($"【P2P】内网穿透成功");
                    P2PTcpClient client = Global.WaiteConnetctTcp[token];
                    Global.WaiteConnetctTcp.Remove(token);
                    client.IsAuth = m_tcpClient.IsAuth = true;
                    client.ToClient = m_tcpClient;
                    m_tcpClient.ToClient = client;
                    Send_0x0201_Success sendPacket = new Send_0x0201_Success();
                    client.Client.Send(sendPacket.PackData());
                    m_tcpClient.Client.Send(sendPacket.PackData());
                }
                else
                {
                    Global.WaiteConnetctTcp.Add(token, m_tcpClient);
                    LogUtils.Debug("【P2P】等待目标tcp.");
                    Global.TaskFactory.StartNew(() =>
                    {
                        Thread.Sleep(Global.P2PTimeout);
                        if (Global.WaiteConnetctTcp.ContainsKey(token))
                        {
                            LogUtils.Debug("【P2P】已超时，内网穿透失败.");
                            Global.WaiteConnetctTcp[token].Close();
                            Global.WaiteConnetctTcp.Remove(token);
                        }
                    });
                }
            }
            return true;
        }
    }
}
