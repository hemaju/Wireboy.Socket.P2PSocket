using Microsoft.VisualStudio.TestTools.UnitTesting;
using P2PSocket.Core.Extends;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Test.Core
{
    [TestClass]
    public class PacketTest
    {
        [TestMethod]
        public void TestSocketParse()
        {
            byte[] data = ("这是一条测试数据").ToBytes();
            SendPacket sendPacket = new SendPacket(data);
            List<byte> dataList = new List<byte>();
            dataList.AddRange(sendPacket.PackData());
            dataList.AddRange(sendPacket.PackData());
            data = dataList.ToArray();
            ReceivePacket ReceivePacket = new ReceivePacket();
            while (data.Length > 0)
            {
                if (ReceivePacket.ParseData(ref data))
                {
                    string str = ReceivePacket.Data.ToStringUnicode();
                    Console.WriteLine(str);
                    Assert.AreEqual("这是一条测试数据", str);
                    ReceivePacket = new ReceivePacket();
                }
                else
                {
                    Assert.Fail();
                    break;
                }
            }
        }
        [TestMethod]
        public void TestGlobal_CommandList()
        {
            CoreModule coreModule = new CoreModule();
            coreModule.InitCommandList();
            Assert.AreNotEqual(EasyInject.Get<AppCenter>().CommandDict.Count, 0);
        }
        [TestMethod]
        public void TestConfig_LoadFile()
        {
            AppConfig config = EasyInject.Get<IConfig>().LoadFromFile() as AppConfig;
            Assert.AreNotEqual(config.PortMapList.Count, 0);
        }
        [TestMethod]
        public void TestServerStart()
        {
            CoreModule coreModule = new CoreModule();
            coreModule.Start();
        }
    }
}
