using P2PSocket.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace P2PSocket.StartUp_Wpf
{
    /// <summary>
    /// PortMapItemWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PortMapItemWindow : Window
    {
        public PortMapItemWindow()
        {
            InitializeComponent();
            ConfigCenter.Instance.PortMapList.ForEach(t =>
            {
                TextBox item = new TextBox();
                item.IsReadOnly = true;
                string localAddress = string.IsNullOrEmpty(t.LocalAddress) ? "127.0.0.1" : t.LocalAddress;
                item.Text = $"{localAddress}:{t.LocalPort} -> {t.RemoteAddress}:{t.RemotePort}";
                stackPanel.Children.Add(item);
            });
        }
    }
}
