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
    public partial class SplashScreen : PhoneApplicationPage
    {
        public SplashScreen()
        {
            InitializeComponent();
            this.Loaded += SplashScreen_Loaded;
        }

        void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            App.Properties = DeviceProperties.GetInstance(); // Construct the DeviceProperties instance
            App.Properties.Init();
            NavigationService.Navigate(new Uri("/MainPage.xaml?was_launched=true", UriKind.Relative));
        }
    }
}