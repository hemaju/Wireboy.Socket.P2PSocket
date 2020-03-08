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
        public static void ListenTcp<T>(P2PTcpClient tcpClient) where T : ReceivePacket
        {
            TcpCenter.Instance.ConnectedTcpList.Add(tcpClient);
            try
            {
                Guid curGuid = AppCenter.Instance.CurrentGuid;
                byte[] buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                NetworkStream tcpStream = tcpClient.GetStream();
                ReceivePacket msgReceive = Activator.CreateInstance(typeof(T)) as ReceivePacket;
                while (tcpClient.Connected && curGuid == AppCenter.Instance.CurrentGuid)
                {
                    int curReadLength = tcpStream.ReadSafe(buffer, 0, buffer.Length);
                    if (curReadLength > 0)
                    {
                        byte[] refData = buffer.Take(curReadLength).ToArray();
                        while (msgReceive.ParseData(ref refData))
                        {
                            LogUtils.Debug($"命令类型:{msgReceive.CommandType}");
                            // 执行command
                            using (P2PCommand command = FindCommand(tcpClient, msgReceive))
                            {
                                command?.Excute();
                            }
                            //重置msgReceive
                            msgReceive.Reset();
                            if (refData.Length <= 0) break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【错误】Global_Func.ListenTcp：{Environment.NewLine}{ex}");
            }
            //如果tcp已关闭，需要关闭相关tcp
            try
            {
                tcpClient.ToClient?.SafeClose();
            }
            catch { }
            LogUtils.Debug($"tcp连接{tcpClient.RemoteEndPoint}已断开");
            TcpCenter.Instance.ConnectedTcpList.Remove(tcpClient);

        }

        public static void BindTcp(P2PTcpClient readTcp, P2PTcpClient toTcp)
        {
            TcpCenter.Instance.ConnectedTcpList.Add(readTcp);
            byte[] buffer = new byte[P2PGlobal.P2PSocketBufferSize];
            NetworkStream readStream = readTcp.GetStream();
            NetworkStream toStream = toTcp.GetStream();
            while (readTcp.Connected)
            {
                int curReadLength = readStream.ReadSafe(buffer, 0, buffer.Length);
                if (curReadLength > 0)
                {
                    bool isError = true;
                    if (toTcp != null && toTcp.Connected)
                    {
                        try
                        {
                            toStream.Write(buffer, 0, curReadLength);
                            isError = false;
                        }
                        catch { }
                    }
                    if(isError)
                    {
                        LogUtils.Warning($"Tcp连接{toTcp.RemoteEndPoint}已断开.");
                        readTcp.SafeClose();
                        break;
                    }
                }
                else
                {
                    LogUtils.Warning($"Tcp连接{readTcp.RemoteEndPoint}已断开.");
                    //如果tcp已关闭，需要关闭相关tcp
                    toTcp?.SafeClose();
                    break;
                }
            }
            TcpCenter.Instance.ConnectedTcpList.Remove(readTcp);

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
                    command = Activator.CreateInstance(type, tcpClient, packet.Data) as P2PCommand;
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
