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
        Dictionary<string, TcpClient> m_httpClientMap = new Dictionary<string, TcpClient>();
        P2PService m_p2PService;
        TaskFactory m_taskFactory = new TaskFactory();
        int m_guidLength = Guid.NewGuid().ToByteArray().Length;
        public HttpServer(P2PService p2PService)
        {
            m_p2PService = p2PService;
        }
        public void Start()
        {
            if (!string.IsNullOrEmpty(ConfigServer.AppSettings.HttpServerName))
            {
                try
                {
                    m_p2PService.ServerTcp.WriteAsync(ConfigServer.AppSettings.HttpServerName, P2PSocketType.Http.Code, P2PSocketType.Http.ServerName.Code);
                    Logger.Info.WriteLine("[HttpServer]->[服务器] 成功设置http服务名:{0}", ConfigServer.AppSettings.HttpServerName);
                }
                catch (Exception ex)
                {
                    Logger.Error.WriteLine("[HttpServer]->[服务器] 无法连接服务器，设置http服务名失败！");
                }
            }
            else
            {
                Logger.Info.WriteLine("[HttpServer] 未配置Http服务，跳过启动Http服务！");
            }
        }
        public void HandleHttpPackage(byte type, byte[] data)
        {
            //[长度][guid][数据]
            try
            {
                byte[] curGuid = data.Take(m_guidLength).ToArray();
                string guidKey = curGuid.ToStringUnicode();

                switch (type)
                {
                    case P2PSocketType.Http.Transfer.Code:
                        {
                            byte[] bytes = data.Skip(m_guidLength).ToArray();
                            Logger.Debug.WriteLine("[Web]->[WebServer] 接收到来自浏览器的数据，长度{0}", bytes.Length);
                            if (!m_httpClientMap.ContainsKey(guidKey))
                            {
                                string domain = GetHttpRequestHost(bytes, bytes.Length);
                                ConnectWebServer(curGuid, domain);
                            }
                            if (m_httpClientMap.ContainsKey(guidKey))
                            {
                                try
                                {
                                    m_httpClientMap[guidKey].WriteAsync(bytes);
                                    Logger.Debug.WriteLine("[WebServer]->[Port] 向本地站点发送数据，长度{0}", bytes.Length);
                                }
                                catch (Exception ex)
                                {
                                    m_httpClientMap.Remove(guidKey);
                                    Logger.Error.WriteLine("[WebServer]->[Port] 向本地站点发送数据错误：\r\n{1} ", ex);
                                }
                            }

                        }
                        break;
                    case P2PSocketType.Http.Break.Code:
                        {
                            m_httpClientMap.Remove(guidKey);
                        }
                        break;
                    case P2PSocketType.Http.Error.Code:
                        {
                        }
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void ConnectWebServer(byte[] curGuid, string domain)
        {
            string key_in = curGuid.ToStringUnicode();
            if (!m_httpClientMap.ContainsKey(key_in))
            {
                HttpModel model = MatchHttpModel(domain);
                if (model == null) return;
                //连接网站
                TcpClient tcpClient = new TcpClient(model.WebIp, model.WebPort);
                m_httpClientMap.Add(key_in, tcpClient);
                m_taskFactory.StartNew(() =>
                {
                    string key = key_in;
                    TcpClient client = tcpClient;
                    NetworkStream stream = client.GetStream();
                    byte[] webBytes = new byte[1024];
                    while (stream.CanRead)
                    {
                        if (!m_httpClientMap.ContainsKey(key))
                        {
                            client.Close();
                            break;
                        }
                        int webLength = 0;
                        try
                        {
                            webLength = stream.Read(webBytes, 0, webBytes.Length);
                            Logger.Debug.WriteLine("[Port]->[HttpServer] 接收数据，长度:{0}", webLength);
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug.WriteLine("[Port]->[HttpServer] 连接已断开.");
                            m_httpClientMap.Remove(key);
                        }
                        if (webLength > 0)
                        {
                            List<byte> webRet = new List<byte>();
                            webRet.AddRange(curGuid);
                            webRet.AddRange(webBytes.Take(webLength));
                            //发送数据
                            try
                            {
                                m_p2PService.ServerTcp.WriteAsync(webRet.ToArray(), P2PSocketType.Http.Code, P2PSocketType.Http.Transfer.Code);
                                Logger.Debug.WriteLine("[HttpServer]->[Web] 发送数据，长度:{0}", webLength);
                            }
                            catch (Exception ex)
                            {
                                Logger.Debug.WriteLine("[HttpServer]->[Web] 连接已断开.");
                            }
                        }
                        else
                        {
                            Logger.Debug.WriteLine("[Port]->[HttpServer] 接收到长度0的数据，断开连接！");
                            client.Close();
                            List<byte> webRet = new List<byte>();
                            webRet.AddRange(curGuid);
                            m_p2PService.ServerTcp.WriteAsync(webRet.ToArray(), P2PSocketType.Http.Code, P2PSocketType.Http.Break.Code);
                            break;
                        }
                    }
                    m_httpClientMap.Remove(key);
                });
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
        public HttpModel MatchHttpModel(string domain)
        {
            string webDomain = domain.Split(':').FirstOrDefault();
            return ConfigServer.HttpSettings.Where(t =>
            {
                if (webDomain.StartsWith(t.Domain))
                    return true;
                return false;
            }).FirstOrDefault();
        }
    }
}
