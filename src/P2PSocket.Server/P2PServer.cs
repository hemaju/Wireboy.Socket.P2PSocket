﻿using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Commands;
using P2PSocket.Server.Models;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace P2PSocket.Server
{
    public class P2PServer
    {
        public List<TcpListener> ListenerList { set; get; } = new List<TcpListener>();
        public List<TcpListener> HoneyListenList { set; get; } = new List<TcpListener>();
        public List<string> BlackIpList = new List<string>();
        AppCenter appCenter = EasyInject.Get<AppCenter>();
        ClientCenter clientCenter = EasyInject.Get<ClientCenter>();
        public P2PServer()
        {

        }

        /// <summary>
        ///     启动服务
        /// </summary>
        public void StartServer()
        {
            ListenHoneyPort();
            ListenMessagePort();
            StartPortMap();
        }

        /// <summary>
        ///     启动蜜罐端口
        /// </summary>
        private void ListenHoneyPort()
        {
            foreach (int port in appCenter.Config.HoneyPort)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    HoneyListenList.Add(listener);
                    listener.BeginAcceptSocket(ListenHoneyPortCallBack, listener);
                }
                catch (Exception ex)
                {
                    LogUtils.Error($"蜜罐端口监听失败：{ex}");
                }
            }
        }

        private void ListenHoneyPortCallBack(IAsyncResult ar)
        {
            TcpListener listener = ar.AsyncState as TcpListener;
            try
            {
                Socket socket = listener.EndAcceptSocket(ar);
                string ip = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();
                if (!BlackIpList.Contains(ip))
                    BlackIpList.Add(ip);
                socket.Close();
            }
            catch (Exception ex)
            {
                LogUtils.Error($"处理蜜罐请求出错：{ex}");
            }
            listener.BeginAcceptSocket(ListenHoneyPortCallBack, listener);
        }

        private bool IsBlackIp(IPEndPoint ip)
        {
            try
            {
                return BlackIpList.Contains((ip as IPEndPoint).Address.ToString());
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        ///     监听映射端口
        /// </summary>
        private void StartPortMap()
        {
            foreach (PortMapItem item in appCenter.Config.PortMapList)
            {
                if (item.MapType == PortMapType.ip)
                {
                    ListenPortMapPortWithIp(item);
                }
                else
                {
                    ListenPortMapPortWithServerName(item);
                }
            }
        }

        /// <summary>
        ///     监听消息端口
        /// </summary>
        private void ListenMessagePort()
        {
            TcpListener listener = null;
            EasyOp.Do(() =>
            {
                listener = new TcpListener(IPAddress.Any, appCenter.Config.LocalPort);
                listener.Start();
                LogUtils.Info($"监听服务端口：{appCenter.Config.LocalPort}");
            }, () =>
            {
                ListenerList.Add(listener);
                ListenSt listenSt = new ListenSt();
                listenSt.listener = listener;
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_Client, listenSt);
                }, ex =>
                {
                    LogUtils.Error($"监听服务端口发生错误:{Environment.NewLine}{ex}");
                    EasyOp.Do(() => listener.Stop());
                    ListenerList.Remove(listener);
                });
            }, ex =>
            {
                LogUtils.Error($"服务端口监听失败[{appCenter.Config.LocalPort}]:{Environment.NewLine}{ex}");
            });
        }
        struct ListenSt
        {
            public TcpListener listener;
            public PortMapItem item;
        }

        public void AcceptSocket_Client(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            Socket socket = null;

            EasyOp.Do(() =>
            {
                socket = listener.EndAcceptSocket(ar);
            }, () =>
            {
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_Client, st);
                }, exx =>
                {
                    LogUtils.Error($"端口监听失败:{Environment.NewLine}{exx}");
                    EasyOp.Do(() => listener.Stop());
                    ListenerList.Remove(listener);
                });

                try
                {
                    if (IsBlackIp(socket.RemoteEndPoint as IPEndPoint))
                    {
                        socket.SafeClose();
                        return;
                    }
                }
                catch
                {
                    socket.SafeClose();
                    return;
                }


                P2PTcpClient tcpClient = null;
                EasyOp.Do(() =>
                {
                    tcpClient = new P2PTcpClient(socket);
                }, () =>
                {
                    LogUtils.Trace($"端口{ appCenter.Config.LocalPort}新连入Tcp：{tcpClient.Client.RemoteEndPoint}");
                    //接收数据
                    EasyOp.Do(() =>
                    {
                        Global_Func.ListenTcp<ReceivePacket>(tcpClient);
                    }, ex =>
                    {
                        LogUtils.Debug($"准备接收Tcp数据出错：{Environment.NewLine}{ex}");
                        EasyOp.Do(() => tcpClient?.SafeClose());
                    });
                }, ex =>
                {
                    LogUtils.Debug($"处理新接入Tcp时出错：{Environment.NewLine}{ex}");
                    EasyOp.Do(() => tcpClient?.SafeClose());
                });
            }, ex =>
            {
                LogUtils.Debug($"获取新接入的Tcp连接失败");
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_Client, st);
                }, exx =>
                {
                    LogUtils.Error($"端口监听失败:{Environment.NewLine}{exx}");
                    EasyOp.Do(() => listener.Stop());
                    ListenerList.Remove(listener);
                });
            });

        }

        private void ListenPortMapPortWithServerName(PortMapItem item)
        {
            TcpListener listener = null;

            EasyOp.Do(() =>
            {
                listener = new TcpListener(IPAddress.Any, item.LocalPort);
                listener.Start();
            }, () =>
            {
                ListenerList.Add(listener);
                ListenSt listenSt = new ListenSt();
                listenSt.listener = listener;
                listenSt.item = item;
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_ClientName, listenSt);
                    LogUtils.Info($"端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
                }, ex =>
                {
                    LogUtils.Error($"建立端口映射失败 {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
                    EasyOp.Do(() => listener.Stop());
                    ListenerList.Remove(listener);
                });
            }, ex =>
            {
                LogUtils.Error($"建立端口映射失败 {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
                EasyOp.Do(() => listener?.Stop());
            });
        }

        public void AcceptSocket_ClientName(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            PortMapItem item = st.item;
            Socket socket = null;
            EasyOp.Do(() =>
            {
                socket = listener.EndAcceptSocket(ar);
            }, () =>
            {
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_ClientName, st);
                }, exx =>
                {
                    LogUtils.Error($"端口监听失败:{Environment.NewLine}{exx}");
                });

                try
                {
                    if (IsBlackIp(socket.RemoteEndPoint as IPEndPoint))
                    {
                        socket.SafeClose();
                        return;
                    }
                }
                catch
                {
                    socket.SafeClose();
                    return;
                }
                LogUtils.Debug($"开始内网穿透：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
                P2PTcpClient tcpClient = null;
                EasyOp.Do(() =>
                {
                    tcpClient = new P2PTcpClient(socket);
                }, () =>
                {
                    string token = tcpClient.Token;
                    //获取目标tcp
                    if (clientCenter.TcpMap.ContainsKey(item.RemoteAddress) && clientCenter.TcpMap[item.RemoteAddress].TcpClient.Connected)
                    {
                        //加入待连接集合
                        clientCenter.WaiteConnetctTcp.Add(token, tcpClient);
                        //发送p2p申请
                        Models.Send.Send_0x0211 packet = new Models.Send.Send_0x0211(token, item.RemotePort, tcpClient.RemoteEndPoint);
                        EasyOp.Do(() =>
                        {
                            clientCenter.TcpMap[item.RemoteAddress].TcpClient.BeginSend(packet.PackData());
                        }, () =>
                        {
                            Thread.Sleep(appCenter.Config.P2PTimeout);
                            //如果指定时间内没有匹配成功，则关闭连接
                            if (clientCenter.WaiteConnetctTcp.ContainsKey(token))
                            {
                                LogUtils.Debug($"建立隧道失败{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}，{appCenter.Config.P2PTimeout / 1000}秒无响应，已超时.");
                                EasyOp.Do(() => tcpClient?.SafeClose());
                                EasyOp.Do(() => clientCenter.WaiteConnetctTcp[token]?.SafeClose());
                                clientCenter.WaiteConnetctTcp.Remove(token);
                            }
                        }, ex =>
                        {
                            LogUtils.Debug($"建立隧道失败{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}，目标客户端已断开连接!");
                            EasyOp.Do(() => tcpClient?.SafeClose());
                            if (clientCenter.WaiteConnetctTcp.ContainsKey(token))
                            {
                                EasyOp.Do(() => clientCenter.WaiteConnetctTcp[token]?.SafeClose());
                                clientCenter.WaiteConnetctTcp.Remove(token);
                            }
                        });
                    }
                    else
                    {
                        LogUtils.Debug($"建立隧道失败{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}，客户端不在线!");
                        EasyOp.Do(() => tcpClient?.SafeClose());
                    }
                }, ex =>
                {
                    LogUtils.Debug($"处理新接入Tcp时发生错误：{Environment.NewLine}{ex}");
                    EasyOp.Do(() => socket?.SafeClose());
                });
            }, ex =>
            {
                LogUtils.Debug($"获取新接入的Tcp连接失败：{Environment.NewLine}{ex}");
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_ClientName, st);
                }, exx =>
                {
                    LogUtils.Error($"端口监听失败:{Environment.NewLine}{exx}");
                });
            });




        }
        /// <summary>
        ///     直接转发类型的端口监听
        /// </summary>
        /// <param name="item"></param>
        private void ListenPortMapPortWithIp(PortMapItem item)
        {
            TcpListener listener = null;
            EasyOp.Do(() =>
            {
                listener = new TcpListener(IPAddress.Any, item.LocalPort);
                listener.Start();
            }, () =>
            {
                ListenerList.Add(listener);
                ListenSt listenSt = new ListenSt();
                listenSt.listener = listener;
                listenSt.item = item;
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_Ip, listenSt);
                    LogUtils.Info($"端口映射：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}", false);
                }, ex =>
                {
                    LogUtils.Error($"建立端口映射失败 {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
                    EasyOp.Do(() => listener.Stop());
                    ListenerList.Remove(listener);
                });
            }, ex =>
            {
                LogUtils.Error($"建立端口映射失败 {item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
            });




        }

        public void AcceptSocket_Ip(IAsyncResult ar)
        {
            ListenSt st = (ListenSt)ar.AsyncState;
            TcpListener listener = st.listener;
            PortMapItem item = st.item;
            Socket socket = null;

            EasyOp.Do(() =>
            {
                socket = listener.EndAcceptSocket(ar);
            }, () =>
            {
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_Ip, st);
                }, ex =>
                {
                    LogUtils.Error($"端口监听失败:{Environment.NewLine}{ex}");
                });
                try
                {
                    if (IsBlackIp(socket.RemoteEndPoint as IPEndPoint))
                    {
                        socket.SafeClose();
                        return;
                    }
                }
                catch
                {
                    socket.SafeClose();
                    return;
                }


                P2PTcpClient tcpClient = null;
                EasyOp.Do(() =>
                {
                    tcpClient = new P2PTcpClient(socket);
                }, () =>
                {
                    P2PTcpClient ipClient = null;
                    EasyOp.Do(() =>
                    {
                        ipClient = new P2PTcpClient(item.RemoteAddress, item.RemotePort);
                    }, () =>
                    {
                        tcpClient.ToClient = ipClient;
                        ipClient.ToClient = tcpClient;
                        RelationTcp toRelation = new RelationTcp();
                        toRelation.readTcp = tcpClient;
                        toRelation.writeTcp = tcpClient.ToClient;
                        toRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                        RelationTcp fromRelation = new RelationTcp();
                        fromRelation.readTcp = toRelation.writeTcp;
                        fromRelation.writeTcp = toRelation.readTcp;
                        fromRelation.buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                        EasyOp.Do(() =>
                        {
                            StartTransferTcp_Ip(toRelation);
                            StartTransferTcp_Ip(fromRelation);
                        }, ex =>
                        {
                            LogUtils.Debug($"建立隧道失败：{Environment.NewLine}{ex}");
                            EasyOp.Do(() => ipClient.SafeClose());
                            EasyOp.Do(() => tcpClient.SafeClose());
                        });
                    }, ex =>
                    {
                        LogUtils.Debug($"建立隧道失败：{item.LocalPort}->{item.RemoteAddress}:{item.RemotePort}{Environment.NewLine}{ex}");
                        EasyOp.Do(() => tcpClient.SafeClose());
                    });

                }, ex =>
                {
                    LogUtils.Debug($"处理新接入Tcp时发生错误：{Environment.NewLine}{ex}");
                    EasyOp.Do(() => socket?.SafeClose());
                });

            }, ex =>
            {
                LogUtils.Debug($"获取新接入的Tcp连接失败：{Environment.NewLine}{ex}");
                EasyOp.Do(() =>
                {
                    listener.BeginAcceptSocket(AcceptSocket_Ip, st);
                }, exx =>
                {
                    LogUtils.Error($"端口监听失败:{Environment.NewLine}{exx}");
                });
            });


        }

        private void StartTransferTcp_Ip(RelationTcp tcp)
        {
            tcp.readTcp.GetStream().BeginRead(tcp.buffer, 0, tcp.buffer.Length, TransferTcp_Ip, tcp);
        }
        private void TransferTcp_Ip(IAsyncResult ar)
        {
            RelationTcp relation = (RelationTcp)ar.AsyncState;
            int length = 0;
            EasyOp.Do(() =>
            {
                length = relation.readTcp.GetStream().EndRead(ar);
            }, () =>
            {
                if (length <= 0 || !EasyOp.Do(() =>
                {
                    relation.writeTcp.BeginSend(relation.buffer.Take(length).ToArray());
                    StartTransferTcp_Ip(relation);
                }))
                {
                    SafeClose(relation.readTcp);
                    SafeClose(relation.writeTcp);
                }
            }, ex =>
            {
                SafeClose(relation.readTcp);
                SafeClose(relation.writeTcp);
            });
        }

        private void SafeClose(TcpClient tcp)
        {
            EasyOp.Do(() => tcp.GetStream().Close(3000));
            EasyOp.Do(() => tcp.Close());
        }

        public struct RelationTcp
        {
            public TcpClient readTcp;
            public TcpClient writeTcp;
            public byte[] buffer;
        }
    }
}
