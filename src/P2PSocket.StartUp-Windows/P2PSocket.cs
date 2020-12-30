using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace P2PSocket.StartUp_Windows
{
    partial class P2PSocket : ServiceBase
    {
        static string RunDirName = "P2PSocket";
        public P2PSocket()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (!StartServer(AppDomain.CurrentDomain))
                {
                    StartClient(AppDomain.CurrentDomain);
                }
            }
            catch (Exception ex)
            {
                StreamWriter ss = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RunDirName, "Error.log"));
                ss.WriteLine(ex);
                ss.Close();
                throw ex;
            }
        }

        protected override void OnStop()
        {
            Environment.Exit(0);
        }
        public static bool StartClient(AppDomain appDomain)
        {
            bool ret = false;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RunDirName, "P2PSocket.Client.dll");
            if (File.Exists(filePath))
            {
                Assembly assembly = Assembly.LoadFrom(filePath);
                assembly = appDomain.Load(assembly.FullName);
                object obj = assembly.CreateInstance("P2PSocket.Client.CoreModule");
                MethodInfo method = obj.GetType().GetMethod("Start");
                method.Invoke(obj, null);
                ret = true;
            }
            return ret;
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

        public static bool StartServer(AppDomain appDomain)
        {
            bool ret = false;
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RunDirName, "P2PSocket.Server.dll");
            if (File.Exists(filePath))
            {
                Assembly assembly = Assembly.LoadFrom(filePath);
                assembly = appDomain.Load(assembly.FullName);
                object obj = assembly.CreateInstance("P2PSocket.Server.CoreModule");
                MethodInfo method = obj.GetType().GetMethod("Start");
                method.Invoke(obj, null);
                ret = true;
            }
            return ret;
        }
    }
}
