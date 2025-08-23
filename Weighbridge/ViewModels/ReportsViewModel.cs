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

        private int _totalLoads;
        private double _grossWeight;
        private double _tareWeight;
        private double _netWeight;

        public int TotalLoads
        {
            get => _totalLoads;
            set
            {
                if (_totalLoads != value)
                {
                    _totalLoads = value;
                    OnPropertyChanged();
                }
            }
        }

        public double GrossWeight
        {
            get => _grossWeight;
            set
            {
                if (_grossWeight != value)
                {
                    _grossWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TareWeight
        {
            get => _tareWeight;
            set
            {
                if (_tareWeight != value)
                {
                    _tareWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public double NetWeight
        {
            get => _netWeight;
            set
            {
                if (_netWeight != value)
                {
                    _netWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public ReportsViewModel(IReportsService reportsService)
        {
            _reportsService = reportsService;
            GenerateReportCommand = new Command(async () => await GenerateReport());
            _dockets = new ObservableCollection<Docket>();
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

        

        public ICommand GenerateReportCommand { get; }

        private async Task GenerateReport()
        {
            var dockets = await _reportsService.GetDocketsByDateRangeAsync(StartDate, EndDate);
            if (dockets != null)
            {
                TotalLoads = dockets.Count;
                GrossWeight = (double)dockets.Sum(d => d.EntranceWeight);
                TareWeight = (double)dockets.Sum(d => d.ExitWeight);
                NetWeight = (double)dockets.Sum(d => d.NetWeight);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}