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
        List<Object> ModuleList = new List<object>();
        public P2PSocket()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                bool flag = StartServer(AppDomain.CurrentDomain) | StartClient(AppDomain.CurrentDomain);
            }
            catch (Exception ex)
            {
                StreamWriter ss = new StreamWriter($"{AppDomain.CurrentDomain.BaseDirectory}P2PSocket/Error.log");
                ss.WriteLine(ex.Message);
                ss.Close();
                throw ex;
            }
        }

        protected override void OnStop()
        {
            // TODO: 在此处添加代码以执行停止服务所需的关闭操作。
            foreach (Object obj in ModuleList)
            {
                try
                {
                    MethodInfo method = obj.GetType().GetMethod("Stop");
                    method.Invoke(obj, null);
                }
                catch (Exception ex)
                {
                }
            }
        }
        public static bool StartClient(AppDomain appDomain)
        {
            bool ret = false;

            if (File.Exists($"{appDomain.BaseDirectory}P2PSocket/P2PSocket.Client.dll"))
            {
                Assembly assembly = Assembly.LoadFrom($"{appDomain.BaseDirectory}P2PSocket/P2PSocket.Client.dll");
                assembly = appDomain.Load(assembly.FullName);
                object obj = assembly.CreateInstance("P2PSocket.Client.CoreModule");
                MethodInfo method = obj.GetType().GetMethod("Start");
                method.Invoke(obj, null);
                ret = true;
            }
            return ret;
        }
        public static bool StartServer(AppDomain appDomain)
        {
            bool ret = false;
            if (File.Exists($"{appDomain.BaseDirectory}P2PSocket/P2PSocket.Server.dll"))
            {
                Assembly assembly = Assembly.LoadFrom($"{appDomain.BaseDirectory}P2PSocket/P2PSocket.Server.dll");
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
