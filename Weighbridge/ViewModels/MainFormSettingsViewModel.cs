using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Weighbridge.Models;

namespace Weighbridge.ViewModels
{
    public class MainFormSettingsViewModel : INotifyPropertyChanged
    {
        private MainFormConfig _formConfig;
        public MainFormConfig FormConfig
        {
            get => _formConfig;
            set => SetProperty(ref _formConfig, value);
        }

        public ICommand SaveConfigCommand { get; }

        public event Func<string, string, string, Task> ShowAlert;

        public MainFormSettingsViewModel()
        {
            LoadConfig();
            SaveConfigCommand = new Command(async () => await SaveConfig());
        }

        private void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(FileSystem.AppDataDirectory, "mainformconfig.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    FormConfig = JsonSerializer.Deserialize<MainFormConfig>(json);
                }
                else
                {
                    FormConfig = new MainFormConfig();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                FormConfig = new MainFormConfig();
            }
        }

        private async Task SaveConfig()
        {
            try
            {
                string configPath = Path.Combine(FileSystem.AppDataDirectory, "mainformconfig.json");
                string json = JsonSerializer.Serialize(FormConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                if (ShowAlert != null)
                {
                    await ShowAlert.Invoke("Success", "Settings saved successfully.", "OK");
                }
            }
            catch (Exception ex)
            {
                if (ShowAlert != null)
                {
                    await ShowAlert.Invoke("Error", $"Failed to save settings: {ex.Message}", "OK");
                }
            }
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
