using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Prism.Commands;
using Windows.Devices.Enumeration;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using System.Diagnostics;

namespace HeartRateMonitorAppPart2
{
    /// <summary>
    /// This class controls the UI and gets the data from the Bluetooth class and displays it
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {

        private bool startLog = false;
        private string _heartRateText = "";
        private int averageHeartRate = 60; //first value for averaging
        private Stopwatch stopWatch = new();
        private TimeSpan tspan;

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
            if (_ageText != null && _weightText != null)
            {
                stopWatch.Start();
                _statsEnabled = false;
                startLog = true;
                HRValues.Clear();
            }
            else
            {
                MessageBox.Show("Error! please fill in Age and weight fields");
            }
        }

        public DelegateCommand Stop => _stop ??= new DelegateCommand(StopLog);

        private void StopLog()
        {
            startLog = false;
            _statsEnabled=true; 
            stopWatch.Stop();
        }

        private DelegateCommand _stop;


        #endregion

        #region "Properties"
        public bool StatsEnabled
        {
            get { return _statsEnabled; }
            set
            {
                SetProperty(ref _statsEnabled,value, nameof(StatsEnabled));
            }
        }
        private bool _statsEnabled = true;

        public string Calories
        {
            get { return _calories; }
            set
            {
                SetProperty(ref _calories, value, nameof(Calories));
            }
        }
        private string _calories;

        public string TimeElapsed
        {
            get { return _timeElapsed; }
            set
            {
                SetProperty(ref _timeElapsed, value, nameof(TimeElapsed));
            }
        }
        private string _timeElapsed;

        public bool IsMale
        {
            get { return _isMale; }
            set
            {
                SetProperty(ref _isMale, value, () => { SettingsService.Tracker.Persist(this); }, nameof(IsMale));
            }
        }
        private bool _isMale = true;

        public string AgeText
        {
            get { return _ageText; }
            set
            {
                SetProperty(ref _ageText, value, () => { SettingsService.Tracker.Persist(this); }, nameof(AgeText));
            }
        }
        private string _ageText;

        public string WeightText
        {
            get { return _weightText; }
            set
            {
                SetProperty(ref _weightText, value, () => { SettingsService.Tracker.Persist(this); }, nameof(WeightText));
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

        private SettingsService SettingsService { get; set; }

        public MainWindowViewModel(BluetoothControl bluetoothControl, SettingsService _settingsService)
        {
            BluetoothInterface = bluetoothControl;
            SettingsService = _settingsService;

            SettingsService.Tracker.Configure<MainWindowViewModel>()
                                    .Properties(w => new { w.AgeText, w.WeightText, w.IsMale });
            SettingsService.Tracker.Track(this);

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
                    //sends heart rate to graph
                    HRValues?.Add(Convert.ToDouble(e.Value));
                    //math calculation for calories
                    averageHeartRate = (averageHeartRate + Convert.ToInt32(e.Value)) / 2 ;

                    var heartRate = Convert.ToDouble(averageHeartRate);
                    var weight = Convert.ToDouble(WeightText);
                    var age = Convert.ToDouble(AgeText);
                    var caloriesBurned = 0.0;
                    var hours = (double)tspan.TotalHours;

                    //display calories
                    //display time elapsed
                    tspan = stopWatch.Elapsed;
                    TimeElapsed = String.Format("{0:00}:{1:00}:{2:00}",tspan.Hours, tspan.Minutes, tspan.Seconds);
                    if (_isMale)
                    {
                        caloriesBurned = ((-55.0969 + (0.6309 * heartRate) + (0.1988 * weight) + (0.2017 * age)) / 4.184) * 60 * hours;
                    }
                    else
                    {
                        caloriesBurned = ((-20.4022 + (0.4472 * heartRate) + (0.1263 * weight) + (0.074 * age)) / 4.184) * 60 * hours;
                    }
                    caloriesBurned = Math.Round(caloriesBurned,0);
                    Calories = caloriesBurned.ToString();
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


