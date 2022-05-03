using System;

namespace HeartRateMonitorAppPart2
{
    public class BluetoothReadEventArgs : EventArgs
    {
        public string Value;

        public BluetoothReadEventArgs(string _value)
        {
            Value = _value;
        }
    }
}
