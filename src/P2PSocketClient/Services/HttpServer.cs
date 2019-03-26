using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Wireboy.Socket.P2PClient.Models;

namespace Wireboy.Socket.P2PClient.Services
{
    public class HttpServer
    {
        Dictionary<string, TcpClient> _httpClientMap = new Dictionary<string, TcpClient>();
        P2PService _p2PService;
        int _port = 1700;
        TaskFactory _taskFactory = new TaskFactory();
        public HttpServer(P2PService p2PService)
        {
            _p2PService = p2PService;
        }
        public void Start()
        {
            byte[] bytes = GetHttpSendBytes(HttpMsgType.Http服务名, new byte[1], ("webgroup").ToBytes());
            _p2PService.ServerTcp.WriteAsync(bytes, MsgType.Http服务);
        }
        public void RecieveServerTcp(byte[] data)
        {
            int index = 0;
            byte type = data.ReadByte(ref index);
            short length = data.ReadShort(ref index);
            byte[] curGuid = data.ReadBytes(ref index, length);

            if (!_httpClientMap.ContainsKey(curGuid.ToStringUnicode()))
            {
                //连接网站
                TcpClient tcpClient = new TcpClient("127.0.0.1", _port);
                Logger.Write("新的请求");
                _httpClientMap.Add(curGuid.ToStringUnicode(), tcpClient);
                _taskFactory.StartNew(() =>
                {
                    try
                    {
                        NetworkStream stream = tcpClient.GetStream();
                        byte[] webBytes = new byte[1024];
                        while (true)
                        {
                            int webLength = stream.Read(webBytes, 0, webBytes.Length);
                            if (webLength > 0)
                            {
                                Logger.Write("web->浏览器：{0}",webLength);
                                byte[] webRet = GetHttpSendBytes(HttpMsgType.Http数据, curGuid,webBytes.Take(webLength).ToArray());
                                //发送数据
                                _p2PService.ServerTcp.WriteAsync(webRet,MsgType.Http服务);
                            }
                        }
                    }catch(Exception ex)
                    {
                        _httpClientMap.Remove(curGuid.ToStringUnicode());
                    }
                });
            }
            switch (type)
            {
                case (byte)HttpMsgType.Http数据:
                    {
                        if (_httpClientMap.ContainsKey(curGuid.ToStringUnicode()))
                        {
                            length = data.ReadShort(ref index);
                            byte[] bytes = data.ReadBytes(ref index, length);
                            Logger.Write("浏览器->web：{0}", bytes.Length);
                            _httpClientMap[curGuid.ToStringUnicode()].WriteAsync(bytes, MsgType.不封包);
                        }
                    }
                    break;
                case (byte)HttpMsgType.None:
                    {

                    }
                    break;
            }
        }
        public byte[] GetHttpSendBytes(HttpMsgType httpMsgType, byte[] guid, byte[] data)
        {
            List<byte> ret = new List<byte>();
            ret.Add((byte)httpMsgType);
            ret.AddRange(guid.TransferSendBytes());
            ret.AddRange(data.TransferSendBytes());
            return ret.ToArray();
        }
    }
}
