/**
 * Copyright (c) 2012 Nokia Corporation.
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

namespace Compass.Ui
{
    /// <summary>
    /// 
    /// </summary>
    public partial class CompassControl : UserControl
    {
        // Constants
        private const double RadiansToDegreesCoefficient = 57.2957795; // 180 / PI
        private const int PlateNativeWidth = 290;
        private const int PlateNativeHeight = 600;
        private const int ScaleNativeWidth = 276;
        private const int ScaleNativeHeight = 276;
        private const int NeedleNativeWidth = 21;
        private const float ScaleRelativeTopMargin = 0.32f;
        private const float PlateManipulationRelativeTopMargin = 0.15f;

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
        private double _compassReading = 0;
        private CompassControlArea _manipulatedArea = CompassControlArea.None;
        private double _previousX = 0;
        private double _previousY = 0;
        private int _plateManipulationBottom = 0;
        private int _scaleManipulationTop = 0;
        private int _scaleManipulationBottom = 0;
        private Boolean _isHd = false;
        private double _plateCenterX = 0;
        private double _plateCenterY = 0;

        // Getters and setters

        public double CompassReading
        {
            get
            {
                return _compassReading;
            }
            set
            {
                _compassReading = value;
                //Debug.WriteLine("CompassControl::CompassReading.set(): " + _compassReading);
                UpdateNeedleAngle();
            }
        }

        public CompassControlArea ManipulatedArea
        {
            get
            {
                return _manipulatedArea;
            }
        }

        public double PlateWidth
        {
            get
            {
                return Plate.ActualWidth;
            }
        }

        public double PlateHeight
        {
            get
            {
                return Plate.ActualHeight;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CompassControl()
        {
            InitializeComponent();

            if (Application.Current.Host.Content.ActualWidth >= 720)
            {
                _isHd = true;
            }

            Debug.WriteLine("CompassControl::CompassControl(): Is HD: " + _isHd);

            SetRelativeSize(0.6f);
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
                    if (y < _plateManipulationBottom)
                    {
                        return CompassControlArea.PlateTop;
                    }
                    else if (y >= _plateManipulationBottom && y < _scaleManipulationTop)
                    {
                        return CompassControlArea.PlateCenter;
                    }
                    else if (y > _scaleManipulationBottom)
                    {
                        return CompassControlArea.PlateBottom;
                    }
                }
                else if (container == Scale)
                {
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
            _previousX = e.ManipulationOrigin.X;
            _previousY = e.ManipulationOrigin.Y;
            _manipulatedArea = AreaAt(_previousX, _previousY, e.ManipulationContainer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            _manipulatedArea = CompassControlArea.None;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (_manipulatedArea == CompassControlArea.PlateTop)
            {
                double deltaX = e.ManipulationOrigin.X - _plateCenterX;
                double deltaY = e.ManipulationOrigin.Y - _plateCenterY;
                double previousDeltaX = _previousX - _plateCenterX;
                double previousDeltaY = _previousY - _plateCenterY;

                int angleDelta = 
                    -(int)Math.Round(
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

                PlateRotation.Angle = (PlateRotation.Angle + angleDelta) % 360;
                UpdateNeedleAngle();
            }
            else if (_manipulatedArea == CompassControlArea.Scale)
            {
                double x = e.ManipulationOrigin.X;
                double y = e.ManipulationOrigin.Y;

                double centerX = Scale.ActualWidth / 2;
                double centerY = Scale.ActualHeight / 2;
                double deltaX = x - centerX;
                double deltaY = y - centerY;
                double previousDeltaX = _previousX - centerX;
                double previousDeltaY = _previousY - centerY;

                int angleDelta =
                    -(int)Math.Round(
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
        private void SetRelativeSize(float relativeSize)
        {
            if (relativeSize <= 0)
            {
                return;
            }

            if (_isHd)
            {
                // The app is running on a device with HD resolution. Thus,
                // increase relativeSize for similar experience as on lower
                // resolution.
                relativeSize *= 1.5f;
            }

            Plate.Width = relativeSize * PlateNativeWidth;
            ScaleShadow.Width = relativeSize * ScaleNativeWidth;
            Scale.Width = relativeSize * ScaleNativeWidth;
            Needle.Width = relativeSize * NeedleNativeWidth;

            _plateCenterX = relativeSize * PlateNativeWidth / 2;
            _plateCenterY = relativeSize * PlateNativeHeight / 2;

            Thickness topMargin = new Thickness();
            topMargin.Top = relativeSize * PlateNativeHeight * ScaleRelativeTopMargin;
            ScaleShadow.Margin = topMargin;
            Scale.Margin = topMargin;
            Needle.Margin = topMargin;

            _plateManipulationBottom =
                (int)(PlateNativeHeight * PlateManipulationRelativeTopMargin);
            _scaleManipulationTop = (int)(topMargin.Top + 60 * relativeSize);
            _scaleManipulationBottom = (int)(_scaleManipulationTop + ScaleNativeHeight * relativeSize);

            Debug.WriteLine("Manipulation margins: " + _plateManipulationBottom
                + ", " + _scaleManipulationTop
                + ", " + _scaleManipulationBottom);
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
