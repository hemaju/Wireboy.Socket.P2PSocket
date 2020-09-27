using System;
using System.Reflection;
using System.IO;
using System.Threading;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace P2PSocket.StartUp
{
    class Program
    {
        static void Main(string[] args)
        {
            bool flag = false;
            if (File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}P2PSocket/P2PSocket.Server.dll"))
            {
                Assembly assembly = Assembly.LoadFrom($"{AppDomain.CurrentDomain.BaseDirectory}P2PSocket/P2PSocket.Server.dll");
                assembly = AppDomain.CurrentDomain.Load(assembly.FullName);
                object obj = assembly.CreateInstance("P2PSocket.Server.CoreModule");
                MethodInfo method = obj.GetType().GetMethod("Start");
                method.Invoke(obj, null);
                flag = true;
            }
            else if (File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}P2PSocket/P2PSocket.Client.dll"))
            {
                Assembly assembly = Assembly.LoadFrom($"{AppDomain.CurrentDomain.BaseDirectory}P2PSocket/P2PSocket.Client.dll");
                assembly = AppDomain.CurrentDomain.Load(assembly.FullName);
                object obj = assembly.CreateInstance("P2PSocket.Client.CoreModule");
                MethodInfo method = obj.GetType().GetMethod("Start");
                method.Invoke(obj, null);
                flag = true;
            }
            if (!flag)
            {
                Console.WriteLine($"在目录{AppDomain.CurrentDomain.BaseDirectory}P2PSocket中，未找到P2PSocket.Client.dll和P2PSocket.Server.dll.");
            }
            object block = new object();
            new Task(() =>
            {
                Monitor.Wait(block);
            }).Wait();
        }
    }
}
