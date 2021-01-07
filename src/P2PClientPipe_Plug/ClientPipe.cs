using P2PSocket.Client;
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
using w_pipe = wireboy.net.pipe;

namespace P2PClientPipe_Plug
{
    public class ClientPipe : IPipeServer
    {
        protected class LogItem
        {
            public PipeStream item;
            public LogLevel level;
        }
        /// <summary>
        /// 需要输出日志的管道实例集合
        /// </summary>
        protected List<LogItem> logItems = new List<LogItem>();
        AppCenter appCenter;
        w_pipe.PipeServer pipeServer;
        public ClientPipe()
        {
            appCenter = EasyInject.Get<AppCenter>();
        }

        public void Start(string pipeName = "")
        {
            pipeServer = new w_pipe.PipeServer();
            pipeServer.StartListen(pipeName);
            EasyInject.Get<ILogger>().OnWriteLog += PipeServer_OnWriteLog;
            //订阅“命令”消息
            pipeServer.RegisterHandler<string>("ExcuteCmd", ReadCallBack);
            //订阅“获取监听端口”消息
            pipeServer.RegisterHandler<string>("GetListenPorts", (_, pipe) =>
            {
                string ret = "";
                for (int i = 0; i < appCenter.Config.PortMapList.Count; i++)
                {
                    PortMapItem item = appCenter.Config.PortMapList[i];
                    if (i != appCenter.Config.PortMapList.Count - 1)
                        ret += item.LocalPort + ",";
                    else
                        ret += item.LocalPort;
                }
                //发送“监听端口”信息
                w_pipe.PipeServer.SendMsg(pipe, "GetListenPorts_R", ret);
            });
            //订阅“获取版本”消息
            pipeServer.RegisterHandler<string>("GetVersionInfo", (_, pipe) =>
            {
                //发送“版本信息”
                w_pipe.PipeServer.SendMsg(pipe, "GetVersionInfo_R", EasyInject.Get<AppCenter>().SoftVerSion);
            });
        }

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
                            ReplayCmdMsg(item.item, $"{e.Time}:{e.Msg}");
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

        private void ReplayCmdMsg(PipeStream pipe, string text)
        {
            w_pipe.PipeServer.SendMsg(pipe, "ExcuteCmd_R", text);
        }
        /// <summary>
        /// 管道数据读取回调方法
        /// </summary>
        /// <param name="ar"></param>
        protected void ReadCallBack(string strData, PipeStream pipe)
        {
            string[] strSplit = strData.Split(' ').Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

            if (strSplit[0] == "ls")
            {
                string msg = "当前监听端口：";
                appCenter.Config.PortMapList.ForEach(t => { msg += t.LocalPort + " "; });
                ReplayCmdMsg(pipe, msg);
            }
            else if (strSplit[0] == "v")
            {
                ReplayCmdMsg(pipe, $"当前版本 { EasyInject.Get<AppCenter>().SoftVerSion}");
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
                            ReplayCmdMsg(pipe, "添加/修改端口映射成功!");
                            LogUtils.Info($"管道命令:添加/修改端口映射 {obj.LocalPort}->{obj.RemoteAddress}:{obj.RemotePort}");
                        }
                        else
                            ReplayCmdMsg(pipe, "添加/修改端口映射失败,请参考wiki中的端口映射配置项!");
                    }
                    else
                        ReplayCmdMsg(pipe, "添加/修改端口映射失败,请参考wiki中的端口映射配置项!");
                }, e =>
                {
                    ReplayCmdMsg(pipe, $"添加/修改端口映射异常:{e}");
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
                        ReplayCmdMsg(pipe, "移除端口映射成功!");
                        LogUtils.Info($"管道命令:移除端口映射 {localPort}");
                    }, e =>
                    {
                        ReplayCmdMsg(pipe, $"移除端口映射异常:{e}");
                    });
                }
                else
                {
                    ReplayCmdMsg(pipe, "移除端口映射失败!");
                }
            }
            else if (strSplit[0] == "log")
            {
                if (strSplit.Length == 2 && strSplit[1] == "-s")
                {
                    LogItem pipeSt = logItems.FirstOrDefault(t => t.item == pipe);
                    logItems.Remove(pipeSt);
                    ReplayCmdMsg(pipe, "停止记录日志");
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
                    if (!logItems.Any(t => t.item == pipe))
                    {
                        LogItem item = new LogItem();
                        item.item = pipe;
                        item.level = level;
                        logItems.Add(item);
                        ReplayCmdMsg(pipe, $"开始记录日志，级别：{level}");
                    }
                    else
                    {
                        LogItem pipeSt = logItems.FirstOrDefault(t => t.item == pipe);
                        pipeSt.level = level;
                        ReplayCmdMsg(pipe, $"设置日志级别：{level}");
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
                ReplayCmdMsg(pipe, msg);

            }
            else
            {
                ReplayCmdMsg(pipe, "暂不支持此命令,输入\"h\"查看帮助");
            }
        }
    }
}
