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

namespace P2PSocket.Server
{

    public static class Global_Func
    {
        public static void ListenTcp<T>(P2PTcpClient tcpClient) where T : RecievePacket
        {
            byte[] buffer = new byte[P2PGlobal.P2PSocketBufferSize];
            NetworkStream tcpStream = tcpClient.GetStream();
            RecievePacket msgRecieve = Activator.CreateInstance(typeof(T)) as RecievePacket; 
            while (tcpClient.Connected)
            {
                int curReadLength = tcpStream.ReadSafe(buffer, 0, buffer.Length);
                if (curReadLength > 0)
                {
                    byte[] refData = buffer.Take(curReadLength).ToArray();
                    while (msgRecieve.ParseData(ref refData))
                    {
                        //todo：执行command
                        P2PCommand command = FindCommand(tcpClient, msgRecieve);
                        if (command != null) command.Excute();
                        //重置msgRecieve
                        msgRecieve = Activator.CreateInstance(typeof(T)) as RecievePacket;
                        if (refData.Length <= 0) break;
                    }
                }
                else
                {
                    //如果tcp已关闭，需要关闭相关tcp
                    if (tcpClient.ToClient != null && tcpClient.ToClient.Connected)
                    {
                        tcpClient.ToClient.Close();
                    }
                    break;
                }
            }
        }

        /// <summary>
        ///     匹配对应的Command命令
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static P2PCommand FindCommand(P2PTcpClient tcpClient, RecievePacket packet)
        {
            P2PCommand command = null;
            if (Global.AllowAnonymous.Contains(packet.CommandType) || tcpClient.IsAuth)
            {
                foreach (Type type in Global.CommandList)
                {
                    IEnumerable<Attribute> attributes = type.GetCustomAttributes();
                    if (!attributes.Any(t => t is CommandFlag))
                    {
                        continue;
                    }
                    CommandFlag flag = attributes.First(t => t is CommandFlag) as CommandFlag;
                    if (flag.CommandType == packet.CommandType)
                    {
                        command = Activator.CreateInstance(type, tcpClient, packet.GetBytes()) as P2PCommand;
                    }
                }
            }
            else
            {
                throw new Exception("没有权限");
            }
            return command;
        }
    }
}
