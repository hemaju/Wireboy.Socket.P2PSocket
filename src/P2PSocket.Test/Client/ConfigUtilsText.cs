using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Client.Utils;
using P2PSocket.Client;

namespace P2PSocket.Test.Client
{
    [TestClass]
    public class ConfigUtilsText
    {
        [TestMethod]
        public void LoadFromFile_Test()
        {
            ConfigUtils.LoadFromFile();
            Assert.AreNotEqual(Global.AllowPort.Count, 0);
        }
    }
}
