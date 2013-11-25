/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

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
        public static SolidColorBrush GetThemeBackgroundBrush()
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
        /// Creates a circle with the given radius filled with the given color.
        /// </summary>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="a">Color alpha level.</param>
        /// <param name="r">Red.</param>
        /// <param name="g">Green.</param>
        /// <param name="b">Blue.</param>
        /// <returns>The created circle as an ellipse object.</returns>
        public static Ellipse CreateFilledCircle(double radius, byte a, byte r, byte g, byte b)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b));
            return ellipse;
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
