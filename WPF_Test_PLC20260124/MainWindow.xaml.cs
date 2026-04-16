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

namespace WPF_Test_PLC20260124
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void JogBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && DataContext is MainViewModel vm)
            {
                vm.SetMBit(btn.Tag.ToString(), true);
            }
        }

        private void JogBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && DataContext is MainViewModel vm)
            {
                vm.SetMBit(btn.Tag.ToString(), false);
            }
        }

        private void JogBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Button btn && btn.Tag != null && DataContext is MainViewModel vm)
            {
                vm.SetMBit(btn.Tag.ToString(), false);
            }
        }
    }
}
