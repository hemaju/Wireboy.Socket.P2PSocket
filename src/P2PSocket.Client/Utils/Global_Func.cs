using P2PSocket.Client.Utils;
using P2PSocket.Core;
using P2PSocket.Core.Commands;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using System;
using System.Collections.Generic;
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
            try
            {
                Guid curGuid = Global.CurrentGuid;
                byte[] buffer = new byte[P2PGlobal.P2PSocketBufferSize];
                NetworkStream tcpStream = tcpClient.GetStream();
                ReceivePacket msgReceive = Activator.CreateInstance(typeof(T)) as ReceivePacket;
                while (tcpClient.Connected && curGuid == Global.CurrentGuid)
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
                        //如果tcp已关闭，需要关闭相关tcp
                        try
                        {
                            tcpClient.ToClient?.Close();
                        }
                        catch { }
                        LogUtils.Debug($"tcp连接{tcpClient.RemoteEndPoint}已断开");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtils.Error($"【错误】Global_Func.ListenTcp：{Environment.NewLine}{ex}");
            }
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
            if (Global.AllowAnonymous.Contains(packet.CommandType) || tcpClient.IsAuth)
            {
                if (Global.CommandDict.ContainsKey(packet.CommandType))
                {
                    Type type = Global.CommandDict[packet.CommandType];
                    command = Activator.CreateInstance(type, tcpClient, packet.GetBytes()) as P2PCommand;
                }
                else
                {
                    LogUtils.Warning($"{tcpClient.RemoteEndPoint}请求了未知命令{packet.CommandType}");
                }
            }
            else
            {
                tcpClient.Close();
                if (tcpClient.ToClient != null && tcpClient.ToClient.Connected)
                {
                    tcpClient.ToClient.Close();
                }
                LogUtils.Warning($"拦截{tcpClient.RemoteEndPoint}未授权命令");
            }
            return command;
        }
    }
}
