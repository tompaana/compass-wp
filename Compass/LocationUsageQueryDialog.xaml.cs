/**
 * Copyright (c) 2013 Nokia Corporation. All rights reserved.
 *
 * Nokia, Nokia Connecting People, Nokia Developer, and HERE are trademarks
 * and/or registered trademarks of Nokia Corporation. Other product and company
 * names mentioned herein may be trademarks or trade names of their respective
 * owners.
 *
 * See the license text file delivered with this project for more information.
 */

using Microsoft.Phone.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Compass.Resources;

namespace Compass.Ui
{
    /// <summary>
    /// The LocationUsageQueryDialog is a user control which can be placed on
    /// the first page in the app. The control must be the last element inside
    /// the layout grid and span all rows and columns so it is not obscured.
    /// </summary>
    public partial class LocationUsageQueryDialog : UserControl
    {
        // PhoneApplicationFrame needed for detecting back presses
        private PhoneApplicationFrame _rootFrame = null;

        public EventHandler LocationUsageAllowed = null;

        // Title of the review/feedback notification
        public string Title
        {
            set
            {
                if (title.Text != value)
                {
                    title.Text = value;
                }
            }
        }

        // Message of the review/feedback notification
        public string Message
        {
            set
            {
                if (message.Text != value)
                {
                    message.Text = value;
                }
            }
        }

        // Button text for not acting upon review/feedback notification
        public string NoText
        {
            set
            {
                if ((string)noButton.Content != value)
                {
                    noButton.Content = value;
                }
            }
        }

        // Button text for acting upon review/feedback notification
        public string YesText
        {
            set
            {
                if ((string)yesButton.Content != value)
                {
                    yesButton.Content = value;
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocationUsageQueryDialog()
        {
            InitializeComponent();

            Title = AppResources.LocationUsageQueryTitle;
            Message = AppResources.LocationUsageQueryText;
            YesText = AppResources.LocationAllowButtonText;
            NoText = AppResources.LocationCancelButtonText;

            yesButton.Click += yesButton_Click;
            noButton.Click += noButton_Click;
            Loaded += LocationUsageQueryDialog_Loaded;
            hideContent.Completed += hideContent_Completed;
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        public void Show()
        {
            LayoutRoot.Opacity = 0;
            xProjection.RotationX = 90;

            Visibility = Visibility.Visible;

            showContent.Begin();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocationUsageQueryDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // This class needs to be aware of Back key presses.
            AttachBackKeyPressed();
        }

        /// <summary>
        /// Detect back key presses.
        /// </summary>
        private void AttachBackKeyPressed()
        {
            if (_rootFrame == null)
            {
                _rootFrame = Application.Current.RootVisual as PhoneApplicationFrame;
                _rootFrame.BackKeyPress += LocationUsageQueryDialog_BackKeyPress;
            }
        }

        /// <summary>
        /// Handle back key presses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocationUsageQueryDialog_BackKeyPress(object sender, CancelEventArgs e)
        {
            // If back is pressed whilst notification is open, close 
            // the notification and cancel back to stop app from exiting.
            if (Visibility == System.Windows.Visibility.Visible)
            {
                noButton_Click(this, null);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Called when yes button is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void yesButton_Click(object sender, RoutedEventArgs e)
        {
            hideContent.Begin();

            if (LocationUsageAllowed != null)
            {
                LocationUsageAllowed(this, null);
            }
        }

        /// <summary>
        /// Called when no button is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void noButton_Click(object sender, RoutedEventArgs e)
        {
            hideContent.Begin();
        }

        /// <summary>
        /// Called when notification gets hidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hideContent_Completed(object sender, EventArgs e)
        {
            if (_rootFrame != null)
            {
                _rootFrame.BackKeyPress -= LocationUsageQueryDialog_BackKeyPress;
            }

            Visibility = Visibility.Collapsed;
        }
    }
}
