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
using LiveCharts;
using LiveCharts.Wpf;

namespace HeartRateMonitorAppPart2
{
    /// <summary>
    /// This class controls the UI and gets the data from the Bluetooth class and displays it
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {

        public bool startLog = false;

        private string _heartRateText;
        public ChartValues<double> HRValues { get; set; }

        public SeriesCollection SeriesCollection { get; set; }

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

        #region "Commands"
        public DelegateCommand ConnectCommand => _connectCommand ??= new DelegateCommand(ConnectOrDisconnect);
        private DelegateCommand _connectCommand;

        private bool _connecting;

        public async void ConnectOrDisconnect()
        {
            if (_connecting)
                return;
            try
            {
                _connecting = true;
                await Task.Factory.StartNew(() =>
                {
                    BluetoothInterface.RunMainControl();
                    _connecting = false;
                });
                Connected = true;
            }
            catch (Exception ex)
            {
                _connecting = false;
                Connected = false;
                var err = $"An error has occurred: {ex.Message}";
                Console.WriteLine(err);
                MessageBox.Show(err);
            }
        }
        public DelegateCommand Start => _start ??= new DelegateCommand(StartLogs);
        private DelegateCommand _start;
        private void StartLogs()
        {
            startLog = true;
            HRValues.Clear();
        }

        public DelegateCommand Stop => _stop ??= new DelegateCommand(StopLog);

        private void StopLog()
        {
            startLog = false;
        }

        private DelegateCommand _stop;


        #endregion

        #region "Properties"
        public bool IsMale
        {
            get { return _isMale; }
            set
            {
                SetProperty(ref _isMale, value, nameof(IsMale));
            }
        }
        private bool _isMale = true;

        public string AgeText
        {
            get { return _ageText; }
            set
            {
                SetProperty(ref _ageText, value, nameof(AgeText));
            }
        }
        private string _ageText;

        public string WeightText
        {
            get { return _weightText; }
            set
            {
                SetProperty(ref _weightText, value, nameof(WeightText));
            }
        }
        private string _weightText;

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

        #endregion

        public MainWindowViewModel(BluetoothControl bluetoothControl)
        {
            BluetoothInterface = bluetoothControl;
            GraphControl();
        }

        public void GraphControl()
        {
            HRValues = new ChartValues<double> { 95, 95, 95, 95, 95, 95 };
            //{ 95, 130, 100, 60, 85, 175 };
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    LineSmoothness= 0,
                    Values = HRValues,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent,
                    PointGeometry= null,
                }
            };  
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
                if (startLog)
                {
                    HRValues?.Add(Convert.ToDouble(e.Value));
                }
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


