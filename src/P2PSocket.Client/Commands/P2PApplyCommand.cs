using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using P2PSocket.Client.Models.Recieve;

namespace P2PSocket.Client.Commands
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
            if (step == 2)
            {
                bool isDestClient = m_data.ReadBoolean();
                int tokenLength = m_data.ReadInt32();
                string token = m_data.ReadBytes(tokenLength).ToStringUnicode();
                Models.Send.P2PApplyRequest sendPacket = new Models.Send.P2PApplyRequest(token);
                if (isDestClient)
                {
                    Debug.WriteLine("P2P第二步2：接收到P2P服务端命令，创建连接.");
                    int port = m_data.ReadInt32();
                    P2PTcpClient portClient = new P2PTcpClient("127.0.0.1", port);
                    P2PTcpClient serverClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                    portClient.IsAuth = serverClient.IsAuth = true;
                    portClient.ToClient = serverClient;
                    serverClient.ToClient = portClient;
                    serverClient.Client.Send(sendPacket.PackData());
                    Debug.WriteLine("P2P第三步2：向服务端发送tcp已连接消息.");
                    Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<RecievePacket>(serverClient); });
                }
                else
                {
                    if (Global.WaiteConnetctTcp.ContainsKey(token))
                    {
                        Debug.WriteLine("P2P第二步1：接收到P2P服务端命令，创建连接.");
                        P2PTcpClient portClient = Global.WaiteConnetctTcp[token];
                        Global.WaiteConnetctTcp.Remove(token);
                        P2PTcpClient serverClient = new P2PTcpClient(Global.ServerAddress, Global.ServerPort);
                        portClient.IsAuth = serverClient.IsAuth = true;
                        portClient.ToClient = serverClient;
                        serverClient.ToClient = portClient;
                        serverClient.Client.Send(sendPacket.PackData());
                        Debug.WriteLine("P2P第三步1：向服务端发送tcp已连接消息.");
                        Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<RecievePacket>(serverClient); });
                    }
                    else
                    {
                        Debug.WriteLine("P2P第二步：接收到P2P服务端命令，但tcp已超时.");
                    }
                }
            }
            else if (step == 4)
            {
                Debug.WriteLine("P2P第四步：启动p2p转发服务.");
                Global.TaskFactory.StartNew(() => { Global_Func.ListenTcp<P2PPacket>(m_tcpClient.ToClient); });
            }

            return true;
        }
    }
}
