/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compass
{
    /// <summary>
    /// Manages loading and saving of the application data. Provides also
    /// general helper methods including ones that offer details of the device
    /// the app is run on.
    /// </summary>
    class AppSettings
    {
        // Constants
        public const double CalibrationRequested = -1.0;
        public const int HeadingAccuracyThreshold = 10; // Degrees
        private const string Tag = "AppUtils.";
        private const string LocationAllowedId = "LocationAllowed";
        private const string LastKnownLocationId = "Location";
        private const string MapModeId = "MapMode";
        private const string AutoNorthId = "AutoNorth";
        private const string RotateMapId = "RotateMap";

        // Members
        private static AppSettings _instance = null;
        private IsolatedStorageSettings _appSettings = IsolatedStorageSettings.ApplicationSettings;

        // Properties

        public bool LocationAllowed
        {
            get;
            set;
        }

        public GeoCoordinate LastKnownLocation
        {
            get;
            set;
        }

        public MapCartographicMode MapMode
        {
            get;
            set;
        }

        public double HeadingAccuracy
        {
            get;
            set;
        }

        public bool AutoNorth
        {
            get;
            set;
        }

        public bool RotateMap
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <returns>A singleton instance of AppUtils class.</returns>
        public static AppSettings GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AppSettings();
            }

            return _instance;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private AppSettings()
        {
            LastKnownLocation = null;
            MapMode = MapCartographicMode.Road;
        }

        /// <summary>
        /// Loads application data.
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                LocationAllowed = (bool)_appSettings[LocationAllowedId];
                LastKnownLocation = (GeoCoordinate)_appSettings[LastKnownLocationId];
                MapMode = (MapCartographicMode)_appSettings[MapModeId];
                AutoNorth = (bool)_appSettings[AutoNorthId];
                RotateMap = (bool)_appSettings[RotateMapId];

                Debug.WriteLine(Tag + "LoadSettings():\n" + ToString());
            }
            catch (System.Collections.Generic.KeyNotFoundException e)
            {
                Debug.WriteLine(Tag + "LoadSettings(): " + e.ToString());
            }
        }

        /// <summary>
        /// Saves application data.
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                _appSettings[LocationAllowedId] = LocationAllowed;
                _appSettings[LastKnownLocationId] = LastKnownLocation;
                _appSettings[MapModeId] = MapMode;
                _appSettings[AutoNorthId] = AutoNorth;
                _appSettings[RotateMapId] = RotateMap;

                Debug.WriteLine(Tag + "SaveSettings():\n" + ToString());
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine(Tag + "SaveSettings(): " + e.ToString());
            }
        }

        public override string ToString()
        {
            return "- Location allowed: " + LocationAllowed + ", "
                    + "\n- Last known location: " + LastKnownLocation + ", "
                    + "\n- Map mode: " + MapMode + ", "
                    + "\n- Auto north: " + AutoNorth + ", "
                    + "\n- Rotate map: " + RotateMap;
        }
    }
}
