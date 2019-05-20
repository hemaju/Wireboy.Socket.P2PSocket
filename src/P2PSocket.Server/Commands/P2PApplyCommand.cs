using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using P2PSocket.Server.Models.Recieve;
using P2PSocket.Server.Models.Send;
using System.Threading;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.P2P0x0201)]
    public class P2PApplyCommand : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public P2PApplyCommand(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            int step = m_data.ReadInt32();
            //是否第一步创建
            if (step == 1)
            {
                //token,servername,port
                int tokenLength = m_data.ReadInt32();
                string token = m_data.ReadBytes(tokenLength).ToStringUnicode();
                int clientNameLength = m_data.ReadInt32();
                string clientName = m_data.ReadBytes(clientNameLength).ToStringUnicode();
                int clientPort = m_data.ReadInt32();
                if (Global.TcpMap.ContainsKey(clientName) && Global.TcpMap[clientName].Connected)
                {
                    Debug.WriteLine("P2P第二步：向客户端发送P2P命令.");
                    P2PApplyRequest sendDPacket = new P2PApplyRequest(token, clientPort);
                    P2PApplyRequest sendSPacket = new P2PApplyRequest(token);
                    Global.TcpMap[clientName].Client.Send(sendDPacket.PackData());
                    m_tcpClient.Client.Send(sendSPacket.PackData());
                }
                else
                {
                    //发送客户端未在线
                    Debug.WriteLine("P2P第一步：服务端查询到客户端不在线.");
                }
            }
            else if (step == 3)
            {
                int tokenLength = m_data.ReadInt32();
                string token = m_data.ReadBytes(tokenLength).ToStringUnicode();
                if (Global.WaiteConnetctTcp.ContainsKey(token))
                {
                    Debug.WriteLine("P2P第三步：匹配成功.");
                    P2PTcpClient client = Global.WaiteConnetctTcp[token];
                    Global.WaiteConnetctTcp.Remove(token);
                    client.IsAuth = m_tcpClient.IsAuth = true;
                    client.ToClient = m_tcpClient;
                    m_tcpClient.ToClient = client;
                    P2PApplyRequest sendPacket = new P2PApplyRequest();
                    client.Client.Send(sendPacket.PackData());
                    m_tcpClient.Client.Send(sendPacket.PackData());
                }
                else
                {
                    Global.WaiteConnetctTcp.Add(token, m_tcpClient);
                    Debug.WriteLine("P2P第三步：将tcp加入待关联集合.");
                    Global.TaskFactory.StartNew(() => {
                        Thread.Sleep(Global.P2PTimeout);
                        if (Global.WaiteConnetctTcp.ContainsKey(token))
                        {
                            Debug.WriteLine("P2P第三步：5秒超时，关闭连接.");
                            Global.WaiteConnetctTcp[token].Close();
                            Global.WaiteConnetctTcp.Remove(token);
                        }
                        else
                        {
                            Debug.WriteLine("P2P第四步：5秒内成功连接.");
                        }
                    });
                }
            }
            return true;
        }
    }
}
