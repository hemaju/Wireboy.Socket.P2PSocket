using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Client.Utils;
using P2PSocket.Client;
using P2PSocket.Core.Models;
using P2PSocket.Client.Models.Send;
using P2PSocket.Core.Utils;
using P2PSocket.Client.Models.ConfigIO;

namespace P2PSocket.Test.Client
{
    [TestClass]
    public class ConfigUtilsText
    {
        [TestMethod]
        public void LoadFromFile_Test()
        {
            //AppConfig config = EasyInject.Get<ConfigManager>().LoadFromFile() as AppConfig;
            //Assert.AreNotEqual(config.AllowPortList.Count, 0);
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


        [TestMethod]
        public void TestGetItemString()
        {
            Common cm = new Common(null);
            (string, string) item = ("LocalPort", "80");
            string itemStr = cm.GetItemString<(string, string)>(item);
            Assert.AreEqual<string>(itemStr, "LocalPort=80");
        }

    }
}
