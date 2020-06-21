using P2PSocket.Core;
using P2PSocket.Core.Models;
using P2PSocket.Core.Extends;
using System;
using System.Collections.Generic;
using System.Text;
using P2PSocket.Core.Utils;
using System.Net.NetworkInformation;
using System.Linq;
using System.Net.Sockets;

namespace P2PSocket.Client.Models.Send
{
    public class Send_0x0104 : SendPacket
    {
        public Send_0x0104() : base(P2PCommandType.Login0x0104)
        {
            string mac = GetActiveMacAddress();
            //  客户端名称
            BinaryUtils.Write(Data, mac);
        }

        public string GetActiveMacAddress(string separator = "-")
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            //Debug.WriteLine("Interface information for {0}.{1}     ",
            //    computerProperties.HostName, computerProperties.DomainName);
            if (nics == null || nics.Length < 1)
            {
                throw new Exception("无法识别mac地址");
            }


            //Debug.WriteLine("  Number of interfaces .................... : {0}", nics.Length);
            foreach (NetworkInterface adapter in nics.Where(c =>
                c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up))
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                var unicastAddresses = properties.UnicastAddresses;
                if (unicastAddresses.Any(temp => temp.Address.AddressFamily == AddressFamily.InterNetwork))
                {
                    var address = adapter.GetPhysicalAddress();
                    if (string.IsNullOrEmpty(separator))
                    {
                        return address.ToString();
                    }
                    else
                    {
                        return string.Join(separator, address.GetAddressBytes().Select(t=>t.ToString("X")));
                    }
                }
            }
            throw new Exception("无法识别mac地址");
        }
    }
}
