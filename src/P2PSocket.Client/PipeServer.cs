using P2PSocket.Core.Enums;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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
        List<LogItem> logItems = new List<LogItem>();

        private void PipeServer_OnWriteLog(object sender, LogInfo e)
        {
            for (int i = logItems.Count - 1; i >= 0; i--)
            {
                LogItem item = logItems[i];
                if (item.item.IsConnected)
                {
                    if (item.level >= e.LogLevel)
                    {
                        WriteLine(item.item, $"{e.Time}:{e.Msg}");
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
            NamedPipeServerStream server = new NamedPipeServerStream("P2PSocket.Client", PipeDirection.InOut, 20, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            PipeSt st = new PipeSt()
            {
                pipe = server,
                buffer = new byte[1024]
            };
            server.BeginWaitForConnection(PipeCallBack, st);
        }
        protected void PipeCallBack(IAsyncResult ar)
        {
            PipeSt st = ar.AsyncState as PipeSt;
            st.pipe.EndWaitForConnection(ar);
            st.pipe.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
            InStart();
        }
        protected void ReadCallBack(IAsyncResult ar)
        {

            PipeSt st = ar.AsyncState as PipeSt;
            int length = st.pipe.EndRead(ar);
            if (length > 0)
            {
                st.pipe.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
                string strData = Encoding.Unicode.GetString(st.buffer.Take(length).ToArray());
                string[] strSplit = strData.Split(' ').Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

                if (strSplit[0] == "ls")
                {
                    WriteLine(st.pipe, "当前监听的端口：3389,12255");
                }
                else if (strSplit[0] == "v")
                {
                    WriteLine(st.pipe, $"客户端版本:{ EasyInject.Get<AppCenter>().SoftVerSion}");
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
                        LogLevel level = EasyInject.Get<AppCenter>().Config.LogLevel;
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
                else
                {
                    WriteLine(st.pipe, "暂不支持此命令");
                }
            }
        }

        private void WriteLine(NamedPipeServerStream pipe, string text)
        {
            byte[] data = Encoding.Unicode.GetBytes(text);
            pipe.BeginWrite(data, 0, data.Length, null, null);
        }
    }
}
