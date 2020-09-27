using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace P2PSocket.StartUp_Windows
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "-install")
                {
                    string serviceName = "P2PSocket";
                    if (args.Length > 1) serviceName = args[1];
                    try
                    {
                        Console.WriteLine("服务名 >> " + serviceName);
                        ServiceIO service = new ServiceIO();
                        //service.InstallService(AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(t => t.FullName.Contains("P2PSocket.StartUp_Windows")).Location);
                        //Console.ReadKey();
                        //return;
                        string filePath = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(t => t.FullName.Contains("P2PSocket.StartUp_Windows")).Location.Replace(".dll", ".exe");
                        Console.WriteLine(filePath);
                        service.ServiceStop(serviceName);
                        Console.WriteLine("服务已停止");
                        service.UninstallService(serviceName);
                        Thread.Sleep(1000);
                        Console.WriteLine("服务已卸载");
                        service.InstallService(serviceName, filePath);
                        Thread.Sleep(1000);
                        Console.WriteLine("服务已安装");
                        service.ServiceStart(serviceName);
                        Thread.Sleep(1000);
                        Console.WriteLine("服务已启动");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                else if (args.Any(t => t.ToLower() == "-ws"))
                {
                    ServiceBase.Run(new P2PSocket());
                }
                else if (args.Any(t => t.ToLower() == "-uninstall"))
                {
                    string serviceName = "P2PSocket";
                    if (args.Length > 1) serviceName = args[1];
                    try
                    {
                        Console.WriteLine("服务名 >> " + serviceName);
                        ServiceIO service = new ServiceIO();
                        service.ServiceStop(serviceName);
                        Console.WriteLine("服务已停止");
                        service.UninstallService(serviceName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                }
            }
            else
            {

                try
                {
                    bool flag = P2PSocket.StartServer(AppDomain.CurrentDomain) | P2PSocket.StartClient(AppDomain.CurrentDomain);
                    if (!flag)
                    {
                        Console.WriteLine($"在目录{AppDomain.CurrentDomain.BaseDirectory}P2PSocket中，未找到P2PSocket.Client.dll和P2PSocket.Server.dll.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            object block = new object();
            new Task(() =>
            {
                Monitor.Wait(block);
            }).Wait();
        }
    }
    public class ServiceIO
    {
        public bool IsServiceExisted(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController sc in services)
            {
                if (sc.ServiceName.ToLower() == serviceName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        //安装服务
        public void InstallService(string serviceName, string serviceFilePath)
        {
            Console.WriteLine(serviceFilePath);
            ExcuteCmd($"sc create {serviceName} binPath=\"{serviceFilePath} -ws\" start=auto displayname=wireboy内网穿透-" + serviceName);
        }
        public void ServiceStart(string serviceName)
        {
            using (ServiceController control = ServiceController.GetServices().FirstOrDefault(t => t.ServiceName.ToLower() == serviceName.ToLower()))
            {
                if (control.Status == ServiceControllerStatus.Stopped)
                {
                    control.Start(new string[] { "-ws" });
                }
            }
        }
        public void UninstallService(string serviceName)
        {
            ExcuteCmd($"sc delete {serviceName}");
        }
        public void ServiceStop(string serviceName)
        {
            using (ServiceController control = ServiceController.GetServices().FirstOrDefault(t => t.ServiceName.ToLower() == serviceName.ToLower()))
            {
                if (control != null)
                {
                    if (control.Status == ServiceControllerStatus.Running)
                    {
                        control.Stop();
                    }
                }
            }
        }
        public void ExcuteCmd(string cmdMsg)
        {

            Process p = new Process();
            //设置要启动的应用程序
            p.StartInfo.FileName = "cmd.exe";
            //是否使用操作系统shell启动
            p.StartInfo.UseShellExecute = false;
            // 接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardInput = true;
            //输出信息
            p.StartInfo.RedirectStandardOutput = true;
            // 输出错误
            p.StartInfo.RedirectStandardError = true;
            //不显示程序窗口
            p.StartInfo.CreateNoWindow = true;
            //启动程序
            p.Start();

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(cmdMsg + "&exit");
            //p.StandardInput.WriteLine(cmdMsg);

            p.StandardInput.AutoFlush = true;

            //获取输出信息
            string strOuput = p.StandardOutput.ReadToEnd();
            //等待程序执行完退出进程
            p.WaitForExit();
            p.Close();
            Console.WriteLine(strOuput);
        }

    }
}
