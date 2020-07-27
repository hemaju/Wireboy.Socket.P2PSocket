using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace P2PSocket.Server
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
