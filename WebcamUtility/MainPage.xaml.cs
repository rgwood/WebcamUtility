using System;
using System.IO;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.Media.Capture;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.UI.ViewManagement;
using Windows.System;

namespace WebcamUtility
{
    public sealed partial class MainPage : Page, IDisposable
    {
        // TODO: Animate the change 😎

        ObservableCollection<DeviceInformationWrapper> devices = new ObservableCollection<DeviceInformationWrapper>();
        MediaCapture mediaCapture;
        bool isPreviewing;
        DeviceWatcher deviceWatcher;

        public MainPage()
        {
            this.InitializeComponent();
            CameraListView.ItemsSource = devices;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += (a, b) => throw new NotImplementedException();
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Start();
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            // WinRT event handlers run on a worker thread so we gotta run the update
            // on a UI thread. Not sure whether this applies to *all* WinRT events
            // Can verify with Dispatcher.HasThreadAccess (returns false in this handler)
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var di = new DeviceInformationWrapper(deviceInfo);
                devices.Add(di);
                if (devices.Count == 1)
                {
                    CameraListView.SelectedItem = di;
                }
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var device in devices)
                {
                    if (device.Id == update.Id)
                    {
                        devices.Remove(device);
                        break;
                    }
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
                    await CleanupCameraAsync();
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
                    try
                    {
                        await mediaCapture.StopPreviewAsync();
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // 
                    {
                        // Can fail when camera has been unplugged but we don't really care
                        // Error message: "The video recording device is no longer present."
                        Debug.WriteLine(ex);
                    }
                    isPreviewing = false;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }

        }

        //TODO: implement
        private void ShowMessageToUser(string message)
        {
            throw new NotImplementedException(message);
        }

        // The default SplitView behaviour is undesirable; the pane closes as soon as
        // the window is resized or the content is clicked. Suppress it.
        private void splitView_PaneClosing(SplitView _, SplitViewPaneClosingEventArgs args)
        {
            args.Cancel = true;
        }

        public void Dispose()
        {
            mediaCapture.Dispose();
        }

        private async void CameraListView_SelectionChanged(object _1, SelectionChangedEventArgs _2)
        {
            var selectedDevice = (DeviceInformationWrapper) CameraListView.SelectedItem;
            if (selectedDevice == null)
            {
                await CleanupCameraAsync();
                CameraListView.SelectedItem = devices.First();
            }
            else
            {
                await StartPreviewAsync(selectedDevice.Id);
            }
        }

        private void Page_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.F:
                    ToggleFullScreen();
                    break;
                // We never actually get an Escape key here 🤔
                // TODO: get this working
                case VirtualKey.Escape:
                    ExitFullScreen();
                    break;
                default:
                    break;
            }
        }

        private void ToggleFullScreen()
        {
            var view = ApplicationView.GetForCurrentView();
            if(view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
            }
            else
            {
                // Not clear from docs when this might fail - should I handle it?
                view.TryEnterFullScreenMode();
            }
        }

        private void ExitFullScreen()
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
            }
        }
    }
}
