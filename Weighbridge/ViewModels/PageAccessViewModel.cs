using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Weighbridge.ViewModels
{
    public class PageAccessViewModel : INotifyPropertyChanged
    {
        private string _pageName;
        private bool _hasAccess;

        public string PageName
        {
            get => _pageName;
            set => SetProperty(ref _pageName, value);
        }

        public bool HasAccess
        {
            get => _hasAccess;
            set => SetProperty(ref _hasAccess, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", System.Action onChanged = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
