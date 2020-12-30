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
        static string RunDirName = "P2PSocket";
        static void Main(string[] args)
        {
            bool flag = false;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            string serverFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RunDirName, "P2PSocket.Server.dll");
            string clientFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RunDirName, "P2PSocket.Client.dll");
            if (File.Exists(serverFilePath))
            {
                Assembly assembly = Assembly.LoadFrom(serverFilePath);
                assembly = AppDomain.CurrentDomain.Load(assembly.FullName);
                object obj = assembly.CreateInstance("P2PSocket.Server.CoreModule");
                MethodInfo method = obj.GetType().GetMethod("Start");
                method.Invoke(obj, null);
                flag = true;
            }
            else if (File.Exists(clientFilePath))
            {
                Assembly assembly = Assembly.LoadFrom(clientFilePath);
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
            Monitor.Enter(block);
            Monitor.Wait(block);
            Monitor.Exit(block);
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name.ToLower().Contains("p2psocket.") || assemblyName.Name.ToLower().Contains("wireboy."))
            {
                return Assembly.LoadFrom(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "P2PSocket"), assemblyName.Name + ".dll"));
            }
            return null;
        }
    }
}
