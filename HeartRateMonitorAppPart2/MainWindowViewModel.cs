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
using Prism.Commands;
using System.ComponentModel;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using System.Windows.Threading;

namespace HeartRateMonitorAppPart2
{
    /// <summary>
    /// This class controls the UI and gets the data from the Bluetooth class and displays it
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {
        //setup a instance reference to the bluetoothControl class for bluetooth communication
        public BluetoothControl? BluetoothInterface;
        //sets up a Device list collection that will fire events when data is added it and link that the duplcate list in the bluetoothControl class
        public ObservableCollection<DeviceInformation> DeviceList { get => BluetoothInterface?.DeviceList; }
        //lil bro set this up (figure out how it functions and relate then write up)
        private Dispatcher UIDispatcher;

        //
        public DeviceInformation SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                SetProperty(ref _selectedDevice, value, nameof(SelectedDevice));
                if (BluetoothInterface != null)
                {
                    BluetoothInterface.trackerName = _selectedDevice.Name;
                    BluetoothInterface.blueToothDevice = _selectedDevice;
                }
            }
        }
        private DeviceInformation _selectedDevice;

        public DelegateCommand ConnectCommand => _connectCommand ??= new DelegateCommand(ConnectOrDisconnect);
        private DelegateCommand _connectCommand;

        public async void ConnectOrDisconnect()
        {
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    BluetoothInterface.RunMainControl();
                });
                Connected = true;
            }
            catch (Exception ex)
            {
                Connected = false;
                var err = $"An error has occurred: {ex.Message}";
                Console.WriteLine(err);
                MessageBox.Show(err);
            }
        }

        public bool Connected
        {
            get { return _connected; }
            set
            {
                SetProperty(ref _connected, value, nameof(Connected));
                RaisePropertyChanged(nameof(NotConnected));
            }
        }
        private bool _connected;

        public bool NotConnected
        {
            get { return !Connected; }
        }

        public string HeartRateText
        {
            get { return _heartRateText; }
            set
            {
                SetProperty(ref _heartRateText, value, nameof(HeartRateText));
            }
        }
        private string _heartRateText;


        public MainWindowViewModel(BluetoothControl bluetoothControl)
        {
            BluetoothInterface = bluetoothControl;
        }

        public void Initialize(Dispatcher _UIDispatcher)
        {
            this.UIDispatcher = _UIDispatcher;
            if (BluetoothInterface != null)
            {
                BluetoothInterface.ReadReady += OnBluetoothReadReady;
                BluetoothInterface.SafeInvoke = UISafeInvoke;
                BluetoothInterface.InitializeDeviceWatcher();
            }
        }

        private void OnBluetoothReadReady(object sender, BluetoothReadEventArgs e)
        {
            if (e != null && e.Value != null)
            {
                //HeartRateLiveTextbox.AppendText($"「{e.Entry.Text}」\n{e.Entry.TranslatedText}\n\n");
                //HeartRate.Dispatcher.BeginInvoke(() =>
                //{
                //    HeartRate.Text = e.Value;
                //});
                //HeartRateLiveTextbox.ScrollToEnd();
                HeartRateText = e.Value;
            }
        }

        private void UISafeInvoke(Action _action)
        {
            UIDispatcher.BeginInvoke(() =>
            {
                _action.Invoke();
            });
        }

    }
}


