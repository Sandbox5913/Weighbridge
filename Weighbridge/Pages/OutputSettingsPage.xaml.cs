
using Weighbridge.Models;
using System.Text.Json;
using Weighbridge.Services;

namespace Weighbridge.Pages
{
    public partial class OutputSettingsPage : ContentPage
    {
        private DocketTemplate _template = new();
        private readonly IDocketService _docketService;

        public OutputSettingsPage(IDocketService docketService)
        {
            InitializeComponent();
            _docketService = docketService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            var templateJson = Preferences.Get("DocketTemplate", string.Empty);
            if (!string.IsNullOrEmpty(templateJson))
            {
                _template = JsonSerializer.Deserialize<DocketTemplate>(templateJson) ?? new DocketTemplate();
            }

            HeaderTextEntry.Text = _template.HeaderText;
            PageWidthEntry.Text = _template.PageWidthMm.ToString();
            PageHeightEntry.Text = _template.PageHeightMm.ToString();
            HeaderFontSizeEntry.Text = _template.HeaderFontSize.ToString();
            BodyFontSizeEntry.Text = _template.BodyFontSize.ToString();

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

            ExportEnabledSwitch.IsToggled = Preferences.Get("ExportEnabled", false);
            ExportFolderPathEntry.Text = Preferences.Get("ExportFolderPath", string.Empty);
            ExportFormatPicker.SelectedItem = Preferences.Get("ExportFormat", "Csv");
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
            _template.HeaderText = HeaderTextEntry.Text;
            if (float.TryParse(PageWidthEntry.Text, out float width))
            {
                _template.PageWidthMm = width;
            }
            if (float.TryParse(PageHeightEntry.Text, out float height))
            {
                _template.PageHeightMm = height;
            }
            if (float.TryParse(HeaderFontSizeEntry.Text, out float headerFontSize))
            {
                _template.HeaderFontSize = headerFontSize;
            }
            if (float.TryParse(BodyFontSizeEntry.Text, out float bodyFontSize))
            {
                _template.BodyFontSize = bodyFontSize;
            }

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

            Preferences.Set("ExportEnabled", ExportEnabledSwitch.IsToggled);
            Preferences.Set("ExportFolderPath", ExportFolderPathEntry.Text);
            if (ExportFormatPicker.SelectedItem != null)
            {
                Preferences.Set("ExportFormat", ExportFormatPicker.SelectedItem.ToString());
            }

            DisplayAlert("Success", "Settings saved.", "OK");
        }

        private async void OnShowPrintPreviewClicked(object sender, EventArgs e)
        {
            var docketData = new DocketData
            {
                EntranceWeight = "12345",
                ExitWeight = "54321",
                NetWeight = "41976",
                VehicleLicense = "PREVIEW123",
                SourceSite = "Source Site Preview",
                DestinationSite = "Destination Site Preview",
                Material = "Material Preview",
                Customer = "Customer Preview",
                TransportCompany = "Transport Company Preview",
                Driver = "Driver Preview",
                Remarks = "This is a preview of the remarks.",
                Timestamp = DateTime.Now
            };

            var template = new DocketTemplate
            {
                LogoPath = _template.LogoPath,
                HeaderText = HeaderTextEntry.Text,
                PageWidthMm = float.TryParse(PageWidthEntry.Text, out float width) ? width : 210,
                PageHeightMm = float.TryParse(PageHeightEntry.Text, out float height) ? height : 297,
                HeaderFontSize = float.TryParse(HeaderFontSizeEntry.Text, out float headerFontSize) ? headerFontSize : 20,
                BodyFontSize = float.TryParse(BodyFontSizeEntry.Text, out float bodyFontSize) ? bodyFontSize : 12,
                ShowEntranceWeight = ShowEntranceWeightSwitch.IsToggled,
                ShowExitWeight = ShowExitWeightSwitch.IsToggled,
                ShowNetWeight = ShowNetWeightSwitch.IsToggled,
                ShowVehicleLicense = ShowVehicleLicenseSwitch.IsToggled,
                ShowSourceSite = ShowSourceSiteSwitch.IsToggled,
                ShowDestinationSite = ShowDestinationSiteSwitch.IsToggled,
                ShowMaterial = ShowMaterialSwitch.IsToggled,
                ShowCustomer = ShowCustomerSwitch.IsToggled,
                ShowTransportCompany = ShowTransportCompanySwitch.IsToggled,
                ShowDriver = ShowDriverSwitch.IsToggled,
                ShowRemarks = ShowRemarksSwitch.IsToggled,
                ShowTimestamp = ShowTimestampSwitch.IsToggled
            };

            var filePath = await _docketService.GeneratePdfAsync(docketData, template);
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }

        private async void OnBrowseFolderClicked(object sender, EventArgs e)
        {
            // This is a placeholder for a folder picker. MAUI does not have a built-in folder picker.
            // You might need to use a third-party library or implement a platform-specific solution.
            var folderPath = await DisplayPromptAsync("Folder Path", "Enter the full path to the export folder.");
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                ExportFolderPathEntry.Text = folderPath;
            }
        }
    }
}