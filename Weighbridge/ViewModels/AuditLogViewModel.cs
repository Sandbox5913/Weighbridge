
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.ViewModels
{
    public class AuditLogViewModel : INotifyPropertyChanged
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private string _searchText;

        public AuditLogViewModel(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
            LoadAuditLogsCommand = new Command(async () => await LoadAuditLogs());
            SearchCommand = new Command(async () => await FilterAuditLogs());
            LoadAuditLogsCommand.Execute(null);
        }

        public ICommand LoadAuditLogsCommand { get; }
        public ICommand SearchCommand { get; }

        public ObservableCollection<AuditLog> AuditLogs { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    SearchCommand.Execute(null);
                }
            }
        }

        private async Task LoadAuditLogs()
        {
            var auditLogs = await _auditLogRepository.GetAuditLogsAsync();
            AuditLogs.Clear();
            foreach (var log in auditLogs)
            {
                AuditLogs.Add(log);
            }
        }

        private async Task FilterAuditLogs()
        {
            var allLogs = await _auditLogRepository.GetAuditLogsAsync();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                AuditLogs.Clear();
                foreach (var log in allLogs)
                {
                    AuditLogs.Add(log);
                }
            }
            else
            {
                var filteredLogs = allLogs.Where(log =>
                    log.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    log.Action.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    log.EntityType.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    log.Details.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    log.Timestamp.ToString("g").Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                AuditLogs.Clear();
                foreach (var log in filteredLogs)
                {
                    AuditLogs.Add(log);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
