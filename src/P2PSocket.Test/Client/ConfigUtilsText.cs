using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Client.Utils;
using P2PSocket.Client;
using P2PSocket.Core.Models;
using P2PSocket.Client.Models.Send;

namespace P2PSocket.Test.Client
{
    [TestClass]
    public class ConfigUtilsText
    {
        [TestMethod]
        public void LoadFromFile_Test()
        {
            ConfigCenter config = ConfigUtils.LoadFromFile();
            Assert.AreNotEqual(config.AllowPortList.Count, 0);
        }

        [TestMethod]
        public void TestParsePort()
        {
            AllowPortItem t = new AllowPortItem("0-900");
        }

        [TestMethod]
        public void TestMacAddress()
        {
            Send_0x0104 send_0X0104 = new Send_0x0104();
            send_0X0104.GetActiveMacAddress();
        }
    }
}
