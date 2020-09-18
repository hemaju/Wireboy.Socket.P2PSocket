using P2PSocket.Client.Utils;
using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
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


        public static bool BindTcp(P2PTcpClient readTcp, P2PTcpClient toTcp)
        {
            //TcpCenter.Instance.ConnectedTcpList.Add(readTcp);
            //TcpCenter.Instance.ConnectedTcpList.Add(toTcp);
            bool ret = true;
            RelationTcp_Ip toRelation = new RelationTcp_Ip();
            EasyOp.Do(() =>
            {
                toRelation.readTcp = readTcp;
                toRelation.readSs = readTcp.GetStream();
                toRelation.writeTcp = toTcp;
                toRelation.writeSs = toTcp.GetStream();
                toRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                StartTransferTcp_Ip(toRelation);
            },
            () =>
            {
                EasyOp.Do(() =>
                {
                    RelationTcp_Ip fromRelation = new RelationTcp_Ip();
                    fromRelation.readTcp = toRelation.writeTcp;
                    fromRelation.readSs = toRelation.writeSs;
                    fromRelation.writeTcp = toRelation.readTcp;
                    fromRelation.writeSs = toRelation.readSs;
                    fromRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                    StartTransferTcp_Ip(fromRelation);
                },
                ex =>
                {
                    LogUtils.Debug($"绑定Tcp失败:{Environment.NewLine}{ex}");
                    EasyOp.Do(readTcp.SafeClose);
                    ret = false;
                });
            },
            ex =>
            {
                LogUtils.Debug($"绑定Tcp失败:{Environment.NewLine}{ex}");
                EasyOp.Do(readTcp.SafeClose);
                EasyOp.Do(toTcp.SafeClose);
                ret = false;
            });
            return ret;
        }

        private static void StartTransferTcp_Ip(RelationTcp_Ip tcp)
        {
            tcp.readTcp.GetStream().BeginRead(tcp.buffer, 0, tcp.buffer.Length, TransferTcp_Ip, tcp);
        }
        private static void TransferTcp_Ip(IAsyncResult ar)
        {
            RelationTcp_Ip relation = (RelationTcp_Ip)ar.AsyncState;

            if (relation.readTcp.Connected)
            {
                int length = 0;
                EasyOp.Do(() =>
                {
                    length = relation.readSs.EndRead(ar);
                }, () =>
                {
                    EasyOp.Do(() =>
                    {
                        LogUtils.Trace($"接收到数据 From:{relation.readTcp.RemoteEndPoint} length:{length}");
                    });
                    if (length > 0)
                    {
                        if (relation.writeTcp.Connected)
                        {
                            EasyOp.Do(() =>
                            {
                                relation.writeSs.Write(relation.buffer.Take(length).ToArray(), 0, length);
                            }, () =>
                            {
                                EasyOp.Do(() =>
                                {
                                    StartTransferTcp_Ip(relation);
                                }, ex =>
                                {
                                    LogUtils.Debug($"Tcp连接已被断开 {relation.readTcp.RemoteEndPoint}");
                                    relation.writeTcp?.SafeClose();
                                });
                            }, ex =>
                            {
                                relation.readTcp?.SafeClose();
                            });

                        }
                    }
                    else
                    {
                        LogUtils.Debug($"Tcp连接已被断开 {relation.readTcp.RemoteEndPoint}");
                        relation.writeTcp?.SafeClose();
                    }
                }, ex =>
                {
                    LogUtils.Debug($"Tcp连接已被断开 {relation.readTcp.RemoteEndPoint}");
                    relation.writeTcp?.SafeClose();
                });
            }
            else
            {
                LogUtils.Debug($"Tcp连接已被断开 {relation.readTcp.RemoteEndPoint}");
                relation.writeTcp?.SafeClose();
            }
            //TcpCenter.Instance.ConnectedTcpList.Remove(relation.readTcp);
            //TcpCenter.Instance.ConnectedTcpList.Remove(relation.writeTcp);
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
            if (EasyInject.Get<AppCenter>().AllowAnonymous.Contains(packet.CommandType) || tcpClient.IsAuth)
            {
                if (EasyInject.Get<AppCenter>().CommandDict.ContainsKey(packet.CommandType))
                {
                    Type type = EasyInject.Get<AppCenter>().CommandDict[packet.CommandType];
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
