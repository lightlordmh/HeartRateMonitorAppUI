using System;

namespace HeartRateMonitorAppPart2
{
    /// <summary>
    /// this small class is for storing the heart rate data in an event that fires when the data is added(I think bro did this)
    /// </summary>
    public class BluetoothReadEventArgs : EventArgs
    {
        public string Value;

        public BluetoothReadEventArgs(string _value)
        {
            Value = _value;
        }
    }
}
