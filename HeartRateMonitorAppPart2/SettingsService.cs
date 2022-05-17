using Jot;
using System;
using System.Windows;

namespace HeartRateMonitorAppPart2
{
    public class SettingsService
    {
        public Tracker Tracker;

        public SettingsService()
        {
            Tracker = new Tracker();
        }
    }
}
