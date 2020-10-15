using P2PSocket.Client.Commands;
using P2PSocket.Client.Models.Send;
using P2PSocket.Client.Utils;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TransferEncryption_Plug
{
    public class Cmd_0x0202Ex : Cmd_0x0202
    {
        public Cmd_0x0202Ex(P2PTcpClient tcpClient, byte[] data) : base(tcpClient, data)
        {
        }

        public override bool Excute()
        {
            LogUtils.Trace($"开始处理消息：0x0201 From:{m_tcpClient.ToClient.RemoteEndPoint} Length:{((MemoryStream)m_data.BaseStream).Length}");
            bool ret = true;
            //是否来自端口
            if (BinaryUtils.ReadBool(m_data))
            {
                //需要数据加密
                //Port->Client
                byte[] sendBytes = BinaryUtils.ReadBytes(m_data);
                Send_0x0202 sendPacket = new Send_0x0202(Encryption(sendBytes), false);
                EasyOp.Do(() =>
                {
                    m_tcpClient.ToClient.BeginSend(sendPacket.PackData());
                }, ex =>
                {
                    LogUtils.Debug($"命令：0x0202 转发来自端口的数据失败：{Environment.NewLine}{ex}");
                    ret = false;
                });
            }
            else
            {
                //需要数据解密
                //Server->Client
                byte[] sendBytes = BinaryUtils.ReadBytes(m_data);
                EasyOp.Do(() =>
                {
                    m_tcpClient.ToClient.BeginSend(Decryption(sendBytes));
                }, ex =>
                {
                    LogUtils.Debug($"命令：0x0202 转发来自服务器的数据失败：{Environment.NewLine}{ex}");
                    ret = false;
                });
            }
            return ret;
        }

        protected byte[] Encryption(byte[] data)
        {
            for (int i = 0; i < data.Length - 1; i += 2)
            {
                byte temp = data[i];
                data[i] = data[i + 1];
                data[i + 1] = temp;
            }
            return data;
        }

        protected byte[] Decryption(byte[] data)
        {
            for (int i = 0; i < data.Length - 1; i += 2)
            {
                byte temp = data[i];
                data[i] = data[i + 1];
                data[i + 1] = temp;
            }
            return data;
        }
    }
}
