/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Windows;
using System.Windows.Media;

namespace Compass
{
    class UiHelper
    {
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
