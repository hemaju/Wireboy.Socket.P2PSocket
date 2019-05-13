using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class SendPacket
    {
        public BinaryWriter Data { set; get; } = new BinaryWriter(new MemoryStream());
        public P2PCommandType CommandType { set; get; } = P2PCommandType.UnKnown;
        public SendPacket(byte[] data)
        {
            Data.Write(data);
        }
        public SendPacket()
        {
        }
        public SendPacket(P2PCommandType commandType)
        {
            CommandType = commandType;
        }

        /// <summary>
        ///     打包数据（header，type，length，data，footer）
        /// </summary>
        /// <returns></returns>
        public virtual byte[] PackData()
        {
            BinaryWriter writer  = new BinaryWriter(new MemoryStream());
            SetHeader(writer);
            SetCommandType(writer);
            SetPacketLength(writer);
            writer.Write(((MemoryStream)Data.BaseStream).ToArray());
            SetFooter(writer);
            return ((MemoryStream)writer.BaseStream).ToArray();
        }
        protected virtual void SetCommandType(BinaryWriter writer)
        {
            writer.Write((ushort)CommandType);
        }
        protected virtual void SetHeader(BinaryWriter writer)
        {
            writer.Write(P2PGlobal.PacketHeader);
        }

        protected virtual void SetPacketLength(BinaryWriter writer)
        {
            writer.Write((short)Data.BaseStream.Length);
        }

        protected virtual void SetFooter(BinaryWriter writer)
        {
            writer.Write(P2PGlobal.PacketFooter);
        }
    }
}
