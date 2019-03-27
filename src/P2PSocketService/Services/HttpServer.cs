using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Wireboy.Socket.P2PService.Models;
using System.Linq;
using Wireboy.Socket.P2PService.Services;
using System.Threading;

namespace Wireboy.Socket.P2PService
{
    public class HttpServer
    {
        Dictionary<string, TcpClient> _transferClient = new Dictionary<string, TcpClient>();
        P2PService _p2PService = null;
        Dictionary<string, TcpClient> _httpClientMap = new Dictionary<string, TcpClient>();
        public HttpServer(P2PService p2PService)
        {
            this._p2PService = p2PService;
            //List<HttpModel> httpModels = new List<HttpModel>();
            //HttpModel httpModel = new HttpModel();
            //httpModel.Domain = "www.test.com";
            //httpModel.ServerName = "webgroup";
            //httpModel.Port = 1705;
            //httpModels.Add(httpModel);
            //httpModel = new HttpModel();
            //httpModel.Domain = "blog.test.com";
            //httpModel.ServerName = "pgroup";
            //httpModel.Port = 1705;
            //httpModels.Add(httpModel);
            //ConfigServer.HttpSettings.Add(1705, httpModels);
        }
        TaskFactory _taskFactory = new TaskFactory();
        public void Start()
        {
            foreach (int port in ConfigServer.HttpSettings.Keys)
            {
                _taskFactory.StartNew(() =>
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    while (true)
                    {
                        TcpClient tcpClient = listener.AcceptTcpClient();
                        //接收tcp数据
                        _taskFactory.StartNew(() =>
                        {
                            try
                            {
                                RecieveClientTcp(tcpClient, port);
                            }
                            catch (Exception ex)
                            {
                                Logger.Write("{0}", ex);
                            }
                        });
                    }
                });
            }
        }
        /// <summary>
        /// 接收浏览器请求
        /// </summary>
        /// <param name="readTcp"></param>
        /// <param name="port"></param>
        public void RecieveClientTcp(TcpClient readTcp, int port)
        {
            EndPoint endPoint = readTcp.Client.RemoteEndPoint;
            //当前tcp的guid
            byte[] guid = Guid.NewGuid().ToByteArray();
            string guidKey = guid.ToStringUnicode();
            TcpClient transferClient = null;
            try
            {
                NetworkStream readStream = readTcp.GetStream();
                TcpHelper tcpHelper = new TcpHelper();
                //接收缓存
                byte[] buffer = new byte[1024];
                if (_httpClientMap.ContainsKey(guidKey))
                {
                    _httpClientMap.Remove(guidKey);
                }
                _httpClientMap.Add(guidKey, readTcp);
                //是否第一次
                bool isFirst = true;
                while (true)
                {
                    int length = readStream.Read(buffer, 0, buffer.Length);
                    if (length > 0)
                    {
                        Logger.Debug("浏览器->web：{0}", length);
                        if (isFirst)
                        {
                            //读取域名信息
                            string domain = GetHttpRequestHost(buffer, length);
                            HttpModel httpModel = MatchHttpModel(domain, ConfigServer.HttpSettings[port]);
                            //获取目的服务器
                            if (httpModel != null && _transferClient.ContainsKey(httpModel.ServerName))
                                transferClient = _transferClient[httpModel.ServerName];
                        }
                        if (transferClient == null)
                        {
                            readTcp.Close();
                            break;
                        }
                        byte[] sendBytes = GetHttpRequestBytes(HttpMsgType.Http数据, guid, buffer.Take(length).ToArray());
                        transferClient.WriteAsync(sendBytes, MsgType.Http服务);
                        isFirst = false;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                _httpClientMap.Remove(guidKey);
                BreakHttpRequest(guid, transferClient);
                Logger.Write("接收来自{0}的数据异常：\r\n{1} ", endPoint, ex);
            }
            _httpClientMap.Remove(guidKey);
        }
        /// <summary>
        /// 断开web服务器的连接
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="transferClient"></param>
        private void BreakHttpRequest(byte[] guid, TcpClient transferClient)
        {
            if (transferClient == null) return;
            try
            {
                byte[] sendBytes = GetHttpRequestBytes(HttpMsgType.断开连接, guid, new byte[1]);
                transferClient.WriteAsync(sendBytes, MsgType.Http服务);
            }
            catch { }
        }
        public byte[] GetHttpRequestBytes(HttpMsgType httpMsgType, byte[] guid, byte[] data)
        {
            List<byte> ret = new List<byte>();
            ret.Add((byte)httpMsgType);
            ret.AddRange(guid.TransferSendBytes());
            ret.AddRange(data.TransferSendBytes());
            return ret.ToArray();
        }

        /// <summary>
        /// 处理服务器response
        /// </summary>
        /// <param name="data"></param>
        public void RecieveHttpServerTcp(byte[] data, TcpClient tcpClient)
        {
            int index = 0;
            byte type = data.ReadByte(ref index);
            short length = data.ReadShort(ref index);
            byte[] curGuid = data.ReadBytes(ref index, length);
            //Logger.Write("收到web服务数据：{0}", data.Length - index - 2);
            switch (type)
            {
                case (byte)HttpMsgType.Http数据:
                    {
                        string key = curGuid.ToStringUnicode();
                        try
                        {
                            if (_httpClientMap.ContainsKey(key))
                            {
                                length = data.ReadShort(ref index);
                                byte[] bytes = data.ReadBytes(ref index, length);
                                Logger.Debug("web->浏览器：{0}", bytes.Length);
                                _httpClientMap[key].WriteAsync(bytes, MsgType.不封包);
                            }
                        }
                        catch (Exception ex)
                        {
                            _httpClientMap.Remove(key);
                            Console.WriteLine("{0}", ex);
                        }
                    }
                    break;
                case (byte)HttpMsgType.Http服务名:
                    {
                        short strLength = data.ReadShort(ref index);
                        String httpServerName = data.ReadString(ref index, strLength);
                        if (_transferClient.ContainsKey(httpServerName))
                        {
                            _transferClient.Remove(httpServerName);
                        }
                        _transferClient.Add(httpServerName, tcpClient);
                    }
                    break;
                case (byte)HttpMsgType.None:
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// 获取请求的域名（如果返回空，则说明不是http请求，可能是https）
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public string GetHttpRequestHost(byte[] bytes, int length)
        {
            bool read = false;
            List<byte> byteList = new List<byte>();
            for (int i = 0; i < length; i++)
            {
                if (bytes[i] == 13 && (i + 1) < length && bytes[i + 1] == 10)
                {
                    if (read) break;
                    i++;
                    read = true;
                }
                else if (read)
                {
                    byteList.Add(bytes[i]);
                }
            }
            String str = Encoding.ASCII.GetString(byteList.ToArray());
            int indexOf = str.IndexOf(':');
            if (indexOf > -1) str = str.Substring(indexOf + 1).Trim();
            return str;
        }

        public HttpModel MatchHttpModel(string domain, List<HttpModel> httpModels)
        {
            string webDomain = domain.Split(':').FirstOrDefault();
            HttpModel ret = httpModels.Where(t =>
            {
                if (webDomain.StartsWith(t.Domain))
                    return true;
                return false;
            }).FirstOrDefault();
            if (ret == null)
            {
                ret = httpModels.Where(t => t.Type.ToLower() != "http").FirstOrDefault();
            }
            return ret;
        }
    }
}
