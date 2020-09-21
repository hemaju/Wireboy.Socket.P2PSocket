using P2PSocket.Client.Utils;
using P2PSocket.Core.Enums;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace P2PSocket.Client
{
    public class PipeServer : IPipeServer
    {
        class PipeSt
        {
            public NamedPipeServerStream pipe;
            public byte[] buffer;
        }

        class LogItem
        {
            public NamedPipeServerStream item;
            public LogLevel level;
        }
        public PipeServer()
        {
            EasyInject.Get<ILogger>().OnWriteLog += PipeServer_OnWriteLog;

        }

        /// <summary>
        /// 需要输出日志的管道实例集合
        /// </summary>
        List<LogItem> logItems = new List<LogItem>();

        /// <summary>
        /// 日志写入响应方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PipeServer_OnWriteLog(object sender, LogInfo e)
        {
            for (int i = logItems.Count - 1; i >= 0; i--)
            {
                LogItem item = logItems[i];
                if (item.item.IsConnected)
                {
                    if (item.level >= e.LogLevel)
                    {
                        EasyOp.Do(() =>
                        {
                            WriteLine(item.item, $"{e.Time}:{e.Msg}");
                        }, ex =>
                        {
                            //管道写入发生异常，则不再向此管道实例写入日志
                            logItems.RemoveAt(i);
                        });
                    }
                }
                else
                {
                    logItems.RemoveAt(i);
                }
            }
        }

        public void Start()
        {
            InStart();
        }

        protected void InStart()
        {
            //创建一个管道监听
            NamedPipeServerStream server = new NamedPipeServerStream("P2PSocket.Client", PipeDirection.InOut, 20, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            PipeSt st = new PipeSt()
            {
                pipe = server,
                buffer = new byte[1024]
            };
            server.BeginWaitForConnection(PipeCallBack, st);
        }

        /// <summary>
        /// 接收到管道连接的回调方法
        /// </summary>
        /// <param name="ar"></param>
        protected void PipeCallBack(IAsyncResult ar)
        {
            PipeSt st = ar.AsyncState as PipeSt;
            st.pipe.EndWaitForConnection(ar);
            st.pipe.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
            InStart();
        }

        /// <summary>
        /// 管道数据读取回调方法
        /// </summary>
        /// <param name="ar"></param>
        protected void ReadCallBack(IAsyncResult ar)
        {

            PipeSt st = ar.AsyncState as PipeSt;
            int length = st.pipe.EndRead(ar);
            if (length > 0)
            {
                st.pipe.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
                string strData = Encoding.Unicode.GetString(st.buffer.Take(length).ToArray());
                string[] strSplit = strData.Split(' ').Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
                AppCenter appCenter = EasyInject.Get<AppCenter>();

                if (strSplit[0] == "ls")
                {
                    string msg = "当前监听端口：";
                    appCenter.Config.PortMapList.ForEach(t => { msg += t.LocalPort + " "; });
                    WriteLine(st.pipe, msg);
                }
                else if (strSplit[0] == "v")
                {
                    WriteLine(st.pipe, $"当前版本 { EasyInject.Get<AppCenter>().SoftVerSion}");
                }
                else if (strSplit[0] == "use")
                {
                    IConfig configManager = EasyInject.Get<IConfig>();
                    EasyOp.Do(() =>
                    {
                        PortMapItem obj = configManager.ParseToObject("[PortMapItem]", strSplit[1]) as PortMapItem;
                        if (obj != null)
                        {
                            //设置配置文件更新时间，避免出发重加载配置文件逻辑
                            appCenter.LastUpdateConfig = DateTime.Now;
                            //添加/修改指定项到配置文件
                            configManager.SaveItem(obj);
                            P2PClient client = EasyInject.Get<P2PClient>();
                            //监听/修改端口映射
                            if (client.UsePortMapItem(obj))
                            {
                                appCenter.Config.PortMapList.Add(obj);
                                WriteLine(st.pipe, "添加/修改端口映射成功!");
                                LogUtils.Info($"管道命令:添加/修改端口映射 {obj.LocalPort}->{obj.RemoteAddress}:{obj.RemotePort}");
                            }
                            else
                                WriteLine(st.pipe, "添加/修改端口映射失败,请参考wiki中的端口映射配置项!");
                        }
                        else
                            WriteLine(st.pipe, "添加/修改端口映射失败,请参考wiki中的端口映射配置项!");
                    }, e =>
                    {
                        WriteLine(st.pipe, $"添加/修改端口映射异常:{e}");
                    });
                }
                else if (strSplit[0] == "del")
                {
                    int localPort;
                    if (int.TryParse(strSplit[1], out localPort))
                    {
                        EasyOp.Do(() =>
                        {
                            //设置配置文件更新时间，避免出发重加载配置文件逻辑
                            appCenter.LastUpdateConfig = DateTime.Now;
                            P2PClient client = EasyInject.Get<P2PClient>();
                            //停止监听端口
                            client.UnUsePortMapItem(localPort);
                            IConfig configManager = EasyInject.Get<IConfig>();
                            //移除配置文件中指定项
                            configManager.RemoveItem(new PortMapItem() { LocalPort = localPort });
                            WriteLine(st.pipe, "移除端口映射成功!");
                            LogUtils.Info($"管道命令:移除端口映射 {localPort}");
                        }, e =>
                        {
                            WriteLine(st.pipe, $"移除端口映射异常:{e}");
                        });
                    }
                    else
                    {
                        WriteLine(st.pipe, "移除端口映射失败!");
                    }
                }
                else if (strSplit[0] == "log")
                {
                    if (strSplit.Length == 2 && strSplit[1] == "-s")
                    {
                        LogItem pipeSt = logItems.FirstOrDefault(t => t.item == st.pipe);
                        logItems.Remove(pipeSt);
                        WriteLine(st.pipe, "停止记录日志");
                    }
                    else
                    {
                        LogLevel level = appCenter.Config.LogLevel;
                        if (strSplit.Length == 2)
                        {
                            switch (strSplit[1].ToLower())
                            {
                                case "debug": level = LogLevel.Debug; break;
                                case "error": level = LogLevel.Error; break;
                                case "info": level = LogLevel.Info; break;
                                case "none": level = LogLevel.None; break;
                                case "warning": level = LogLevel.Warning; break;
                                case "trace": level = LogLevel.Trace; break;
                            }
                        }
                        if (!logItems.Any(t => t.item == st.pipe))
                        {
                            LogItem item = new LogItem();
                            item.item = st.pipe;
                            item.level = level;
                            logItems.Add(item);
                            WriteLine(st.pipe, $"开始记录日志，级别：{level}");
                        }
                        else
                        {
                            LogItem pipeSt = logItems.FirstOrDefault(t => t.item == st.pipe);
                            pipeSt.level = level;
                            WriteLine(st.pipe, $"设置日志级别：{level}");
                        }
                    }
                }
                else if (strSplit[0] == "h")
                {
                    string msg = "";
                    using (MemoryStream ms = new MemoryStream())
                    {
                        StreamWriter writer = new StreamWriter(ms);
                        writer.WriteLine("1.开始日志打印: log [Debug/Info/Warning/Trace]");
                        writer.WriteLine("2.结束日志打印: log -s");
                        writer.WriteLine("3.获取监听端口: ls");
                        writer.WriteLine("4.获取当前版本: v");
                        writer.WriteLine("5.添加/修改端口映射: use 映射配置  (例：\"use 12345->[ClientA]:3389\")");
                        writer.WriteLine("6.删除指定端口映射: del 端口号 (例：\"del 3388\")");
                        writer.Close();
                        msg = Encoding.UTF8.GetString(ms.ToArray());

                    }
                    WriteLine(st.pipe, msg);

                }
                else
                {
                    WriteLine(st.pipe, "暂不支持此命令,输入\"h\"查看帮助");
                }
            }
        }

        private void WriteLine(NamedPipeServerStream pipe, string text)
        {
            PipeSendPacket pipeSend = new PipeSendPacket();
            BinaryUtils.Write(pipeSend.Data, text);
            byte[] data = pipeSend.PackData();
            pipe.BeginWrite(data, 0, data.Length, null, null);
        }
    }
}
