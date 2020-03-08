using P2PSocket.Client;
using P2PSocket.Client.Utils;
using P2PSocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace P2PSocket.StartUp_Wpf
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        CoreModule clientModule = new CoreModule();
        MainViewModel dataContext = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = dataContext;
            Task.Factory.StartNew(UpdateP2PMessage);
            LogUtils.ClientRecordLogHandler = Instance_RecordLogEvent;
            clientModule.Start();
        }

        public void UpdateP2PMessage()
        {
            while (true)
            {
                dataContext.ServerAddress = ConfigCenter.Instance.ServerAddress;
                dataContext.TcpCount = TcpCenter.Instance.ConnectedTcpList.Count;
                Thread.Sleep(1000);
            }
        }
        public void Instance_RecordLogEvent(System.IO.StreamWriter ss, LogInfo logInfo)
        {
            if (LogUtils.Instance.LogLevel >= logInfo.LogLevel)
            {
                dataContext.LogMessage += "Client > " + logInfo.Msg;
                dataContext.LogMessage += Environment.NewLine;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PortMapItemWindow fm = new PortMapItemWindow();
            fm.ShowDialog();
        }
    }
}
