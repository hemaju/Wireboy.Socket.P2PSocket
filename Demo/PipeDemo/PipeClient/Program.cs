/*
 * 
 * 
 * 
 * 
 * 
 *                      这只是一个demo，教大家怎么用管道命令
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.IO;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;

namespace PipeClient
{
    class Program
    {
        static void Main(string[] args)
        {
            PipeClient client = new PipeClient();
            while (true)
            {
                Console.Write(":");
                client.WriteLine(Console.ReadLine());
            }
        }
        class PipeSt
        {
            public NamedPipeClientStream pipe;
            public byte[] buffer;
            public PipeRecievePacket packet;
        }
        public class PipeClient
        {
            NamedPipeClientStream client = new NamedPipeClientStream(".", "P2PSocket.Client", PipeDirection.InOut, PipeOptions.Asynchronous);
            IAsyncResult gar;
            public PipeClient()
            {
                PipeSt st = new PipeSt()
                {
                    pipe = client,
                    buffer = new byte[10240],
                    packet = new PipeRecievePacket()
                };
                client.Connect(2000);
                gar = client.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
            }
            public void ReadCallBack(IAsyncResult ar)
            {
                PipeSt st = ar.AsyncState as PipeSt;
                int length = st.pipe.EndRead(ar);
                if (length > 0)
                {
                    byte[] refData = st.buffer.Take(length).ToArray();

                    while (st.packet.ParseData(ref refData))
                    {
                        using (BinaryReader reader = new BinaryReader(new MemoryStream(st.packet.Data)))
                        {
                            //Console.WriteLine($"数据：{length} 长度：{reader.BaseStream.Length}");
                            string str = BinaryUtils.ReadString(reader);
                            Console.CursorLeft = 0;
                            Console.WriteLine("From Client：" + str);
                            Console.Write(":");
                            //重置msgReceive
                            st.packet.Reset();
                            if (refData.Length <= 0) break;
                        }
                    }
                    st.pipe.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
                }
            }
            public void WriteLine(string str)
            {
                //StreamWriter writer = new StreamWriter(client);
                //writer.WriteLine(str);
                //writer.Close();
                byte[] data = Encoding.Unicode.GetBytes(str);
                client.Write(data, 0, data.Length);
            }
        }
    }
}
