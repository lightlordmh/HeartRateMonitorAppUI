using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Prism.Mvvm;

namespace HeartRateMonitorAppPart2
{   
    //<summary>
    //controls communcation with and data collection from bluetooth heart rate tracking device
    //</summary>
    public class BluetoothControl : BindableBase
    {
        //class object for storing a bluetooth device's information  
        public DeviceInformation? blueToothDevice;
        //class object for 
        protected DeviceWatcher? deviceWatcher;
        //Event for sending heart rate data
        public event EventHandler<BluetoothReadEventArgs> ReadReady;
        //self event firing list of devices found by the watcher
        public ObservableCollection<DeviceInformation> DeviceList { get; private set; } = new ObservableCollection<DeviceInformation>();
        //an action event delegate to bypass problems with threading 
        public Action<Action> SafeInvoke;
        //variable for storing the ID code for the heart rate service
        public string HEART_RATE_SERVICE_ID = "180d";
        //The name used to Identify the device to connect to
        public string trackerName;
        // = "808S 0039730";

        public GattDeviceService? cachedDeviceService;
        public GattCharacteristic? cachedCharacteristic;

        //unknown purpose ask drew
        public bool Connected
        {
            get { return _connected; }
            set
            {
                SafeInvoke?.Invoke(() =>
                {
                    SetProperty(ref _connected, value, nameof(Connected));
                });
            }
        }
        private bool _connected;

        //constructor not in use 
        public BluetoothControl()
        {
        }
        //This initializes, setup, and starts the watcher in a new thread (this is a thread)
        public void InitializeDeviceWatcher()
        {
            //variable storing properties for the initialization of the watcher
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            //Initialize the watcher and adds in its arguments
            deviceWatcher = DeviceInformation.CreateWatcher(
                                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                requestedProperties,
                                DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            // Added, Updated and Removed are required to get all nearby devices
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // Start the watcher.
            deviceWatcher.Start();
        }

        //the main control method for connecting and getting data from device (this is a thread)
        public async Task RunMainControl()
        {

            await SetupDevice();

            if (deviceWatcher == null)
            {
                return;
            }
            else
            {
                deviceWatcher.Stop();
            }

            //while (true)
            //{
            //    Thread.Sleep(200);
            //}
        }

        //Starts the negotiation for communication with the device
        private async Task SetupDevice()
        {
            //loop for when catching when a device selected
            while (true)
            {
                if (blueToothDevice == null)
                {
                    Thread.Sleep(200);
                }
                else
                {
                    //attempt to pair with the device
                    BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(blueToothDevice.Id);
                    Console.WriteLine("Attempting to Pair With device");
                    //get attempt to get the services
                    GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync();
                    //if services found continue else fail
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                       var tmp = await SetupService(result);
                    }
                    else
                    {
                        Console.WriteLine("pairing failed");
                        Console.WriteLine(result.Status);
                    }

                    // Device found, exit
                    break;
                }
            }

        }

        //Determines if the device has a heartrate service
        private async Task<bool> SetupService(GattDeviceServicesResult result)
        {
            Console.WriteLine("Pairing succeeded");
            //bool to store if the service was found
            bool sucessCheck = false;
            var services = result.Services;
            //cyle through all the services available 
            foreach (var service in services)
            {
                //check if the current service is the heartrate service
                if (service.Uuid.ToString("N").Substring(4, 4) == HEART_RATE_SERVICE_ID)
                {
                    cachedDeviceService = service;

                    Console.WriteLine("Found Heart rate service");
                    //attempt to get the characteristics
                    GattCharacteristicsResult characteristicResult = await service.GetCharacteristicsAsync();
                    //if attempt succeeds  continue else do nothing
                    if (characteristicResult.Status == GattCommunicationStatus.Success)
                    {
                        sucessCheck = await SetupCharacteristic(characteristicResult);
                    }
                }
            }
            //fail if service not found
            if (!sucessCheck)
            {
                Console.WriteLine("heart rate service not found error");
            }
            return sucessCheck;
        }

        //Sets up the final step determining if the device has a notification on a characteristic 
        private async Task<bool> SetupCharacteristic(GattCharacteristicsResult characteristicResult)
        {
            var characteristics = characteristicResult.Characteristics;
            bool successCheck = false;
            foreach (var characteristic in characteristics)
            {
                Console.WriteLine("--------------");
                Console.WriteLine(characteristic);
                GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

                if (properties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    Console.WriteLine("Notify Property found");
                    // This characteristic supports subscribing to notifications.
                    GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Success)
                    {
                        successCheck = true;
                        cachedCharacteristic = characteristic;
                        characteristic.ValueChanged += Characteristic_ValueChanged;
                        //var temp = await characteristic.ReadValueAsync();
                        // Server has been informed of clients interest.
                    }
                }
            }
            if (!successCheck)
            {
                Console.WriteLine("Failed to setup characteristic and property");
            }
            return successCheck;
        }

        //Event for catching when the charateristic's value has changed
        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            //bro set this up no idea why or what it does
            Connected = true;
            // An Indicate or Notify reported that the value has changed. store that value in a variable
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            // Parse the flag value out of the first part of the 16bit variable.
            var flags = reader.ReadByte();
            //Parse the HeartRate Data out of the last part of the 16bit variable
            var value = reader.ReadByte();
            //
            //var stuff = $"{flags} - {value}";
            //Console.WriteLine(stuff);
            //Fire an event that initializes and stores the Heart rate data in an event args class
            ReadReady?.Invoke(this, new BluetoothReadEventArgs(value.ToString()));
        }
        //empty optional implemented event handler for the device watcher, fires if the device watcher is stoppped
        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {

        }
        //empty optional implemented event handler for the device watcher, fires if the device watcher has completed its search for devices
        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {

        }
        //empty required implemented event handler for the device watcher fires if the device watcher removes a device from its list
        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // TODO
        }
        //empty required implemented event handler for the device watcher, fires if the device watcher updateds its list of devices
        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {

        }
        //empty required implemented event handler for the device watcher, fires if the device wather  adds a device to its list
        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            //old system for finding the correct device to connect to
            //if (args.Name == trackerName)
            //{
            //    blueToothDevice = args;
            //    Console.WriteLine("Tracker Found!");
            //}
            //if the name of the device is not null or empty
            if (!String.IsNullOrEmpty(args.Name))
            {
                //fire event creates method adds device to devicelist run this on ui thread
                SafeInvoke?.Invoke(() =>
                {
                    DeviceList.Add(args);
                });
            }
            //Console.WriteLine(args.Name);
            //Console.WriteLine(args.Id);
        }

    }
}
