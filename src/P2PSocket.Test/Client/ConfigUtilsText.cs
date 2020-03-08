using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Client.Utils;
using P2PSocket.Client;
using P2PSocket.Core.Models;

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
    }
}
