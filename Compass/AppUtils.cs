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
    class AppUtils
    {
        // Constants
        private const String Tag = "Compass.AppUtils";
        private const String LocationAllowedId = "LocationAllowed";
        private const String LastKnownLocationId = "Location";
        private const String MapModeId = "MapMode";

        // Members
        private static AppUtils _instance = null;
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

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <returns>A singleton instance of AppUtils class.</returns>
        public static AppUtils GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AppUtils();
            }

            return _instance;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private AppUtils()
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
            }
            catch (System.Collections.Generic.KeyNotFoundException e)
            {
                Debug.WriteLine(Tag + ".LoadSettings(): " + e.ToString());
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

                if (LastKnownLocation != null)
                {
                    _appSettings[LastKnownLocationId] = LastKnownLocation;
                }

                _appSettings[MapModeId] = MapMode;
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine(Tag + ".SaveSettings(): " + e.ToString());
            }
        }
    }
}
