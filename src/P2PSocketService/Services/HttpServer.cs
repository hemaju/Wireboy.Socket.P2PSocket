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
        Dictionary<string, TcpClient> m_transferClient = new Dictionary<string, TcpClient>();
        P2PService m_p2PService = null;
        Dictionary<string, TcpClient> m_httpClientMap = new Dictionary<string, TcpClient>();
        int m_guidLength = Guid.NewGuid().ToByteArray().Length;
        public HttpServer(P2PService p2PService)
        {
            this.m_p2PService = p2PService;
        }
        TaskFactory _taskFactory = new TaskFactory();
        public void Start()
        {
            foreach (int port in ConfigServer.HttpSettings.Keys)
            {
                _taskFactory.StartNew(() =>
                {
                    try
                    {
                        TcpListener listener = new TcpListener(IPAddress.Any, port);
                        listener.Start();
                        string serverName = "";
                        ConfigServer.HttpSettings[port].Select(t => t.ServerName).Distinct().ToList().ForEach(t => serverName += t + " ");
                        Logger.Info.WriteLine("[HttpServer] 启动Http服务，端口:{0} 服务名：{1}", port, serverName);
                        while (true)
                        {
                            TcpClient tcpClient = listener.AcceptTcpClient();
                            //接收tcp数据
                            _taskFactory.StartNew(() =>
                            {
                                RecieveWebTcp(tcpClient, port);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error.WriteLine("[HttpServer] http服务启动失败!\r\n{0}", ex);
                    }
                });
            }
        }
        /// <summary>
        /// 接收浏览器请求
        /// </summary>
        /// <param name="webClientTcp"></param>
        /// <param name="port"></param>
        public void RecieveWebTcp(TcpClient webClientTcp, int port)
        {
            EndPoint endPoint = webClientTcp.Client.RemoteEndPoint;
            //当前tcp的guid
            byte[] guid = Guid.NewGuid().ToByteArray();
            string guidKey = guid.ToStringUnicode();
            //网站服务器Tcp
            TcpClient webServerTcp = null;
            //接收缓存
            byte[] buffer = new byte[1024];
            if (m_httpClientMap.ContainsKey(guidKey))
            {
                m_httpClientMap.Remove(guidKey);
            }
            m_httpClientMap.Add(guidKey, webClientTcp);
            //是否第一次
            bool isFirst = true;
            int length = 0;
            NetworkStream readStream = webClientTcp.GetStream();
            while (readStream.CanRead)
            {
                length = 0;
                try
                {
                    length = readStream.Read(buffer, 0, buffer.Length);
                    Logger.Debug.WriteLine("[Port]->[HttpServer] 接收数据，长度{0}",length);
                }
                catch (Exception ex)
                {
                    Logger.Error.WriteLine("[Port]->[HttpServer] 读取来自{0}的tcp数据错误：\r\n{1} ", endPoint, ex);
                }
                string httpServerName = "";
                if (length > 0)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        //读取域名信息
                        string domain = GetHttpRequestHost(buffer, length);
                        HttpModel httpModel = MatchHttpModel(domain, ConfigServer.HttpSettings[port]);
                        httpServerName = httpModel.ServerName;
                        //获取目的服务器
                        if (httpModel != null && m_transferClient.ContainsKey(httpModel.ServerName))
                            webServerTcp = m_transferClient[httpModel.ServerName];

                    }
                    if (webServerTcp == null)
                    {
                        Logger.Info.WriteLine("[HttpServer] 服务{0}不在线!", httpServerName);
                        webClientTcp.Close();
                        break;
                    }
                    List<byte> sendBytes = new List<byte>();
                    sendBytes.AddRange(guid);
                    sendBytes.AddRange(buffer.Take(length));
                    try
                    {
                        //86 86 type1 type2 长度 guid data
                        webServerTcp.WriteAsync(sendBytes.ToArray(), P2PSocketType.Http.Code, P2PSocketType.Http.Transfer.Code);
                        Logger.Error.WriteLine("[HttpServer]->[HttpClient] 向Http服务{0}发送tcp数据，长度{1} ", httpServerName, sendBytes.Count);
                    }
                    catch (Exception ex)
                    {
                        m_transferClient.Remove(httpServerName);
                        Logger.Error.WriteLine("[HttpServer]->[HttpClient] 向Http服务{0}发送tcp数据错误：\r\n{1} ", httpServerName, ex);
                    }
                }
                else
                {
                    Logger.Info.WriteLine("[HttpServer] 接收到长度为0的数据，关闭Tcp!");
                    webClientTcp.Close();
                    break;
                }
            }
            m_httpClientMap.Remove(guidKey);
            BreakHttpRequest(guid, webServerTcp);
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
                List<byte> sendBytes = new List<byte>();
                sendBytes.AddRange(guid);
                sendBytes.AddRange(new byte[1]);
                transferClient.WriteAsync(sendBytes.ToArray(), P2PSocketType.Http.Code, P2PSocketType.Http.Break.Code);
            }
            catch { }
        }

        /// <summary>
        /// 处理服务器response
        /// </summary>
        /// <param name="data"></param>
        public void HandleHttpPackage(byte type, byte[] data, TcpClient tcpClient)
        {
            byte[] curGuid = data.Take(m_guidLength).ToArray();
            string guidKey = curGuid.ToStringUnicode();
            switch (type)
            {
                case P2PSocketType.Http.Transfer.Code:
                    {
                        string key = curGuid.ToStringUnicode();
                        try
                        {
                            if (m_httpClientMap.ContainsKey(key))
                            {
                                byte[] bytes = data.Skip(m_guidLength).ToArray();
                                m_httpClientMap[key].WriteAsync(bytes);
                            }
                        }
                        catch (Exception ex)
                        {
                            m_httpClientMap.Remove(key);
                            Logger.Error.WriteLine("Http服务-[server->web]转发数据失败！\r\n{0}", ex);
                        }
                    }
                    break;
                case P2PSocketType.Http.ServerName.Code:
                    {
                        String httpServerName = data.ToStringUnicode();
                        if (m_transferClient.ContainsKey(httpServerName))
                        {
                            m_transferClient.Remove(httpServerName);
                        }
                        m_transferClient.Add(httpServerName, tcpClient);
                        Logger.Debug.WriteLine("[HttpClient]->[HttpServer] 设置Http服务名:{0}", httpServerName);
                    }
                    break;
                case P2PSocketType.Http.Break.Code:
                    {
                        string key = curGuid.ToStringUnicode();
                        try
                        {
                            m_httpClientMap[key].Close();
                        }
                        catch { }
                        m_httpClientMap.Remove(key);
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
            bool hasHost = false;
            List<byte> byteList = new List<byte>();
            String str = "";
            for (int i = 0; i < length; i++)
            {
                if (bytes[i] == 13 && (i + 1) < length && bytes[i + 1] == 10)
                {
                    String strTemp = Encoding.ASCII.GetString(byteList.ToArray());
                    if (strTemp.Trim().ToLower().StartsWith("host:"))
                    {
                        hasHost = true;
                        break;
                    }
                    else
                    {
                        byteList.Clear();
                    }
                    i++;
                }
                else
                {
                    byteList.Add(bytes[i]);
                }
            }
            if (hasHost)
            {
                str = Encoding.ASCII.GetString(byteList.ToArray());
                int indexOf = str.IndexOf(':');
                if (indexOf > -1) str = str.Substring(indexOf + 1).Trim();
            }
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
