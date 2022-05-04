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

namespace HeartRateMonitorAppPart2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BluetoothControl? bluetoothControl1;

        public MainWindow(BluetoothControl bluetoothControl)
        {
            InitializeComponent();

            bluetoothControl1 = bluetoothControl;

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (bluetoothControl1 != null)
                bluetoothControl1.ReadReady += OnBluetoothReadReady;
        }

        private void OnBluetoothReadReady(object sender, BluetoothReadEventArgs e)
        {
            if (e != null && e.Value != null)
            {
                //HeartRateLiveTextbox.AppendText($"「{e.Entry.Text}」\n{e.Entry.TranslatedText}\n\n");
                HeartRate.Dispatcher.BeginInvoke(() =>
                {
                    HeartRate.Text = e.Value;
                }); 
                //HeartRateLiveTextbox.ScrollToEnd();
            }
        }



    }
}
