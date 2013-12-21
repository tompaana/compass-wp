/**
 * Copyright (c) 2012-2013 Nokia Corporation.
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
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;

namespace Compass
{
    /// <summary>
    /// 
    /// </summary>
    public partial class CompassControl : UserControl
    {
        // Constants
        public static readonly double DefaultPlateWidth = 169;
        public static readonly double DefaultPlateHeight = 350;
        private const String DebugTag = "CompassControl.";
        private const double RadiansToDegreesCoefficient = 57.2957795; // 180 / PI
        private const double ScaleRelativeTopMargin = 0.16;
        private const double PlateManipulationRelativeTopMargin = 0.15;
        private const int PlateNativeWidth = 870;
        private const int PlateNativeHeight = 1800;
        private const int ScaleNativeWidth = 828;
        private const int ScaleNativeHeight = 828;
        private const int NeedleNativeWidth = 63;

        // Data types

        public enum CompassControlArea
        {
            None = 0,
            PlateTop = 1,
            PlateCenter = 2,
            Scale = 3,
            PlateBottom = 4
        };

        // Members
        private TranslateTransform _scalePosition = null;
        private double _plateCenterX = 0;
        private double _plateCenterY = 0;
        private double _previousX = 0;
        private double _previousY = 0;
        private double _adjustedAngle = 0;
        private double _plateTopEndY = 0;
        private double _plateBottomStartY = 0;

        // Properties

        private double _compassReading = 0;
        public double CompassReading
        {
            get
            {
                return _compassReading;
            }
            set
            {
                _compassReading = value;
                UpdateNeedleAngle();
            }
        }

        /// <summary>
        /// The area of the compass control being manipulated.
        /// </summary>
        public CompassControlArea ManipulatedArea
        {
            get;
            private set;
        }

        public static readonly DependencyProperty PlateWidthProperty =
            DependencyProperty.Register("PlateWidth", typeof(double), typeof(CompassControl), null);

        /// <summary>
        /// The width of the compass control (plate).
        /// Note that this property cannot be used to change the width. Use
        /// PlateHeight instead to manipulate the size of the plate.
        /// </summary>
        public double PlateWidth
        {
            get
            {
                return (double)GetValue(PlateWidthProperty);
            }
            set
            {
                SetValue(PlateWidthProperty, value);
            }
        }

        public static readonly DependencyProperty PlateHeightProperty =
            DependencyProperty.Register("PlateHeight", typeof(double), typeof(CompassControl),
                new PropertyMetadata(0d, new PropertyChangedCallback(OnPlateHeightChanged)));

        public double PlateHeight
        {
            get
            {
                return (double)GetValue(PlateHeightProperty);
            }
            set
            {
                SetValue(PlateHeightProperty, value);
            }
        }

        private static void OnPlateHeightChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((CompassControl)sender).OnPlateHeightChanged((double)e.NewValue);
        }

        private void OnPlateHeightChanged(double value)
        {
            if (value > 0 && value != Plate.ActualHeight)
            {
                double relativeSize = value / PlateNativeHeight;
                SetRelativeSize(relativeSize);
                PlateWidth = relativeSize * PlateNativeWidth;
            }
        }

        public static readonly DependencyProperty PlateAngleProperty =
            DependencyProperty.Register("PlateAngle", typeof(double), typeof(CompassControl),
                new PropertyMetadata(0d, new PropertyChangedCallback(OnPlateAngleChanged)));

        /// <summary>
        /// Compass control (plate) angle.
        /// </summary>
        public double PlateAngle
        {
            get
            {
                return (double)GetValue(PlateAngleProperty);
            }
            set
            {
                SetValue(PlateAngleProperty, value);
            }
        }

        private static void OnPlateAngleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((CompassControl)sender).OnPlateAngleChanged((double)e.NewValue);
        }

        private void OnPlateAngleChanged(double value)
        {
            if (value != PlateRotation.Angle)
            {
                Debug.WriteLine(DebugTag + "OnPlateAngleChanged(): " + value);
                PlateRotation.Angle = value;
                UpdateNeedleAngle();
            }
        }

        /// <summary>
        /// If true, the orienteering box (scale) will always be automatically
        /// set to north.
        /// </summary>
        private bool _autoNorth;
        public bool AutoNorth
        {
            get
            {
                return _autoNorth;
            }
            set
            {
                _autoNorth = value;

                if (_autoNorth == true)
                {
                    RotateBoxToNorth(AngleOffset);
                }
            }
        }

        private double _angleOffset = 0;
        public double AngleOffset
        {
            get
            {
                return _angleOffset;
            }
            set
            {
                Debug.WriteLine(DebugTag + "AngleOffset.set: " + value);
                _angleOffset = value;
                PlateAngle = _angleOffset + _adjustedAngle;
                UpdateNeedleAngle();
            }
        }

        /// <summary>
        /// Enabling/disabling touch manipulation.
        /// </summary>
        public bool ManipulationEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CompassControl()
        {
            InitializeComponent();
            PlateWidth = DefaultPlateWidth;
            PlateHeight = DefaultPlateHeight;
            ManipulatedArea = CompassControlArea.None;
            ManipulationEnabled = true;
            LayoutRoot.CacheMode = new BitmapCache();
        }

        /// <summary>
        /// Rotates the box (scale) to point to north.
        /// </summary>
        /// <param name="offset"></param>
        public void RotateBoxToNorth(double offset)
        {
            ScaleRotation.Angle = offset - PlateRotation.Angle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        private CompassControlArea AreaAt(double x, double y, UIElement container)
        {
            if (container != null)
            {
                if (container == Plate)
                {
                    Debug.WriteLine("Container == Plate");
                    if (y < _plateTopEndY)
                    {
                        return CompassControlArea.PlateTop;
                    }

                    if (y > _plateBottomStartY)
                    {
                        return CompassControlArea.PlateBottom;
                    }

                    return CompassControlArea.PlateCenter;
                }
                else if (container == Scale)
                {
                    Debug.WriteLine("Container == Scale");
                    return CompassControlArea.Scale;
                }
            }

            return CompassControlArea.None;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            if (ManipulationEnabled)
            {
                _previousX = e.ManipulationOrigin.X;
                _previousY = e.ManipulationOrigin.Y;
                ManipulatedArea = AreaAt(_previousX, _previousY, e.ManipulationContainer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            ManipulatedArea = CompassControlArea.None;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (!ManipulationEnabled)
            {
                return;
            }

            if (ManipulatedArea == CompassControlArea.PlateTop)
            {
                double deltaX = e.ManipulationOrigin.X - _plateCenterX;
                double deltaY = e.ManipulationOrigin.Y - _plateCenterY;
                double previousDeltaX = _previousX - _plateCenterX;
                double previousDeltaY = _previousY - _plateCenterY;

                double angleDelta = -Math.Round(
                    (Math.Atan2(previousDeltaY, previousDeltaX)
                    - Math.Atan2(deltaY, deltaX))
                    * RadiansToDegreesCoefficient);

                if (angleDelta > 180)
                {
                    angleDelta -= 360;
                }
                else if (angleDelta < -180)
                {
                    angleDelta += 360;
                }

                _adjustedAngle = (PlateAngle + angleDelta) % 360;
                PlateAngle = AngleOffset + _adjustedAngle;
                UpdateNeedleAngle();

                if (AutoNorth)
                {
                    RotateBoxToNorth(AngleOffset);
                }
            }
            else if (ManipulatedArea == CompassControlArea.Scale && !AutoNorth)
            {
                double x = e.ManipulationOrigin.X;
                double y = e.ManipulationOrigin.Y;

                double centerX = Scale.ActualWidth / 2;
                double centerY = Scale.ActualHeight / 2;
                double deltaX = x - centerX;
                double deltaY = y - centerY;
                double previousDeltaX = _previousX - centerX;
                double previousDeltaY = _previousY - centerY;

                double angleDelta = -Math.Round(
                    (Math.Atan2(previousDeltaY, previousDeltaX)
                    - Math.Atan2(deltaY, deltaX))
                    * RadiansToDegreesCoefficient);

                if (angleDelta > 180)
                {
                    angleDelta -= 360;
                }
                else if (angleDelta < -180)
                {
                    angleDelta += 360;
                }

                ScaleRotation.Angle = (ScaleRotation.Angle + angleDelta) % 360;
            }
        }

        /// <summary>
        /// Scales the components of the compass control based on the given
        /// relative size.
        /// </summary>
        /// <param name="relativeSize">
        ///     The size relative to the native size of
        ///     the components. 1.0 is the native size (100 %).
        /// </param>
        private void SetRelativeSize(double relativeSize)
        {
            if (relativeSize <= 0)
            {
                Debug.WriteLine(DebugTag + "SetRelativeSize(): Invalid argument (" + relativeSize + ")!");
                return;
            }

            double plateWidth = relativeSize * PlateNativeWidth;
            double plateHeight = relativeSize * PlateNativeHeight;

            PlateBackground.Width = plateWidth;
            PlateBackground.Height = plateHeight;
            Plate.Width = plateWidth;
            Plate.Height = plateHeight;

            double scaleWidth = relativeSize * ScaleNativeWidth;
            ScaleShadow.Width = scaleWidth;
            Scale.Width = scaleWidth;

            Needle.Width = relativeSize * NeedleNativeWidth;

            _plateCenterX = plateWidth / 2;
            _plateCenterY = plateHeight / 2;

            if (_scalePosition != ScaleGrid.RenderTransform as TranslateTransform)
            {
                _scalePosition = ScaleGrid.RenderTransform as TranslateTransform;
            }

            double scaleTopMargin = plateHeight * ScaleRelativeTopMargin;

            if (_scalePosition != null)
            {
                _scalePosition.Y = scaleTopMargin;
            }

            _plateTopEndY = plateHeight * PlateManipulationRelativeTopMargin;
            _plateBottomStartY = plateHeight - _plateTopEndY;

            Debug.WriteLine(DebugTag + "SetRelativeSize(): Relative size was set to "
                + relativeSize + ". Plate size is therefore "
                + plateWidth + "x" + plateHeight + " and manipulation margins are "
                + _plateTopEndY + " and "
                + _plateBottomStartY + ".");
        }

        /// <summary>
        /// Updates the needle angle based on the current compass reading.
        /// The angle of the plate is compensated.
        /// </summary>
        private void UpdateNeedleAngle()
        {
            NeedleRotation.Angle = -(_compassReading - 360) - PlateRotation.Angle;
        }
    }
}
