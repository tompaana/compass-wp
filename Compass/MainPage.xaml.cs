/**
 * Copyright (c) 2012-2013 Nokia Corporation.
 */

using Microsoft.Devices;
using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
using System.Windows.Resources;
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
        private const String DebugTag = "MainPage.";
        private const String CalibrationHintImageUri = "Assets/Graphics/calibration_hint.png";
        private const String CalibrationHintImageDarkUri = "Assets/Graphics/calibration_hint_dark.png";
        private const String ToggleFullscreenModeIconUri = "Assets/Graphics/fullscreen_icon.png";
        private const String CenterToLocationIconUri = "Assets/Graphics/center_icon.png";
        private const String ToggleMapModeIconUri = "Assets/Graphics/map_icon.png";
        private const String LocationMarkerImageUri = "Assets/Graphics/location_marker.png";
        private const String LocationMarkerImageGrayUri = "Assets/Graphics/location_marker_gray.png";
        private const String CalibrationSoundEffectUri1 = "Assets/Sounds/calibration.wav";
        private const String CalibrationSoundEffectUri2 = "Assets/Sounds/calibration_2.wav";
        private const double CompassControlPlateHeightNormal = 400;
        private const double CompassControlPlateHeightFullscreen = 690;
        private const double DegreesToRads = Math.PI / 180;
        private const double CalcConstant = 2 * Math.PI * 6378137;
        private const double CalibrationViewHorizontalMargin = 30;
        private const double CalibrationViewVerticalMargin = 50;
        private const double LocationMarkerImageSize = 40;
        private const int CompassUpdateInterval = 40; // Milliseconds (must be multiple of 20)
        private const int CompassDraggingSpeed = 2;
        private const int DefaultMapZoomLevel = 17;
        private const int LocationUpdateInterval = 10; // Seconds
        private const int ShowCalibrationViewDelay = 3; // Seconds
        private const int CalibrationTimerInterval = 1; // Seconds
        private const int CalibrationVibraDuration = 30; // Milliseconds
        private const int CalibrationSuccessfulVibraDuration = 1000; // Milliseconds

        // Members
        private Microsoft.Devices.Sensors.Compass _compass = null;
        private AppSettings _appSettings = null;
        private GeoCoordinate _coordinate = null;
        private MapLayer _mapLayer = null;
        private MapOverlay _locationMarkerOverlay = null;
        private Ellipse _accuracyCircle = null;
        private Image _locationMarkerImage = null;
        private SoundEffectInstance _calibrationSoundEffect1 = null;
        private SoundEffectInstance _calibrationSoundEffect2 = null;
        private Timer _locationTimer = null;
        private Timer _calibrationTimer = null;
        private double _locationAccuracy = 0;
        private double _previousManipulationDeltaX = 0;
        private double _previousManipulationDeltaY = 0;
        private double _startCompassX = 0;
        private double _startCompassY = 0;
        private double _compassAngle = 0;
        private double _previousCompassX = 0;
        private double _previousCompassY = 0;
        private double _zoomLevel = DefaultMapZoomLevel;
        private bool _wasLaunched = false;
        private bool _compassBeingMoved = false;
        private bool _inFullscreenMode = false;
        private bool _locationFound = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            InitializeAndStartCompass();
            RestoreSettings();
            CreateMapItems();
            ConstructCalibrationView();

#if (DEBUG)
            DebugTextBlock.Visibility = Visibility.Visible;
