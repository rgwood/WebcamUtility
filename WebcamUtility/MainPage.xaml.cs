using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WebcamUtility
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DeviceInformationCollection devices;
        public MainPage()
        {
            this.InitializeComponent();


        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            CameraListView.ItemsSource = devices;
            Debug.WriteLine($"{devices.Count} video devices found");
        }

        // The default SplitView behaviour is annoying; the pane closes as soon as the window is resized
        // or the content is clicked. 
        private void splitView_PaneClosing(SplitView sender, SplitViewPaneClosingEventArgs args)
        {
            args.Cancel = true;
        }

         

        // TODO: get a UI working that shows webcams
        // Next: automatically update when the list of webcams changes
        // Next: animate the change 😎


        //

    }
}
