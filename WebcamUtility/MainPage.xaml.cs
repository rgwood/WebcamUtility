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
using Windows.UI.Core;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using System.Collections.ObjectModel;

namespace WebcamUtility
{
    public sealed partial class MainPage : Page, IDisposable
    {
        // TODO: automatically update when the list of webcams changes
        // Next: animate the change 😎

        ObservableCollection<DeviceInformationWrapper> devices = new ObservableCollection<DeviceInformationWrapper>();
        MediaCapture mediaCapture;
        bool isPreviewing;
        DeviceWatcher deviceWatcher;

        public MainPage()
        {
            this.InitializeComponent();
            CameraListView.ItemsSource = devices;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Start();

           

            //await StartPreviewAsync();
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            throw new NotImplementedException();
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            throw new NotImplementedException();
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            // WinRT event handlers run on a worker thread so we gotta run the update
            // on a UI thread. Not sure whether this applies to *all* WinRT events
            // Can verify with Dispatcher.HasThreadAccess (returns false in this handler)
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var di = new DeviceInformationWrapper(deviceInfo);
                devices.Add(di);
                if(devices.Count == 1)
                {
                    CameraListView.SelectedItem = di;
                    await StartPreviewAsync(di.Id);
                }
            });
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
        }

        private async Task StartPreviewAsync(string videoDeviceId = null)
        {
            try
            {
                if(mediaCapture != null && isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                mediaCapture = new MediaCapture();

                if(videoDeviceId == null)
                {
                    await mediaCapture.InitializeAsync();
                }
                else
                {
                    var initSettings = new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = videoDeviceId
                    };
                    await mediaCapture.InitializeAsync(initSettings);
                }
                
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                ShowMessageToUser("The app was denied access to the camera");
                return;
            }

            try
            {
                PreviewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(
            MediaCapture sender,
            MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        private async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }

        }

        private void ShowMessageToUser(string message)
        {
            //TODO: implement this
            throw new NotImplementedException(message);
        }



        // The default SplitView behaviour is annoying; the pane closes as soon as the window is resized
        // or the content is clicked. Suppress it.
        private void splitView_PaneClosing(SplitView _, SplitViewPaneClosingEventArgs args)
        {
            args.Cancel = true;
        }

        public void Dispose()
        {
            mediaCapture.Dispose();
        }
    }
}
