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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace P2PSocket.ConfigWindow
{
    /// <summary>
    /// CellGrid.xaml 的交互逻辑
    /// </summary>
    public partial class CellGrid : UserControl
    {
        public CellGrid()
        {
            InitializeComponent();
        }



        public string LeftText
        {
            get { return (string)GetValue(LeftTextProperty); }
            set { SetValue(LeftTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LeftText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LeftTextProperty =
            DependencyProperty.Register("LeftText", typeof(string), typeof(CellGrid), new PropertyMetadata(""));


        public string CenterText
        {
            get { return (string)GetValue(CenterTextProperty); }
            set { SetValue(CenterTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CenterText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CenterTextProperty =
            DependencyProperty.Register("CenterText", typeof(string), typeof(CellGrid), new PropertyMetadata(""));


        public string RightText
        {
            get { return (string)GetValue(RightTextProperty); }
            set { SetValue(RightTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RightText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RightTextProperty =
            DependencyProperty.Register("RightText", typeof(string), typeof(CellGrid), new PropertyMetadata(""));



        public string LeftSymbol
        {
            get { return (string)GetValue(LeftSymbolProperty); }
            set { SetValue(LeftSymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LeftSymbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LeftSymbolProperty =
            DependencyProperty.Register("LeftSymbol", typeof(string), typeof(CellGrid), new PropertyMetadata("●●●●●●●"));



        public string RightSymbol
        {
            get { return (string)GetValue(RightSymbolProperty); }
            set { SetValue(RightSymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RightSymbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RightSymbolProperty =
            DependencyProperty.Register("RightSymbol", typeof(string), typeof(CellGrid), new PropertyMetadata("●●●●●●●"));








    }
}
