using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.ViewModels
{
    public class ReportsViewModel : INotifyPropertyChanged
    {
        private readonly IReportsService _reportsService;
        private DateTime _startDate = DateTime.Now.Date;
        private DateTime _endDate = DateTime.Now.Date.AddDays(1).AddTicks(-1);
        private ObservableCollection<Docket> _dockets;

        public ReportsViewModel(IReportsService reportsService)
        {
            _reportsService = reportsService;
            GenerateReportCommand = new Command(async () => await GenerateReport());
            Dockets = new ObservableCollection<Docket>();
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Docket> Dockets
        {
            get => _dockets;
            set
            {
                if (_dockets != value)
                {
                    _dockets = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand GenerateReportCommand { get; }

        private async Task GenerateReport()
        {
            var dockets = await _reportsService.GetDocketsByDateRangeAsync(StartDate, EndDate);
            Dockets.Clear();
            foreach (var docket in dockets)
            {
                Dockets.Add(docket);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
