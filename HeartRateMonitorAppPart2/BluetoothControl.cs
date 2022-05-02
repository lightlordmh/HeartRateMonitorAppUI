﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace HeartRateMonitorAppPart2
{
    public class BluetoothControl
    {
        protected static DeviceInformation? blueToothDevice;
        public static string HEART_RATE_SERVICE_ID = "180d";

        public static async Task MainControl()
        {
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            DeviceWatcher deviceWatcher =
                        DeviceInformation.CreateWatcher(
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
            while (true)
            {
                if (blueToothDevice == null)
                {
                    Thread.Sleep(200);
                }
                else
                {
                    Console.WriteLine("Press Any Key to Pair with coospo 808s");
                    Console.ReadKey();
                    BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(blueToothDevice.Id);
                    Console.WriteLine("Attempting to Pair With device");

                    GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync();

                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        Console.WriteLine("Pairing succeeded");
                        var services = result.Services;
                        foreach (var service in services)
                        {
                            if (service.Uuid.ToString("N").Substring(4, 4) == HEART_RATE_SERVICE_ID)
                            {
                                Console.WriteLine("Found Heart rate service");
                                GattCharacteristicsResult characteristicResult = await service.GetCharacteristicsAsync();

                                if (characteristicResult.Status == GattCommunicationStatus.Success)
                                {
                                    var characteristics = characteristicResult.Characteristics;
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
                                                characteristic.ValueChanged += Characteristic_ValueChanged;
                                                // Server has been informed of clients interest.
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine("Press Any Key to Exit Application");
                    Console.ReadKey();
                    break;
                }
            }
            deviceWatcher.Stop();
        }

        private static void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // An Indicate or Notify reported that the value has changed.
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            // Parse the data however required.
            var flags = reader.ReadByte();
            //The Final value I need to send to the graph
            var value = reader.ReadByte();
            Console.WriteLine($"{flags} - {value}");
        }

        private static void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (args.Name == "808S 0039730")
            {
                blueToothDevice = args;
                Console.WriteLine("Tracker Found!");
            }
            //Console.WriteLine(args.Name);
            //Console.WriteLine(args.Id);
            //throw new NotImplementedException();
        }

    }
}