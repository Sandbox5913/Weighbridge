using Microsoft.Maui.Controls;
using System.Collections;
using System.Windows.Input;
using System.Linq; // Added for .Cast<object>().ToList()
using System; // Added for Math

namespace Weighbridge.Behaviors
{
    public class ComboBoxKeyboardBehavior : Behavior<Entry>
    {
        public static readonly BindableProperty SuggestionsSourceProperty =
            BindableProperty.Create(nameof(SuggestionsSource), typeof(IEnumerable), typeof(ComboBoxKeyboardBehavior));
            
        public static readonly BindableProperty IsDropDownOpenProperty =
            BindableProperty.Create(nameof(IsDropDownOpen), typeof(bool), typeof(ComboBoxKeyboardBehavior));
            
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(ComboBoxKeyboardBehavior));
            
        public static readonly BindableProperty SelectionCommandProperty =
            BindableProperty.Create(nameof(SelectionCommand), typeof(ICommand), typeof(ComboBoxKeyboardBehavior));

        public IEnumerable SuggestionsSource
        {
            get => (IEnumerable)GetValue(SuggestionsSourceProperty);
            set => SetValue(SuggestionsSourceProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public ICommand SelectionCommand
        {
            get => (ICommand)GetValue(SelectionCommandProperty);
            set => SetValue(SelectionCommandProperty, value);
        }

        private int _highlightedIndex = -1;
        private List<object> _suggestionsList = new();

        protected override void OnAttachedTo(Entry entry)
        {
            entry.Focused += OnEntryFocused;
            entry.Unfocused += OnEntryUnfocused;
            
            // Add platform-specific keyboard handling
#if WINDOWS
            entry.HandlerChanged += OnHandlerChanged;
#endif
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.Focused -= OnEntryFocused;
            entry.Unfocused -= OnEntryUnfocused;
#if WINDOWS
            entry.HandlerChanged -= OnHandlerChanged;
#endif
            base.OnDetachingFrom(entry);
        }

#if WINDOWS
        private void OnHandlerChanged(object sender, EventArgs e)
        {
            if (sender is Entry entry && entry.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
            {
                textBox.KeyDown += OnKeyDown;
            }
        }

        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (!IsDropDownOpen || SuggestionsSource == null) return;

            _suggestionsList = SuggestionsSource.Cast<object>().ToList();
            
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Down:
                    _highlightedIndex = Math.Min(_highlightedIndex + 1, _suggestionsList.Count - 1);
                    e.Handled = true;
                    break;
                    
                case Windows.System.VirtualKey.Up:
                    _highlightedIndex = Math.Max(_highlightedIndex - 1, -1);
                    e.Handled = true;
                    break;
                    
                case Windows.System.VirtualKey.Enter:
                    if (_highlightedIndex >= 0 && _highlightedIndex < _suggestionsList.Count)
                    {
                        SelectionCommand?.Execute(_suggestionsList[_highlightedIndex]);
                    }
                    e.Handled = true;
                    break;
                    
                case Windows.System.VirtualKey.Escape:
                    IsDropDownOpen = false;
                    e.Handled = true;
                    break;
            }
        }
#endif

        private void OnEntryFocused(object sender, FocusEventArgs e)
        {
            _highlightedIndex = -1;
        }

        private void OnEntryUnfocused(object sender, FocusEventArgs e)
        {
            // Delay hiding to allow for item selection
            Device.StartTimer(TimeSpan.FromMilliseconds(150), () =>
            {
                IsDropDownOpen = false;
                return false;
            });
        }
    }
}