#endif

            this.Loaded += MainPage_Loaded;

        }

        #region Construction helper methods

        /// <summary>
        /// Checks whether the compass is supported or not, initialises and
        /// starts it.
        /// </summary>
        private void InitializeAndStartCompass()
        {
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
                    Debug.WriteLine(DebugTag + "MainPage(): Failed to set compass update interval: "
                        + e.ToString());
                }

                // Set call backs for events
                _compass.Calibrate +=
                    new EventHandler<CalibrationEventArgs>(OnCalibrate);
                _compass.CurrentValueChanged +=
                    new EventHandler<SensorReadingEventArgs<CompassReading>>(
                        OnCompassReadingChanged);

                try
                {
                    _compass.Start();
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show(AppResources.FailedToStartCompassMessage);
                }
            }
            else
            {
                MessageBox.Show(AppResources.NoCompassMessage);
            }
        }

        /// <summary>
        /// Restore the application settings.
        /// </summary>
        private void RestoreSettings()
        {
            _appSettings = AppSettings.GetInstance();
            _appSettings.LoadSettings();
            _coordinate = _appSettings.LastKnownLocation;
            MyMap.CartographicMode = _appSettings.MapMode;
        }

        /// <summary>
        /// Creates the calibration view components and defines the view 
        /// properties.
        /// </summary>
        private void ConstructCalibrationView()
        {
            CalibrationView.Background = UiHelper.GetThemeBackgroundBrush();
            CalibrationView.Background.Opacity = 0.5;

            Thickness margin = new Thickness();
            margin.Top = CalibrationViewVerticalMargin * 2;
            margin.Left = CalibrationViewHorizontalMargin;
            margin.Right = CalibrationViewHorizontalMargin;

            List<FrameworkElement> elements = new List<FrameworkElement>();

            TextBlock textBlock = new TextBlock();
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.Margin = margin;
            textBlock.FontSize = (double)Application.Current.Resources["PhoneFontSizeMediumLarge"];
            textBlock.FontWeight = FontWeights.ExtraBold;
            textBlock.Text = AppResources.CalibrationViewTitle;
            elements.Add(textBlock);

            margin = new Thickness();
            margin.Left = CalibrationViewHorizontalMargin;
            margin.Right = CalibrationViewHorizontalMargin;

            textBlock = new TextBlock();
            textBlock.Margin = margin;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.FontSize = (double)Application.Current.Resources["PhoneFontSizeMedium"];
            textBlock.Text = AppResources.CalibrationViewInfo;
            elements.Add(textBlock);

            Uri uri = new Uri(UiHelper.PhoneHasDarkTheme ?
                CalibrationHintImageUri : CalibrationHintImageDarkUri, UriKind.Relative);
            StreamResourceInfo resourceInfo = Application.GetResourceStream(uri);
            BitmapImage bmp = new BitmapImage();
            bmp.SetSource(resourceInfo.Stream);
            Image image = new Image();
            image.Source = bmp;
            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.VerticalAlignment = VerticalAlignment.Center;
            image.Margin = margin;
            elements.Add(image);

            margin = new Thickness();
            margin.Top = CalibrationViewVerticalMargin;

            textBlock = new TextBlock();
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.Margin = margin;
            textBlock.FontSize = (double)Application.Current.Resources["PhoneFontSizeMediumLarge"];
            textBlock.FontWeight = FontWeights.ExtraBold;
            textBlock.Text = AppResources.Skip;
            textBlock.ManipulationStarted += OnSkipButtonTapped;
            elements.Add(textBlock);

            int rowIndex = 0;

            foreach (FrameworkElement element in elements)
            {
                Grid.SetRow(element, rowIndex++);
                RowDefinition rd = new RowDefinition();
                CalibrationView.RowDefinitions.Add(rd);
                CalibrationView.Children.Add(element);                
            }

            // Create the sound effects
            try
            {
                StreamResourceInfo stream =
                    Application.GetResourceStream(new Uri(CalibrationSoundEffectUri1, UriKind.Relative));
                SoundEffect soundeffect = SoundEffect.FromStream(stream.Stream);
                _calibrationSoundEffect1 = soundeffect.CreateInstance();
                stream = Application.GetResourceStream(new Uri(CalibrationSoundEffectUri2, UriKind.Relative));
                soundeffect = SoundEffect.FromStream(stream.Stream);
                _calibrationSoundEffect2 = soundeffect.CreateInstance();
            }
            catch (Exception e)
            {
                Debug.WriteLine(DebugTag
                    + "ConstructCalibrationView(): Failed to initialise sound effect: "
                    + e.ToString());
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
                new ApplicationBarIconButton(new Uri(ToggleFullscreenModeIconUri, UriKind.Relative));
            appBarToggleFullscreenButton.Text = AppResources.ToggleFullscreenButtonText;
            appBarToggleFullscreenButton.Click += ToggleFullscreenButton_Click;
            ApplicationBar.Buttons.Add(appBarToggleFullscreenButton);

            ApplicationBarIconButton appBarCenterToLocationButton =
                new ApplicationBarIconButton(new Uri(CenterToLocationIconUri, UriKind.Relative));
            appBarCenterToLocationButton.Text = AppResources.CenterToLocationButtonText;
            appBarCenterToLocationButton.Click += new EventHandler(CenterToLocation_Click);
            ApplicationBar.Buttons.Add(appBarCenterToLocationButton);

            ApplicationBarIconButton appBarToggleMapModeButton =
                new ApplicationBarIconButton(new Uri(ToggleMapModeIconUri, UriKind.Relative));
            appBarToggleMapModeButton.Text = AppResources.ToggleMapModeButtonText;
            appBarToggleMapModeButton.Click += new EventHandler(ToggleMapMode_Click);
            ApplicationBar.Buttons.Add(appBarToggleMapModeButton);

            // Create menu items with the localized string from AppResources
            ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.Settings);
            appBarMenuItem.Click += OnSettingsClicked;
            ApplicationBar.MenuItems.Add(appBarMenuItem);
            appBarMenuItem = new ApplicationBarMenuItem(AppResources.Instructions);
            appBarMenuItem.Click += OnInstructionsClicked;
            ApplicationBar.MenuItems.Add(appBarMenuItem);
            appBarMenuItem = new ApplicationBarMenuItem(AppResources.About);
            appBarMenuItem.Click += OnAboutClicked;
            ApplicationBar.MenuItems.Add(appBarMenuItem);
        }

        /// <summary>
        /// (Re)creates the location marker image and set it to the overlay.
        /// The color of the marker depends on whether the location has been
        /// found or not.
        /// </summary>
        private void CreateAndSetLocationMarkerImage()
        {
            Debug.WriteLine(DebugTag + "CreateAndSetLocationMarkerImage(): " + _locationFound);
            _locationMarkerImage = new Image();
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.UriSource = new Uri(_locationFound ?
                LocationMarkerImageUri : LocationMarkerImageGrayUri, UriKind.Relative);
            _locationMarkerImage.Source = bitmapImage;
            _locationMarkerImage.Width = LocationMarkerImageSize;
            _locationMarkerOverlay.Content = _locationMarkerImage;
        }

        /// <summary>
        /// Creates the map overlay objects.
        /// </summary>
        /// <returns>True, if items successfully created. False, if already
        /// created or no location (last known or otherwise) available.</returns>
        private bool CreateMapItems()
        {
            if (_mapLayer != null || _coordinate == null)
            {
                Debug.WriteLine(DebugTag + "CreateMapItems(): Already created or no coordinate!");
                return false;
            }

            System.Windows.Point positionOrigin = new System.Windows.Point(0.5, 0.5);

            _accuracyCircle = UiHelper.CreateFilledCircle(LocationMarkerImageSize * 4, 75, 200, 0, 0);

            MapOverlay accuracyCircleOverlay = new MapOverlay();
            accuracyCircleOverlay.Content = _accuracyCircle;
            accuracyCircleOverlay.PositionOrigin = positionOrigin;

            _locationMarkerOverlay = new MapOverlay();
            CreateAndSetLocationMarkerImage();
            _locationMarkerOverlay.PositionOrigin = positionOrigin;

            // Add the items to the map
            _mapLayer = new MapLayer();
            _mapLayer.Add(accuracyCircleOverlay);
            _mapLayer.Add(_locationMarkerOverlay);

            UpdateMapItems();

            MyMap.Layers.Add(_mapLayer);

            if (!_locationFound && _coordinate != null)
            {
                Debug.WriteLine(DebugTag + "CreateMapItems(): Using the last known location.");
                MyMap.SetView(_coordinate, DefaultMapZoomLevel, MapAnimationKind.Parabolic);
            }

            return true;
        }

        #endregion // Construction helper methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(DebugTag + "MainPage_Loaded()");

            if (_wasLaunched && !_appSettings.LocationAllowed)
            {
                LocationUsageQueryDialog.LocationUsageAllowed += OnLocationUsageAllowed;
                LocationUsageQueryDialog.Show();
            }

            BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine(DebugTag + "OnNavigatedTo(): " + e.IsNavigationInitiator);

            _wasLaunched = !e.IsNavigationInitiator;

            if (_appSettings.LocationAllowed)
            {
                _locationTimer = new Timer(GetCurrentLocation, null,
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(LocationUpdateInterval));
            }
            else
            {
            }

            if (_appSettings.HeadingAccuracy == AppSettings.CalibrationRequested)
            {
                _calibrationTimer = new Timer(OnCalibrationTimerTimeout, null,
                    TimeSpan.FromSeconds(CalibrationTimerInterval),
                    TimeSpan.FromSeconds(CalibrationTimerInterval));
            }

            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Saves the app settings.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_locationTimer != null)
            {
                _locationTimer.Dispose();
                _locationTimer = null;
            }

            if (_calibrationTimer != null)
            {
                _calibrationTimer.Dispose();
                _calibrationTimer = null;
            }

            _appSettings.SaveSettings();
            base.OnNavigatedFrom(e);
        }

        #region Compass sensor event handling

        /// <summary>
        /// Hides the application bar and shows the calibration view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCalibrate(object sender, CalibrationEventArgs e)
        {
            Debug.WriteLine(DebugTag + "OnCalibrate(): Accuracy: "
                + ((_compass == null) ? "n/a" : _compass.CurrentValue.HeadingAccuracy.ToString()));

            if (_calibrationTimer == null)
            {
                _calibrationTimer = new Timer(OnCalibrationTimerTimeout, null,
                    TimeSpan.FromSeconds(ShowCalibrationViewDelay),
                    TimeSpan.FromSeconds(CalibrationTimerInterval));
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
            Dispatcher.BeginInvoke(() =>
            {
                if (CalibrationView.Visibility == Visibility.Visible
                    && e.SensorReading.HeadingAccuracy <= AppSettings.HeadingAccuracyThreshold)
                {
                    // Acceptable accuracy reached
                    OnCalibrated();
                }

                CompassControl.CompassReading = e.SensorReading.TrueHeading;
            });

#if (DEBUG)
            Dispatcher.BeginInvoke(()
                => DebugTextBlock.Text =
                    "\nTrue: " + e.SensorReading.TrueHeading
                    + "\nMagnetic: " + e.SensorReading.MagneticHeading
                    + "\nV3: [" + e.SensorReading.MagnetometerReading.X + ", "
                    + e.SensorReading.MagnetometerReading.Y + ", "
                    + e.SensorReading.MagnetometerReading.Z + "]"
                    + "\nAccuracy: " + e.SensorReading.HeadingAccuracy
            );
#endif
        }

        /// <summary>
        /// Stops the calibration timer, shows the application bar and hides
        /// the calibration view.
        /// </summary>
        private void OnCalibrated()
        {
            if (_calibrationTimer != null)
            {
                _calibrationTimer.Dispose();
                _calibrationTimer = null;
            }

            Dispatcher.BeginInvoke(() =>
            {
                ApplicationBar.IsVisible = true;
                CalibrationView.Visibility = Visibility.Collapsed;
            });

            VibrateController vibrateController = VibrateController.Default;
            vibrateController.Start(TimeSpan.FromMilliseconds(CalibrationSuccessfulVibraDuration));

            if (_calibrationSoundEffect2 != null)
            {
                FrameworkDispatcher.Update();
                _calibrationSoundEffect2.Play();
            }
        }

        #endregion // Compass sensor event handling

        #region Touch event handling

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            double x = e.ManipulationOrigin.X;
            double y = e.ManipulationOrigin.Y;

            Compass.Ui.CompassControl.CompassControlArea area =
                CompassControl.ManipulatedArea;
            Debug.WriteLine(DebugTag + "OnManipulationStarted(): Area at ["
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

                _previousManipulationDeltaX = -1;
                _previousManipulationDeltaY = -1;

                _compassBeingMoved = true;
                e.Handled = true;
            }
            else
            {
                base.OnManipulationStarted(e);
            }
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            _compassBeingMoved = false;
            e.Handled = true;
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (_compassBeingMoved)
            {
                double x = e.ManipulationOrigin.X;
                double y = e.ManipulationOrigin.Y;

                double diffX = 0;
                double diffY = 0;

                if (_previousManipulationDeltaX == -1 && _previousManipulationDeltaY == -1)
                {
                    ManipulationDelta delta = e.DeltaManipulation;
                    _previousManipulationDeltaX = x - delta.Scale.X;
                    _previousManipulationDeltaY = y - delta.Scale.Y;
                }

                diffX = (_previousManipulationDeltaX - x) * CompassDraggingSpeed;
                diffY = (_previousManipulationDeltaY - y) * CompassDraggingSpeed;
                _previousManipulationDeltaX = x;
                _previousManipulationDeltaY = y;

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

        #endregion // Touch event handling

        #region Location and map handling

        /// <summary>
        /// Tries to get the current location of the user. When the location is
        /// received, will make a request to update the map and map items
        /// accordingly.
        /// </summary>
        private async void GetCurrentLocation(Object state)
        {
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;

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

                    if (!_locationFound)
                    {
                        _locationFound = true;

                        if (!CreateMapItems())
                        {
                            // Map items already created. Update the location
                            // marker (to red to indicate that the location is
                            // up-to-date.
                            CreateAndSetLocationMarkerImage();
                        }

                        MyMap.SetView(_coordinate, DefaultMapZoomLevel, MapAnimationKind.Parabolic);
                    }
                    else
                    {
                        UpdateMapItems();
                    }

                    // Store this location
                    AppSettings.GetInstance().LastKnownLocation = _coordinate;
                });
            }
            catch (Exception)
            {
                // Failed get the current location. Location might be disabled
                // in settings
                Debug.WriteLine(DebugTag + "GetCurrentLocation(): Failed to get current location!");
            }
        }

        /// <summary>
        /// Updates the accuracy circle and the location marker position based
        /// on the current geocoordinate and its accuracy.
        /// </summary>
        private void UpdateMapItems()
        {
            // The ground resolution (in meters per pixel) varies depending on the level of detail
            // and the latitude at which it’s measured. It can be calculated as follows:
            double metersPerPixels =
                (Math.Cos(_coordinate.Latitude * DegreesToRads) * CalcConstant)
                / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = _locationAccuracy / metersPerPixels;

            _accuracyCircle.Width = radius * 2;
            _accuracyCircle.Height = radius * 2;

            foreach (MapOverlay overlay in _mapLayer)
            {
                overlay.GeoCoordinate = _coordinate;
            }

            MyMap.InvalidateMeasure();
        }

        /// <summary>
        /// Updates the map markers and stores the zoom level.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapZoomLevelChanged(object sender, EventArgs e)
        {
            _zoomLevel = MyMap.ZoomLevel;

            if (_coordinate != null)
            {
                UpdateMapItems();
            }
        }

        #endregion // Location and map handling

        /// <summary>
        /// Called when the use of location is allowed. Stores the setting and
        /// will start retrieving the user's location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLocationUsageAllowed(object sender, EventArgs e)
        {
            LocationUsageQueryDialog.LocationUsageAllowed -= OnLocationUsageAllowed;
            _appSettings.LocationAllowed = true;

            _locationTimer = new Timer(GetCurrentLocation, null,
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(LocationUpdateInterval));
        }

        /// <summary>
        /// Provides physical and audible feedback when calibration the compass
        /// sensor.
        /// </summary>
        /// <param name="state"></param>
        private void OnCalibrationTimerTimeout(object state)
        {
            Debug.WriteLine(DebugTag + "OnCalibrationTimerTimeout()");

            Dispatcher.BeginInvoke(() =>
            {
                if (CalibrationView.Visibility == Visibility.Collapsed)
                {
                    if (LocationUsageQueryDialog.Visibility == Visibility.Visible)
                    {
                        // Wait until the dialog is hidden
                        Debug.WriteLine(DebugTag + "OnCalibrationTimerTimeout(): Waiting for dialog to be hidden");
                        return;
                    }

                    // Show the calibration view
                    if (ApplicationBar != null)
                    {
                        ApplicationBar.IsVisible = false;
                    }

                    CalibrationView.Visibility = Visibility.Visible;

                    _calibrationTimer.Dispose();
                    _calibrationTimer = new Timer(OnCalibrationTimerTimeout, null,
                        TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(CalibrationTimerInterval));

                    if (_calibrationSoundEffect1 != null)
                    {
                        FrameworkDispatcher.Update();
                        _calibrationSoundEffect1.Play();
                    }
                }
                else
                {
                    // Play vibra
                    VibrateController vibrateController = VibrateController.Default;
                    vibrateController.Start(TimeSpan.FromMilliseconds(CalibrationVibraDuration));
                }
            });
        }

        #region Button tap handlers

        /// <summary>
        /// Hides the calibration view and shows the application bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSkipButtonTapped(object sender, ManipulationStartedEventArgs e)
        {
            OnCalibrated();
        }

        /// <summary>
        /// Toggles the fullscreen compass mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleFullscreenButton_Click(object sender, EventArgs e)
        {
            Debug.WriteLine(DebugTag + "ToggleFullscreenButton_Click(): "
                + _inFullscreenMode + " -> " + !_inFullscreenMode);

            Thickness margin = new Thickness();

            if (_inFullscreenMode)
            {
                // To normal mode
                margin.Left = _previousCompassX;
                margin.Top = _previousCompassY;

                CompassControl.PlateHeight = CompassControlPlateHeightNormal;
                CompassControl.PlateAngle = _compassAngle;
                FullscreenBackground.Visibility = Visibility.Collapsed;
            }
            else
            {
                // To fullscreen mode
                _compassAngle = CompassControl.PlateAngle;
                _previousCompassX = CompassControl.Margin.Left;
                _previousCompassY = CompassControl.Margin.Top;

                margin.Top = -60;
                margin.Left = 20;

                CompassControl.PlateHeight = CompassControlPlateHeightFullscreen;
                CompassControl.PlateAngle = 0;
                FullscreenBackground.Visibility = Visibility.Visible;
            }

            CompassControl.Margin = margin;
            CompassControl.Width = CompassControl.PlateWidth;
            CompassControl.Height = CompassControl.PlateHeight;
            CompassControl.ManipulationEnabled = !CompassControl.ManipulationEnabled;
            _inFullscreenMode = !_inFullscreenMode;
        }

        /// <summary>
        /// Centers the map to the latest user location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CenterToLocation_Click(object sender, EventArgs e)
        {
            if (_coordinate == null)
            {
                if (_appSettings.LocationAllowed)
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
        /// Toggles the map modes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleMapMode_Click(object sender, EventArgs e)
        {
            switch (_appSettings.MapMode)
            {
                case MapCartographicMode.Road:
                    _appSettings.MapMode = MapCartographicMode.Aerial;
                    MyMap.CartographicMode = _appSettings.MapMode;
                    break;
                case MapCartographicMode.Aerial:
                    _appSettings.MapMode = MapCartographicMode.Hybrid;
                    MyMap.CartographicMode = _appSettings.MapMode;
                    break;
                case MapCartographicMode.Hybrid:
                    _appSettings.MapMode = MapCartographicMode.Terrain;
                    MyMap.CartographicMode = _appSettings.MapMode;
                    break;
                default:
                    _appSettings.MapMode = MapCartographicMode.Road;
                    MyMap.CartographicMode = _appSettings.MapMode;
                    break;
            }
        }

        void OnSettingsClicked(object sender, EventArgs e)
        {
            double defaultAccuracyValueInCaseNoCompass = 0;
#if (DEBUG)
            defaultAccuracyValueInCaseNoCompass = 42;
#endif
            _appSettings.HeadingAccuracy = (_compass != null) ?
                _compass.CurrentValue.HeadingAccuracy : defaultAccuracyValueInCaseNoCompass;
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        void OnInstructionsClicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void OnAboutClicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion // Button tap handlers
    }
}