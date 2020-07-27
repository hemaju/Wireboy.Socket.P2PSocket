using P2PSocket.Client.Utils;
using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace P2PSocket.Client
{

    public static class Global_Func
    {
        public struct RelationTcp_Server
        {
            public P2PTcpClient readTcp;
            public NetworkStream readSs;
            public byte[] buffer;
            public ReceivePacket msgReceive;
            public Guid guid;
        }
        public static void ListenTcp<T>(P2PTcpClient tcpClient) where T : ReceivePacket
        {
            TcpCenter.Instance.ConnectedTcpList.Add(tcpClient);
            try
            {
                RelationTcp_Server relationSt = new RelationTcp_Server();
                relationSt.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                relationSt.readTcp = tcpClient;
                relationSt.readSs = tcpClient.GetStream();
                relationSt.msgReceive = Activator.CreateInstance(typeof(T)) as ReceivePacket;
                relationSt.guid = AppCenter.Instance.CurrentGuid;
                relationSt.readSs.BeginRead(relationSt.buffer, 0, relationSt.buffer.Length, ReadTcp_Server, relationSt);
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【错误】Global_Func.ListenTcp：{Environment.NewLine}{ex}");
            }
        }

        public static void ReadTcp_Server(IAsyncResult ar)
        {
            RelationTcp_Server relation = (RelationTcp_Server)ar.AsyncState;
            try
            {
                if (relation.guid == AppCenter.Instance.CurrentGuid)
                {
                    if (relation.readSs.CanRead)
                    {
                        int length = 0;
                        length = relation.readSs.EndRead(ar);
                        if (length > 0)
                        {
                            byte[] refData = relation.buffer.Take(length).ToArray();
                            while (relation.msgReceive.ParseData(ref refData))
                            {
                                LogUtils.Debug($"命令类型:{relation.msgReceive.CommandType}");
                                // 执行command
                                using (P2PCommand command = FindCommand(relation.readTcp, relation.msgReceive))
                                {
                                    command?.Excute();
                                }
                                //重置msgReceive
                                relation.msgReceive.Reset();
                                if (refData.Length <= 0) break;
                            }
                            relation.readSs.BeginRead(relation.buffer, 0, relation.buffer.Length, ReadTcp_Server, relation);
                            return;
                        }
                    }
                }
            }
            catch { }
            LogUtils.Debug($"tcp连接{relation.readTcp.RemoteEndPoint}已断开");
            relation.readSs.Close(3000);
            relation.readTcp.SafeClose();
            if (relation.readTcp.ToClient != null)
                relation.readTcp.ToClient.SafeClose();
            if (TcpCenter.Instance.ConnectedTcpList.Contains(relation.readTcp)) TcpCenter.Instance.ConnectedTcpList.Remove(relation.readTcp);
        }


        public static void BindTcp(P2PTcpClient readTcp, P2PTcpClient toTcp)
        {
            TcpCenter.Instance.ConnectedTcpList.Add(readTcp);
            TcpCenter.Instance.ConnectedTcpList.Add(toTcp);
            RelationTcp_Ip toRelation = new RelationTcp_Ip();
            toRelation.readTcp = readTcp;
            toRelation.readSs = readTcp.GetStream();
            toRelation.writeTcp = toTcp;
            toRelation.writeSs = toTcp.GetStream();
            toRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
            RelationTcp_Ip fromRelation = new RelationTcp_Ip();
            fromRelation.readTcp = toRelation.writeTcp;
            fromRelation.readSs = toRelation.writeSs;
            fromRelation.writeTcp = toRelation.readTcp;
            fromRelation.writeSs = toRelation.readSs;
            fromRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
            StartTransferTcp_Ip(toRelation);
            StartTransferTcp_Ip(fromRelation);
        }

        private static void StartTransferTcp_Ip(RelationTcp_Ip tcp)
        {
            tcp.readTcp.GetStream().BeginRead(tcp.buffer, 0, tcp.buffer.Length, TransferTcp_Ip, tcp);
        }
        private static void TransferTcp_Ip(IAsyncResult ar)
        {
            RelationTcp_Ip relation = (RelationTcp_Ip)ar.AsyncState;

            if (relation.readSs.CanRead)
            {
                int length = 0;
                try
                {
                    length = relation.readSs.EndRead(ar);
                }
                catch { }
                if (length > 0)
                {
                    if (relation.writeSs.CanWrite)
                    {
                        try
                        {
                            relation.writeSs.Write(relation.buffer.Take(length).ToArray(), 0, length);
                            StartTransferTcp_Ip(relation);
                            return;
                        }
                        catch { }
                    }
                }
            }
            relation.readSs.Close(3000);
            relation.writeSs.Close(3000);
            relation.readTcp.Close();
            relation.writeTcp.Close();
            TcpCenter.Instance.ConnectedTcpList.Remove(relation.readTcp);
            TcpCenter.Instance.ConnectedTcpList.Remove(relation.writeTcp);
        }
        public struct RelationTcp_Ip
        {
            public P2PTcpClient readTcp;
            public P2PTcpClient writeTcp;
            public NetworkStream readSs;
            public NetworkStream writeSs;
            public byte[] buffer;
        }

        /// <summary>
        ///     匹配对应的Command命令
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static P2PCommand FindCommand(P2PTcpClient tcpClient, ReceivePacket packet)
        {
            P2PCommand command = null;
            if (AppCenter.Instance.AllowAnonymous.Contains(packet.CommandType) || tcpClient.IsAuth)
            {
                if (AppCenter.Instance.CommandDict.ContainsKey(packet.CommandType))
                {
                    Type type = AppCenter.Instance.CommandDict[packet.CommandType];
                    command = Activator.CreateInstance(type, tcpClient, packet.Data.Select(t => t).ToArray()) as P2PCommand;
                }
                else
                {
                    LogUtils.Warning($"{tcpClient.RemoteEndPoint}请求了未知命令{packet.CommandType}");
                }
            }
            else
            {
                tcpClient.SafeClose();
                if (tcpClient.ToClient != null && tcpClient.ToClient.Connected)
                {
                    tcpClient.ToClient.SafeClose();
                }
                LogUtils.Warning($"拦截{tcpClient.RemoteEndPoint}未授权命令");
            }
            return command;
        }
    }
}
