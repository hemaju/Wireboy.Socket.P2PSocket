using P2PSocket.Core.Enums;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;

namespace P2PSocket.Core.Models
{
    public class BasePipeServer : IPipeServer
    {
        protected class PipeSt
        {
            public NamedPipeServerStream pipe;
            public byte[] buffer;
        }

        protected class LogItem
        {
            public NamedPipeServerStream item;
            public LogLevel level;
        }
        public BasePipeServer()
        {
            EasyInject.Get<ILogger>().OnWriteLog += PipeServer_OnWriteLog;

        }

        /// <summary>
        /// 需要输出日志的管道实例集合
        /// </summary>
        protected List<LogItem> logItems = new List<LogItem>();
        /// <summary>
        /// 命名管道名称
        /// </summary>
        protected string PipeName { set; get; }

        /// <summary>
        /// 日志写入响应方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void PipeServer_OnWriteLog(object sender, LogInfo e)
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

        public virtual void Start(string pipeName = "")
        {
            if (pipeName != "")
                PipeName = pipeName;
            if (string.IsNullOrWhiteSpace(PipeName)) throw new ArgumentException("未设置命名管道名称");
            InStart();
        }

        protected virtual void InStart()
        {
            //创建一个管道监听"P2PSocket.Client"
            NamedPipeServerStream server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 20, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
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
        protected virtual void PipeCallBack(IAsyncResult ar)
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
        protected virtual void ReadCallBack(IAsyncResult ar)
        {
        }

        protected virtual void WriteLine(NamedPipeServerStream pipe, string text)
        {
            PipeSendPacket pipeSend = new PipeSendPacket();
            BinaryUtils.Write(pipeSend.Data, text);
            byte[] data = pipeSend.PackData();
            pipe.BeginWrite(data, 0, data.Length, null, null);
        }
    }
}
