using Microsoft.VisualStudio.TestTools.UnitTesting;
using P2PSocket.Core.Enums;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Test.Core
{
    [TestClass]
    public class BinaryUtilsTest
    {
        [TestMethod]
        public void ReadString()
        {
            var data = "这是一段测试文字";
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadString(binaryReader);
            Assert.AreEqual(result, data);
        }
        [TestMethod]
        public void ReadInt()
        {
            var data = 12345;
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadInt(binaryReader);
            Assert.AreEqual(result, data);
        }
        [TestMethod]
        public void ReadBool()
        {
            var data = false;
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadBool(binaryReader);
            Assert.AreEqual(result, data);
        }
        [TestMethod]
        public void ReadBytes()
        {

            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadBytes(binaryReader);
            Assert.AreEqual(result[0], data[0]);
            Assert.AreEqual(result[1], data[1]);
            Assert.AreEqual(result[2], data[2]);
            Assert.AreEqual(result[3], data[3]);
            Assert.AreEqual(result[4], data[4]);
            Assert.AreEqual(result[5], data[5]);
        }
        [TestMethod]
        public void ReadUshort()
        {
            var data = (ushort)100;
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadUshort(binaryReader);
            Assert.AreEqual(result, data);

        }
        [TestMethod]
        public void ReadIntList()
        {
            var data = new List<int>() { 2, 3, 4, 5, 5, 6, 7 };
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadIntList(binaryReader);
            Assert.AreEqual(result[0], data[0]);
            Assert.AreEqual(result[1], data[1]);
            Assert.AreEqual(result[2], data[2]);
            Assert.AreEqual(result[3], data[3]);
            Assert.AreEqual(result[4], data[4]);
            Assert.AreEqual(result[5], data[5]);

        }
        [TestMethod]
        public void ReadStringList()
        {
            var data = new List<string>() { "11111111", "2222222", "3333333" };
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadStringList(binaryReader);
            Assert.AreEqual(result[0], data[0]);
            Assert.AreEqual(result[1], data[1]);
            Assert.AreEqual(result[2], data[2]);

        }
        [TestMethod]
        public void ReadLogLevel()
        {
            var data = LogLevel.Error;
            BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
            BinaryUtils.Write(binaryWriter, data);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(((MemoryStream)binaryWriter.BaseStream).ToArray()));
            var result = BinaryUtils.ReadLogLevel(binaryReader);
            Assert.AreEqual(result, data);

        }
    }
}
