/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace Compass
{
    class UiHelper
    {
        // Constants
        private const String CalibrationHintImageUri = "Assets/Graphics/calibration_hint.png";
        private const String CalibrationHintImageDarkUri = "Assets/Graphics/calibration_hint_dark.png";

        // Members
        private static UiHelper _instance = new UiHelper();
        private SolidColorBrush _brush = null;

        // Properties

        /// <summary>
        /// Provides information on theme background.
        /// </summary>
        private static bool _hasDarkTheme;
        public static bool PhoneHasDarkTheme
        {
            get
            {
                return _hasDarkTheme;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private UiHelper()
        {
            ResolveUiProperties();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        public static void ConstructCalibrationView(Grid container)
        {
            int rowIndex = 0;

            Uri uri = new Uri(_hasDarkTheme ? CalibrationHintImageUri : CalibrationHintImageDarkUri, UriKind.Relative);
            StreamResourceInfo resourceInfo = Application.GetResourceStream(uri);
            BitmapImage bmp = new BitmapImage();
            bmp.SetSource(resourceInfo.Stream);
            Image image = new Image();
            image.Source = bmp;
            Grid.SetRow(image, rowIndex++);

            container.Children.Add(image);
        }

        /// <returns>A solid color brush instance with color matching the theme
        /// background.</returns>
        public static SolidColorBrush ThemeBackgroundBrush()
        {
            if (_instance._brush == null)
            {
                Color color = new Color();

                if (_hasDarkTheme)
                {
                    color.A = 255;
                    color.R = 0;
                    color.G = 0;
                    color.B = 0;
                }
                else
                {
                    color.A = 255;
                    color.R = 255;
                    color.G = 255;
                    color.B = 255;
                }

                _instance._brush = new SolidColorBrush(color);
            }

            return _instance._brush;
        }

        /// <summary>
        /// Resolves the UI properties so that they are ready to be quieried.
        /// </summary>
        private void ResolveUiProperties()
        {
            // Resolve the theme background
            Visibility darkBackgroundVisibility =
                (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            _hasDarkTheme = (darkBackgroundVisibility == Visibility.Visible);
        }
    }
}
