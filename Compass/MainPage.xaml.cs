/**
 * Copyright (c) 2012-2013 Nokia Corporation.
 */

using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;

using Compass.Resources;

namespace Compass
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        // Constants
        private const String ToggleFullscreenModeIcon = "/Assets/Graphics/fullscreen_icon.png";
        private const String CenterToLocationIcon = "/Assets/Graphics/center_icon.png";
        private const String ToggleMapModeIcon = "/Assets/Graphics/map_icon.png";
        private const int CompassUpdateInterval = 40; // Milliseconds (must be multiple of 20)
        private const int CompassDraggingSpeed = 2;
        private const int DefaultMapZoomLevel = 17;
        private const int LocationUpdateInterval = 10; // Seconds

        // Members
        private Microsoft.Devices.Sensors.Compass _compass = null;
        private GeoCoordinate _coordinate = null;
        private MapLayer _mapLayer = null;
        private Image _hereMarkerImage = null;
        private Timer _timer = null;
        private double _locationAccuracy = 0;
        private double _previousX = 0;
        private double _previousY = 0;
        private double _startCompassX = 0;
        private double _startCompassY = 0;
        private double _compassAngle = 0;
        private double _zoomLevel = DefaultMapZoomLevel;
        private bool _compassBeingMoved = false;
        private bool _inFullscreenMode = false;
        private bool _isLocationAllowed = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            if (Microsoft.Devices.Sensors.Compass.IsSupported)
            {
                // Setup and start the compass
                _compass = new Microsoft.Devices.Sensors.Compass();

                try
                {
                    _compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(CompassUpdateInterval);
                }
                catch (InvalidOperationException e)
                {
                    Debug.WriteLine("MainPage::MainPage(): Failed to set compass update interval: "
                        + e.ToString());
                }

                // Set call backs for events
                /*_compass.Calibrate +=
                    new EventHandler<CalibrationEventArgs>(OnCalibrate);*/
                _compass.CurrentValueChanged +=
                    new EventHandler<SensorReadingEventArgs<CompassReading>>(
                        OnCompassReadingChanged);

                try
                {
                    _compass.Start();
                }
                catch (InvalidOperationException)
                {
                    // TODO: Show an error message
                }
            }
            else
            {
                // TODO: Show an error message
            }
        }

        /// <summary>
        /// Sets the page's ApplicationBar to a new instance of ApplicationBar.
        /// </summary>
        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.4;

            // Create a new button and set the text value to the localized
            // string from AppResources.
            ApplicationBarIconButton appBarToggleFullscreenButton =
                new ApplicationBarIconButton(new Uri(ToggleFullscreenModeIcon, UriKind.Relative));
            appBarToggleFullscreenButton.Text = AppResources.ToggleFullscreenButtonText;
            appBarToggleFullscreenButton.Click += ToggleFullscreenButton_Click;
            ApplicationBar.Buttons.Add(appBarToggleFullscreenButton);

            ApplicationBarIconButton appBarCenterToLocationButton =
                new ApplicationBarIconButton(new Uri(CenterToLocationIcon, UriKind.Relative));
            appBarCenterToLocationButton.Text = AppResources.CenterToLocationButtonText;
            appBarCenterToLocationButton.Click += new EventHandler(CenterToLocation_Click);
            ApplicationBar.Buttons.Add(appBarCenterToLocationButton);

            ApplicationBarIconButton appBarToggleMapModeButton =
                new ApplicationBarIconButton(new Uri(ToggleMapModeIcon, UriKind.Relative));
            appBarToggleMapModeButton.Text = AppResources.ToggleMapModeButtonText;
            appBarToggleMapModeButton.Click += new EventHandler(ToggleMapMode_Click);
            ApplicationBar.Buttons.Add(appBarToggleMapModeButton);

            // Create menu items with the localized string from AppResources
            ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.Instructions);
            ApplicationBar.MenuItems.Add(appBarMenuItem);
            appBarMenuItem = new ApplicationBarMenuItem(AppResources.About);
            ApplicationBar.MenuItems.Add(appBarMenuItem);
        }

        /// <summary>
        /// Event handler for location usage permission at startup.
        /// </summary>
        private void LocationUsage_Click(object sender, EventArgs e)
        {
            LocationPanel.Visibility = Visibility.Collapsed;
            BuildLocalizedApplicationBar();

            if (sender == AllowButton)
            {
                _isLocationAllowed = true;

                _timer = new Timer(GetCurrentLocation, null,
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(LocationUpdateInterval));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private async void GetCurrentLocation(Object state)
        {
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;
            bool firstTime = (_coordinate == null) ? true : false;

            try
            {
                Geoposition currentPosition =
                    await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1),
                                                         TimeSpan.FromSeconds(10));
                _locationAccuracy = currentPosition.Coordinate.Accuracy;
           
                Dispatcher.BeginInvoke(() =>
                {
                    _coordinate =
                        new GeoCoordinate(currentPosition.Coordinate.Latitude,
                                          currentPosition.Coordinate.Longitude);

                    if (firstTime)
                    {
                        CreateMapItems();
                        MyMap.SetView(_coordinate, DefaultMapZoomLevel, MapAnimationKind.Parabolic);
                    }
                    else
                    {
                        UpdateMapItems();
                    }
                });
            }
            catch (Exception)
            {
                // Failed get the current location. Location might be disabled
                // in settings
                Debug.WriteLine("MainPage::GetCurrentLocation(): Failed to get current location!");
            }
        }

        /// <summary>
        /// Helper method to draw a single marker on top of the map.
        /// </summary>
        /// <param name="coordinate">GeoCoordinate of the marker</param>
        /// <param name="mapLayer">Map layer to add the marker</param>
        private void CreateMapItems()
        {
            if (_mapLayer != null || _coordinate == null)
            {
                // Already created or the user has not been located yet!
                return;
            }

            // Load the marker image
            _hereMarkerImage = new Image();
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.UriSource = new Uri("/Assets/Graphics/here-icon.png", UriKind.Relative);
            _hereMarkerImage.Source = bitmapImage;

            // Add the items to the map
            _mapLayer = new MapLayer();
            UpdateMapItems();
            MyMap.Layers.Add(_mapLayer);
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateMapItems()
        {
            _mapLayer.Clear();

            // The ground resolution (in meters per pixel) varies depending on the level of detail
            // and the latitude at which it’s measured. It can be calculated as follows:
            double metersPerPixels =
                (Math.Cos(_coordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137)
                / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = _locationAccuracy / metersPerPixels;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(75, 200, 0, 0));

            MapOverlay overlay = new MapOverlay();
            overlay.Content = ellipse;
            overlay.GeoCoordinate = new GeoCoordinate(_coordinate.Latitude, _coordinate.Longitude);
            overlay.PositionOrigin = new Point(0.5, 0.5);
            _mapLayer.Add(overlay);

            // Create a MapOverLay with the marker image and add it to the map layer
            overlay = new MapOverlay();
            overlay.Content = _hereMarkerImage;
            overlay.GeoCoordinate = new GeoCoordinate(_coordinate.Latitude, _coordinate.Longitude);
            overlay.PositionOrigin = new Point(0.5, 0.5);
            _mapLayer.Add(overlay);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            double x = e.ManipulationOrigin.X;
            double y = e.ManipulationOrigin.Y;

            Compass.Ui.CompassControl.CompassControlArea area =
                CompassControl.ManipulatedArea;
            Debug.WriteLine("MainPage::OnManipulationStarted(): Area at ["
                + x + ", " + y + "] == " + area);

            if (area == Compass.Ui.CompassControl.CompassControlArea.PlateCenter
                || area == Compass.Ui.CompassControl.CompassControlArea.PlateBottom)
            {
                // The touched area is on the compass

                // This page has to manage the manipulation of the compass
                // position because even when the compass has been rotated, the
                // manipulation coordinates are not.
                e.ManipulationContainer = this;                

                Thickness margin = CompassControl.Margin;
                _startCompassX = margin.Left;
                _startCompassY = margin.Top;

                _previousX = -1;
                _previousY = -1;

                _compassBeingMoved = true;
                e.Handled = true;
            }
            else
            {
                base.OnManipulationStarted(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            _compassBeingMoved = false;
            e.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (_compassBeingMoved)
            {
                double x = e.ManipulationOrigin.X;
                double y = e.ManipulationOrigin.Y;

                double diffX = 0;
                double diffY = 0;

                if (_previousX == -1 && _previousY == -1)
                {
                    ManipulationDelta delta = e.DeltaManipulation;
                    _previousX = x - delta.Scale.X;
                    _previousY = y - delta.Scale.Y;
                }

                diffX = (_previousX - x) * CompassDraggingSpeed;
                diffY = (_previousY - y) * CompassDraggingSpeed;
                _previousX = x;
                _previousY = y;
                
                Thickness margin = CompassControl.Margin;
                margin.Top -= diffY;
                margin.Left -= diffX;                
                CompassControl.Margin = margin;

                e.Handled = true;
            }
            else
            {
                base.OnManipulationDelta(e);
            }
        }

        /// <summary>
        /// Updates the needle angle of the compass control. This is a call
        /// back for the compass sensor, and gets called everytime the reading
        /// value is changed. The interval is defined by CompassUpdateInterval
        /// constant.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">An object containing the compass readings.</param>
        private void OnCompassReadingChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            Dispatcher.BeginInvoke(()
                => CompassControl.CompassReading = e.SensorReading.TrueHeading);

            Dispatcher.BeginInvoke(()
                => DebugTextBlock.Text =
                    "\nTrue: " + e.SensorReading.TrueHeading
                    + "\nMagnetic: " + e.SensorReading.MagneticHeading
                    + "\nV3: [" + e.SensorReading.MagnetometerReading.X + ", "
                    + e.SensorReading.MagnetometerReading.Y + ", "
                    + e.SensorReading.MagnetometerReading.Z + "]"
                    + "\nAccuracy: " + e.SensorReading.HeadingAccuracy
            );
        }

        /// <summary>
        /// Updates the map markers and stores the zoom level.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapZoomLevelChanged(object sender, EventArgs e)
        {
            _zoomLevel = MyMap.ZoomLevel;
        }

        /// <summary>
        /// Toggles the fullscreen compass mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleFullscreenButton_Click(object sender, EventArgs e)
        {
            if (_inFullscreenMode)
            {
                CompassControl.Height = 600; // Temporary
                CompassControl.PlateAngle = _compassAngle;
                CompassControl.ManipulationEnabled = true;
                _inFullscreenMode = false;
            }
            else
            {
                CompassControl.Height = 1200;
                _compassAngle = CompassControl.PlateAngle;
                CompassControl.PlateAngle = 0;
                CompassControl.ManipulationEnabled = false;
                _inFullscreenMode = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CenterToLocation_Click(object sender, EventArgs e)
        {
            if (_coordinate == null)
            {
                if (_isLocationAllowed)
                {
                    GetCurrentLocation(null);
                }
            }
            else
            {
                MyMap.SetView(_coordinate, _zoomLevel, MapAnimationKind.Linear);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleMapMode_Click(object sender, EventArgs e)
        {
            switch (MyMap.CartographicMode)
            {
                case MapCartographicMode.Road:
                    MyMap.CartographicMode = MapCartographicMode.Aerial;
                    break;
                case MapCartographicMode.Aerial:
                    MyMap.CartographicMode = MapCartographicMode.Hybrid;
                    break;
                case MapCartographicMode.Hybrid:
                    MyMap.CartographicMode = MapCartographicMode.Terrain;
                    break;
                default:
                    MyMap.CartographicMode = MapCartographicMode.Road;
                    break;
            }
        }
    }
}