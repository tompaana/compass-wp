/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Compass
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private AppSettings _appSettings = AppSettings.GetInstance();

        public SettingsPage()
        {
            InitializeComponent();

            LocationUsageToggleSwitch.IsChecked = _appSettings.LocationAllowed;
            
            double accuracy = _appSettings.HeadingAccuracy;
            HeadingAccuracyTextBlock.Text = "The maximum error in the heading accuracy is " + accuracy + " degrees. ";

            if (accuracy <= AppSettings.HeadingAccuracyThreshold)
            {
                // No calibration required
                CalibrateCompassButton.IsEnabled = false;
                HeadingAccuracyTextBlock.Text += "No calibration required.";
            }
            else
            {
                HeadingAccuracyTextBlock.Text += "Calibration is required.";
            }

            RotateMapSwitch.IsChecked = _appSettings.RotateMap;

            if (!App.Properties.HasCompass)
            {
#if (!DEBUG)
                HeadingAccuracyTitleTextBlock.Visibility = Visibility.Collapsed;
                HeadingAccuracyTextBlock.Visibility = Visibility.Collapsed;
                CalibrateCompassButton.Visibility = Visibility.Collapsed;
                RotateMapSwitch.Visibility = Visibility.Collapsed;
#else
                RotateMapSwitch.IsEnabled = false;
#endif
            }
        }

        private void LocationUsageToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            _appSettings.LocationAllowed = true;
        }

        private void LocationUsageToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            _appSettings.LocationAllowed = false;
        }

        /// <summary>
        /// Places a request for calibration and navigates back to the main
        /// page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalibrateCompassButton_Click(object sender, RoutedEventArgs e)
        {
            _appSettings.HeadingAccuracy = AppSettings.CalibrationRequested;
            NavigationService.GoBack();
        }

        private void RotateMapSwitch_Checked(object sender, RoutedEventArgs e)
        {
            _appSettings.RotateMap = true;
        }

        private void RotateMapSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            _appSettings.RotateMap = false;
        }
    }
}