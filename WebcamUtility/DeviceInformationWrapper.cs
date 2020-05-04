using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;

namespace WebcamUtility
{
    public class DeviceInformationWrapper : INotifyPropertyChanged
    {
        public DeviceInformationWrapper(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
        }

        public DeviceInformationKind Kind => DeviceInformation.Kind;
        public string Id => DeviceInformation.Id;
        public string Name => DeviceInformation.Name;
        public bool IsDefault => DeviceInformation.IsDefault;
        public bool CanPair => DeviceInformation.Pairing.CanPair;
        public bool IsPaired => DeviceInformation.Pairing.IsPaired;
        public IReadOnlyDictionary<string, object> Properties => DeviceInformation.Properties;
        public DeviceInformation DeviceInformation { get; private set; }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);

            OnPropertyChanged("Kind");
            OnPropertyChanged("Id");
            OnPropertyChanged("Name");
            OnPropertyChanged("IsDefault");
            OnPropertyChanged("DeviceInformation");
            OnPropertyChanged("CanPair");
            OnPropertyChanged("IsPaired");
            OnPropertyChanged("GetPropertyForDisplay");
        }

        public string GetPropertyForDisplay(string key) => Properties[key]?.ToString();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
