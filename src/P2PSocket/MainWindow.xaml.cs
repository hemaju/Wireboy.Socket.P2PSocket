using P2PSocektLib;
using P2PSocektLib.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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

namespace P2PSocket
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public string LogText
        {
            get { return (string)GetValue(LogTextProperty); }
            set { SetValue(LogTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LogText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LogTextProperty =
            DependencyProperty.Register("LogText", typeof(string), typeof(MainWindow), new PropertyMetadata(""));


        P2PListener? P2PListener;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnClick_StartListen(object sender, RoutedEventArgs e)
        {
            P2PListener = new P2PListener(11382);
            P2PListener.Start();
            P2PListener.BindAcceptConnectionEvent(async conn =>
            {
                try
                {
                    do
                    {
                        byte[] buffer = new byte[1024];
                        int length = await conn.ReadData(buffer, buffer.Length);
                        if (length > 0)
                        {
                            string text = Encoding.UTF8.GetString(buffer, 0, length);
                            LogText += text + Environment.NewLine;
                            await conn.SendData(new byte[] { 1, 2, 3, 4, 5 }, 5);
                        }
                        else
                        {
                            break;
                        }
                    } while (true);
                }
                catch (Exception ex)
                {
                    LogText += (ex.ToString()) + Environment.NewLine;
                }
            });
        }
        P2PConnect? conn;
        private void BtnClick_NewConnect(object sender, RoutedEventArgs e)
        {
            if (conn != null)
            {
                conn.Close();
            }
            conn = new P2PConnect();
            conn.Connect("127.0.0.1", 11382);
        }

        private void BtnClick_SendData(object sender, RoutedEventArgs e)
        {
            _ = conn.SendData(Encoding.UTF8.GetBytes("这是一段测试文本"));
        }

        P2PSocketSdk? sdk;
        IP2PClient? client;
        IP2PServer? server;
        private void BtnClick_StartClient(object sender, RoutedEventArgs e)
        {
            if (sdk == null)
                sdk = new P2PSocketSdk();
            client = sdk.CreateClient("127.0.0.1", 8899);
            client.UpdatePortMapItem(new PortMapItem(33542, 80, "192.168.124.1", P2PSocektLib.Enum.P2PMode.IP直连));
            client.ConnectServer();

        }

        private void BtnClick_StartServer(object sender, RoutedEventArgs e)
        {
            if (sdk == null)
                sdk = new P2PSocketSdk();
            server = sdk.CreateServer(8899);
            server.StartListen();
        }
    }
}
