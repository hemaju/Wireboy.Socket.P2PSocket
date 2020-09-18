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
            public byte[] buffer;
            public ReceivePacket msgReceive;
            public Guid guid;
        }
        public static void ListenTcp<T>(P2PTcpClient tcpClient) where T : ReceivePacket
        {
            RelationTcp_Server relationSt = new RelationTcp_Server();
            relationSt.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
            relationSt.readTcp = tcpClient;
            relationSt.msgReceive = Activator.CreateInstance(typeof(T)) as ReceivePacket;
            relationSt.guid = EasyInject.Get<AppCenter>().CurrentGuid;
            relationSt.readTcp.GetStream().BeginRead(relationSt.buffer, 0, relationSt.buffer.Length, ReadTcp_Server, relationSt);
        }

        private static void ReadTcp_Server(IAsyncResult ar)
        {
            RelationTcp_Server relation = (RelationTcp_Server)ar.AsyncState;
            if (relation.guid == EasyInject.Get<AppCenter>().CurrentGuid)
            {
                if (relation.readTcp.Connected && relation.readTcp.GetStream().CanRead)
                {
                    int length = 0;
                    EasyOp.Do(() =>
                    {
                        length = relation.readTcp.GetStream().EndRead(ar);
                    }, () =>
                    {
                        if (length > 0)
                        {
                            byte[] refData = relation.buffer.Take(length).ToArray();
                            while (relation.msgReceive.ParseData(ref refData))
                            {
                                // 执行command
                                using (P2PCommand command = FindCommand(relation.readTcp, relation.msgReceive))
                                {
                                    //LogUtils.Trace($"命令类型:{relation.msgReceive.CommandType}");
                                    if (command != null)
                                    {
                                        bool isSuccess = false;
                                        EasyOp.Do(() =>
                                        {
                                            isSuccess = command.Excute();
                                        },
                                        e =>
                                        {
                                            LogUtils.Error($"执行命令{relation.msgReceive.CommandType}时发生异常:{e}");
                                        });
                                        if (!isSuccess)
                                        {
                                            EasyOp.Do(() => { relation.readTcp?.SafeClose(); });
                                            EasyOp.Do(() => { relation.readTcp.ToClient?.SafeClose(); });
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        EasyOp.Do(() => { relation.readTcp?.SafeClose(); });
                                        EasyOp.Do(() => { relation.readTcp.ToClient?.SafeClose(); });
                                        return;
                                    }
                                }
                                //重置msgReceive
                                relation.msgReceive.Reset();
                                if (refData.Length <= 0) break;
                            }
                            if (relation.readTcp.Connected)
                            {
                                EasyOp.Do(() =>
                                {
                                    relation.readTcp.GetStream().BeginRead(relation.buffer, 0, relation.buffer.Length, ReadTcp_Server, relation);
                                }, ex =>
                                {
                                    LogUtils.Debug($"Tcp连接已被断开 {relation.readTcp.RemoteEndPoint}");
                                    EasyOp.Do(() => { relation.readTcp.ToClient?.SafeClose(); });
                                });
                            }
                        }
                        else
                        {
                            EasyOp.Do(() => { relation.readTcp?.SafeClose(); });
                            EasyOp.Do(() => { relation.readTcp.ToClient?.SafeClose(); });

                        }
                    }, ex =>
                    {
                        LogUtils.Debug($"Tcp连接已被断开 {relation.readTcp.RemoteEndPoint}");
                        EasyOp.Do(() => { relation.readTcp.ToClient?.SafeClose(); });
                    });

                }
            }
            else
            {
                LogUtils.Debug($"主动断开{relation.readTcp.RemoteEndPoint}连接");
                EasyOp.Do(() => { relation.readTcp?.SafeClose(); });
                EasyOp.Do(() => { relation.readTcp.ToClient?.SafeClose(); });
            }
            //if (TcpCenter.Instance.ConnectedTcpList.Contains(relation.readTcp))
            //    TcpCenter.Instance.ConnectedTcpList.Remove(relation.readTcp);
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
            AppCenter appCenter = EasyInject.Get<AppCenter>();
            if (appCenter.AllowAnonymous.Contains(packet.CommandType) || tcpClient.IsAuth)
            {
                if (appCenter.CommandDict.ContainsKey(packet.CommandType))
                {
                    Type type = appCenter.CommandDict[packet.CommandType];
                    command = Activator.CreateInstance(type, tcpClient, packet.Data.Select(t => t).ToArray()) as P2PCommand;
                }
                else
                {
                    LogUtils.Warning($"{tcpClient.RemoteEndPoint}请求了未知命令{packet.CommandType}");
                }
            }
            else
            {
                tcpClient?.SafeClose();
                tcpClient.ToClient?.SafeClose();
                LogUtils.Warning($"拦截{tcpClient.RemoteEndPoint}未授权命令");
            }
            return command;
        }
    }
}
