using Weighbridge.Models;
using System.Text.Json;

namespace Weighbridge.Pages
{
    public partial class PrintSettingsPage : ContentPage
    {
        private DocketTemplate _template = new();

        public PrintSettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var templateJson = Preferences.Get("DocketTemplate", string.Empty);
            if (!string.IsNullOrEmpty(templateJson))
            {
                _template = JsonSerializer.Deserialize<DocketTemplate>(templateJson) ?? new DocketTemplate();
            }

            ShowEntranceWeightSwitch.IsToggled = _template.ShowEntranceWeight;
            ShowExitWeightSwitch.IsToggled = _template.ShowExitWeight;
            ShowNetWeightSwitch.IsToggled = _template.ShowNetWeight;
            ShowVehicleLicenseSwitch.IsToggled = _template.ShowVehicleLicense;
            ShowSourceSiteSwitch.IsToggled = _template.ShowSourceSite;
            ShowDestinationSiteSwitch.IsToggled = _template.ShowDestinationSite;
            ShowMaterialSwitch.IsToggled = _template.ShowMaterial;
            ShowCustomerSwitch.IsToggled = _template.ShowCustomer;
            ShowTransportCompanySwitch.IsToggled = _template.ShowTransportCompany;
            ShowDriverSwitch.IsToggled = _template.ShowDriver;
            ShowRemarksSwitch.IsToggled = _template.ShowRemarks;
            ShowTimestampSwitch.IsToggled = _template.ShowTimestamp;

            if (!string.IsNullOrEmpty(_template.LogoPath))
            {
                LogoImage.Source = ImageSource.FromFile(_template.LogoPath);
            }
        }

        private async void OnSelectLogoClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Please select an image file",
                FileTypes = FilePickerFileType.Images,
            });

            if (result != null)
            {
                _template.LogoPath = result.FullPath;
                LogoImage.Source = ImageSource.FromFile(_template.LogoPath);
            }
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            _template.ShowEntranceWeight = ShowEntranceWeightSwitch.IsToggled;
            _template.ShowExitWeight = ShowExitWeightSwitch.IsToggled;
            _template.ShowNetWeight = ShowNetWeightSwitch.IsToggled;
            _template.ShowVehicleLicense = ShowVehicleLicenseSwitch.IsToggled;
            _template.ShowSourceSite = ShowSourceSiteSwitch.IsToggled;
            _template.ShowDestinationSite = ShowDestinationSiteSwitch.IsToggled;
            _template.ShowMaterial = ShowMaterialSwitch.IsToggled;
            _template.ShowCustomer = ShowCustomerSwitch.IsToggled;
            _template.ShowTransportCompany = ShowTransportCompanySwitch.IsToggled;
            _template.ShowDriver = ShowDriverSwitch.IsToggled;
            _template.ShowRemarks = ShowRemarksSwitch.IsToggled;
            _template.ShowTimestamp = ShowTimestampSwitch.IsToggled;

            var templateJson = JsonSerializer.Serialize(_template);
            Preferences.Set("DocketTemplate", templateJson);

            DisplayAlert("Success", "Print settings saved.", "OK");
        }
    }
}
