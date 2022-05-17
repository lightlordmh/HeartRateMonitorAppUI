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
using System.Text.RegularExpressions;

namespace HeartRateMonitorAppPart2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel 
        { 
            get => (MainWindowViewModel) DataContext; 
            set => DataContext = value; 
        }

        private SettingsService SettingsService { get; set; }

        public MainWindow(MainWindowViewModel _viewModel, SettingsService _settingsService)
        {
            InitializeComponent();
            
            SettingsService = _settingsService;

            SettingsService.Tracker.Configure<Window>()
                    .Id(w => w.Name, SystemParameters.VirtualScreenHeight / SystemParameters.VirtualScreenWidth)
                    .Properties(w => new { w.Height, w.Width, w.Left, w.Top, w.WindowState })
                    .PersistOn(nameof(Window.LocationChanged), nameof(Window.SizeChanged), nameof(Window.Closing))
                    .WhenPersistingProperty((f, p) =>
                    {
                        var propNameIsWindowSizeOrPosition = p.Property == nameof(Window.Height) || p.Property == nameof(Window.Width) || p.Property == nameof(Window.Top) || p.Property == nameof(Window.Left);
                        p.Cancel = f.WindowState != WindowState.Normal && propNameIsWindowSizeOrPosition;
                    })
                    .StopTrackingOn(nameof(Window.Closing));

            SettingsService.Tracker.Track(this);

            SizeToFit();
            MoveIntoView();

            try
            {
                ViewModel = _viewModel;
                ViewModel.Initialize(Devices.Dispatcher);
            }
            catch (Exception)
            {
                SystemCommands.CloseWindow(this);
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        protected void SizeToFit()
        {
            if (Height > SystemParameters.VirtualScreenHeight)
                Height = SystemParameters.VirtualScreenHeight;
            if (Width > SystemParameters.VirtualScreenWidth)
                Width = SystemParameters.VirtualScreenWidth;
        }
        protected void MoveIntoView()
        {
            if (Top + (Height / 2) > (SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop))
                Top = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop - Height;
            if (Left + (Width / 2) > (SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft))
                Left = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenLeft - Width;
            if (Top < SystemParameters.VirtualScreenTop)
                Top = SystemParameters.VirtualScreenTop;
            if (Left < SystemParameters.VirtualScreenLeft)
                Left = SystemParameters.VirtualScreenLeft;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SettingsService.Tracker.PersistAll();
        }
    }
}
