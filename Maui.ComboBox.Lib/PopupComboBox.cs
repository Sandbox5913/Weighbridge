using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.Maui.Dispatching;
using Application = Microsoft.Maui.Controls.Application;
using ScrollView = Microsoft.Maui.Controls.ScrollView;
using VisualElement = Microsoft.Maui.Controls.VisualElement;

namespace Maui.ComboBox
{
    public partial class PopupComboBox : ContentView, IDisposable
    {
        private Popup? _popup;
        private bool _disposed;
        private readonly Border _popupContainer = new();
        private Image _arrowImage = new();
        private Entry _textEntry = new();
        private bool _isToggling = false;
        private bool _isUpdatingFromSelection = false;
        private List<object> _filteredItems = new();
        private List<object> _originalItems = new();

        public PopupComboBox()
        {
            HandlePopupComboBox();
        }

        #region Bindable Properties
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(PopupComboBox), propertyChanged: OnItemsSourceChanged);
        public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(PopupComboBox), null, BindingMode.TwoWay);
        public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(PopupComboBox), string.Empty);
        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(PopupComboBox), Colors.Black);
        public static readonly BindableProperty TextSizeProperty = BindableProperty.Create(nameof(TextSize), typeof(double), typeof(PopupComboBox), 12.0);
        public static readonly BindableProperty DropDownWidthProperty = BindableProperty.Create(nameof(DropDownWidth), typeof(double), typeof(PopupComboBox), -1.0);
        public static readonly BindableProperty DropDownHeightProperty = BindableProperty.Create(nameof(DropDownHeight), typeof(double), typeof(PopupComboBox), 200.0);
        public static readonly BindableProperty DropdownCornerRadiusProperty = BindableProperty.Create(nameof(DropdownCornerRadius), typeof(CornerRadius), typeof(PopupComboBox), new CornerRadius(0), propertyChanged: CornerRadiusChanged);
        public static readonly BindableProperty DropdownTextColorProperty = BindableProperty.Create(nameof(DropdownTextColor), typeof(Color), typeof(PopupComboBox), Colors.Black);
        public static readonly BindableProperty DropdownBackgroundColorProperty = BindableProperty.Create(nameof(DropdownBackgroundColor), typeof(Color), typeof(PopupComboBox), Colors.White);
        public static readonly BindableProperty DropdownBorderColorProperty = BindableProperty.Create(nameof(DropdownBorderColor), typeof(Color), typeof(PopupComboBox), Colors.Transparent);
        public static readonly BindableProperty DropdownBorderWidthProperty = BindableProperty.Create(nameof(DropdownBorderWidth), typeof(double), typeof(PopupComboBox), 0.0);
        public static readonly BindableProperty DropdownClosedImageSourceProperty = BindableProperty.Create(nameof(DropdownClosedImageSource), typeof(string), typeof(PopupComboBox), "chevron_right.svg");
        public static readonly BindableProperty DropdownOpenImageSourceProperty = BindableProperty.Create(nameof(DropdownOpenImageSource), typeof(string), typeof(PopupComboBox), "chevron_down.svg");
        public static readonly BindableProperty DropdownImageTintProperty = BindableProperty.Create(nameof(DropdownImageTint), typeof(Color), typeof(PopupComboBox));
        public static readonly BindableProperty DropdownShadowProperty = BindableProperty.Create(nameof(DropdownShadow), typeof(bool), typeof(PopupComboBox), true);
        public static readonly BindableProperty IsEditableProperty = BindableProperty.Create(nameof(IsEditable), typeof(bool), typeof(PopupComboBox), true);
        public static readonly BindableProperty MinimumSearchLengthProperty = BindableProperty.Create(nameof(MinimumSearchLength), typeof(int), typeof(PopupComboBox), 1);
        #endregion

        #region Properties
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public CornerRadius DropdownCornerRadius
        {
            get => (CornerRadius)GetValue(DropdownCornerRadiusProperty);
            set => SetValue(DropdownCornerRadiusProperty, value);
        }

        public double DropDownWidth
        {
            get => (double)GetValue(DropDownWidthProperty);
            set => SetValue(DropDownWidthProperty, value);
        }

        public double DropDownHeight
        {
            get => (double)GetValue(DropDownHeightProperty);
            set => SetValue(DropDownHeightProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty) ?? Colors.Black;
            set => SetValue(TextColorProperty, value);
        }

        public double TextSize
        {
            get => (double)GetValue(TextSizeProperty);
            set => SetValue(TextSizeProperty, value);
        }

        public Color DropdownTextColor
        {
            get => (Color)GetValue(DropdownTextColorProperty) ?? Colors.Black;
            set => SetValue(DropdownTextColorProperty, value);
        }

        public Color DropdownBorderColor
        {
            get => (Color)GetValue(DropdownBorderColorProperty) ?? Colors.Transparent;
            set => SetValue(DropdownBorderColorProperty, value);
        }

        public SolidColorBrush DropdownBorderColorBrush => new(DropdownBorderColor);

        public double DropdownBorderWidth
        {
            get => (double)GetValue(DropdownBorderWidthProperty);
            set => SetValue(DropdownBorderWidthProperty, value);
        }

        public Color DropdownBackgroundColor
        {
            get => (Color)GetValue(DropdownBackgroundColorProperty) ?? Colors.Gainsboro;
            set => SetValue(DropdownBackgroundColorProperty, value);
        }

        public string DropdownClosedImageSource
        {
            get => (string)GetValue(DropdownClosedImageSourceProperty);
            set => SetValue(DropdownClosedImageSourceProperty, value);
        }

        public string DropdownOpenImageSource
        {
            get => (string)GetValue(DropdownOpenImageSourceProperty);
            set => SetValue(DropdownOpenImageSourceProperty, value);
        }

        public Color? DropdownImageTint
        {
            get => (Color?)GetValue(DropdownImageTintProperty);
            set => SetValue(DropdownImageTintProperty, value);
        }

        public bool DropdownShadow
        {
            get => (bool)GetValue(DropdownShadowProperty);
            set => SetValue(DropdownShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the ComboBox allows free typing (true) or is dropdown-only (false).
        /// </summary>
        public bool IsEditable
        {
            get => (bool)GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum number of characters required before filtering suggestions.
        /// </summary>
        public int MinimumSearchLength
        {
            get => (int)GetValue(MinimumSearchLengthProperty);
            set => SetValue(MinimumSearchLengthProperty, value);
        }

        /// <summary>
        /// Gets the current text in the input field.
        /// </summary>
        public string Text => _textEntry?.Text ?? string.Empty;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the text in the input field changes.
        /// </summary>
        public event EventHandler<TextChangedEventArgs> TextChanged;

        /// <summary>
        /// Occurs when the user finishes editing the text.
        /// </summary>
        public event EventHandler<EventArgs> EditingCompleted;
        #endregion

        private INotifyCollectionChanged? _currentItemsSource;

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is PopupComboBox comboBox)
            {
                // Unsubscribe from old collection's CollectionChanged event
                if (comboBox._currentItemsSource != null)
                {
                    comboBox._currentItemsSource.CollectionChanged -= comboBox.OnCollectionChanged;
                    comboBox._currentItemsSource = null;
                }

                comboBox.UpdateOriginalItems();

                // Subscribe to new collection's CollectionChanged event
                if (newValue is INotifyCollectionChanged newNotifyCollection)
                {
                    comboBox._currentItemsSource = newNotifyCollection;
                    comboBox._currentItemsSource.CollectionChanged += comboBox.OnCollectionChanged;
                }
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateOriginalItems();
        }

        private void UpdateOriginalItems()
        {
            _originalItems.Clear();
            if (ItemsSource != null)
            {
                foreach (var item in ItemsSource)
                {
                    _originalItems.Add(item);
                }
            }
            _filteredItems = new List<object>(_originalItems);
            Debug.WriteLine($"PopupComboBox: UpdateOriginalItems - OriginalItems count: {_originalItems.Count}");
        }

        private void HandlePopupComboBox()
        {
            // The text entry for typing
            _textEntry = new Entry
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = Colors.Transparent,
            };
            _textEntry.SetBinding(Entry.TextColorProperty, new Binding(nameof(TextColor), BindingMode.OneWay, source: this));
            _textEntry.SetBinding(Entry.FontSizeProperty, new Binding(nameof(TextSize), BindingMode.OneWay, source: this));
            _textEntry.SetBinding(Entry.PlaceholderProperty, new Binding(nameof(Placeholder), BindingMode.OneWay, source: this));
            _textEntry.SetBinding(Entry.IsReadOnlyProperty, new Binding(nameof(IsEditable), BindingMode.OneWay, source: this, converter: new InvertBoolConverter()));

            _textEntry.TextChanged += OnTextChanged;
            _textEntry.Focused += OnEntryFocused;
            _textEntry.Unfocused += OnEntryUnfocused;
            _textEntry.Completed += OnEditingCompleted;

            // The up/down image
            _arrowImage = new Image
            {
                Source = DropdownClosedImageSource,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 0, 0, 0),
            };

            // Main container for the entry and icon
            var mainButtonLayout = new Grid
            {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill,
            };
            mainButtonLayout.SizeChanged += (_, _) =>
            {
                var dropdownWidth = DropDownWidth > 0 ? DropDownWidth : mainButtonLayout.Width;
                AbsoluteLayout.SetLayoutBounds(_popupContainer, new Rect(0, 0, dropdownWidth, DropDownHeight));
            };

            mainButtonLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            mainButtonLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainButtonLayout.Children.Add(_textEntry);
            mainButtonLayout.SetColumn(_textEntry, 0);
            mainButtonLayout.Children.Add(_arrowImage);
            mainButtonLayout.SetColumn(_arrowImage, 1);

            // Collection view for dropdown items
            var itemCollectionView = new CollectionView
            {
                VerticalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0),
                SelectionMode = SelectionMode.Single,
                ItemsSource = _filteredItems,
                BackgroundColor = Colors.Transparent,
                ItemTemplate = new DataTemplate(() =>
                {
                    var stack = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Fill,
                    };

                    var boxView = new BoxView
                    {
                        Margin = new Thickness(0, 10, 0, 0),
                        Color = Colors.LightGray,
                        HeightRequest = .5
                    };

                    var label = new Label
                    {
                        Margin = new Thickness(10, 10, 10, 0),
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Fill,
                    };
                    label.SetBinding(Label.TextColorProperty, new Binding(nameof(DropdownTextColor), BindingMode.OneWay, source: this));
                    label.SetBinding(Label.FontSizeProperty, new Binding(nameof(TextSize), BindingMode.OneWay, source: this));
                    label.SetBinding(BackgroundColorProperty, new Binding(nameof(DropdownBackgroundColor), BindingMode.OneWay, source: this));
                    label.SetBinding(Label.TextProperty, new Binding("."));

                    stack.Add(label);
                    stack.Add(boxView);
                    return stack;
                }),
                EmptyView = new Label
                {
                    Text = "No matches found...",
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 10),
                },
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
            };

            itemCollectionView.SelectionChanged += OnItemSelected;

            // Setup popup container
            _popupContainer.Content = itemCollectionView;
            _popupContainer.IsVisible = false;
            _popupContainer.Margin = new Thickness(0);
            _popupContainer.Padding = new Thickness(0);
            _popupContainer.BackgroundColor = Colors.Transparent;
            _popupContainer.Stroke = Colors.Transparent;
            _popupContainer.StrokeThickness = 0;

            // Create popup
            var bounds = GetControlBounds();
            var popupWidth = DropDownWidth > 0 ? DropDownWidth : bounds.Width;
            var popupHeight = DropDownHeight;
            var scrollOffset = CheckAndGetScrollOffset();
            _popupContainer.WidthRequest = popupWidth;

            _popup = new Popup
            {
                Content = _popupContainer,
                WidthRequest = popupWidth,
                HeightRequest = popupHeight,
                CanBeDismissedByTappingOutsideOfPopup = true,
                Margin = new Thickness(bounds.X * 0.5, bounds.Y * 0.5 + bounds.Height - scrollOffset, 0, 0),
                Padding = 0,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.Start,
                BackgroundColor = Colors.Transparent
            };

            _popup.Closed += (sender, args) =>
            {
                _popupContainer.IsVisible = false;
                SetDropDownImage(_popupContainer.IsVisible);
            };

            // Set main content
            Content = mainButtonLayout;
            Padding = new Thickness(10);

            // Add tap gesture for arrow
            var togglePopupGesture = new TapGestureRecognizer();
            togglePopupGesture.Tapped += (_, _) => TogglePopup();
            _arrowImage.GestureRecognizers.Add(togglePopupGesture);

            // Property change handlers
            PropertyChanged += OnPropertyChanged;

            UpdateOriginalItems();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.WriteLine($"PopupComboBox: OnTextChanged - NewTextValue: {e.NewTextValue}, OldTextValue: {e.OldTextValue}");
            if (_isUpdatingFromSelection) return;

            TextChanged?.Invoke(this, e);

            if (string.IsNullOrEmpty(e.NewTextValue) || e.NewTextValue.Length < MinimumSearchLength)
            {
                _filteredItems = new List<object>(_originalItems);
                if (_popup?.Parent != null && _popupContainer.IsVisible)
                {
                    _popup.CloseAsync();
                }
            }
            else
            {
                FilterItems(e.NewTextValue);
                if (!_popupContainer.IsVisible && _filteredItems.Any())
                {
                    ShowPopup();
                }
            }

            RefreshCollectionView();
        }

        private void OnEntryFocused(object sender, FocusEventArgs e)
        {
            Debug.WriteLine($"PopupComboBox: OnEntryFocused - IsEditable: {IsEditable}, TextEntry.Text: {_textEntry.Text}, FilteredItems.Any(): {_filteredItems.Any()}");
            if (IsEditable && _originalItems.Any()) // Check original items, as filtered might be empty if no text
            {
                // If text is empty, show all original items
                if (string.IsNullOrEmpty(_textEntry.Text))
                {
                    _filteredItems = new List<object>(_originalItems);
                    RefreshCollectionView();
                }
                ShowPopup();
            }
        }

        private void OnEntryUnfocused(object sender, FocusEventArgs e)
        {
            // Don't close immediately to allow for item selection
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(200);

                // Close only if still visible
                if (_popupContainer.IsVisible)
                    await SafeClosePopupAsync();
            });
        }
        private void OnEditingCompleted(object sender, EventArgs e)
        {
            EditingCompleted?.Invoke(this, e);

            // Try to find exact match
            var exactMatch = _originalItems.FirstOrDefault(item =>
                item?.ToString()?.Equals(_textEntry.Text, StringComparison.OrdinalIgnoreCase) == true);

            if (exactMatch != null)
            {
                _isUpdatingFromSelection = true;
                SelectedItem = exactMatch;
                _isUpdatingFromSelection = false;
            }
        }

        private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
        {
          
            if (e?.CurrentSelection is not { Count: > 0 } || e.CurrentSelection[0] is not { } selectedItem)
            {
                return;
            }

            // --- IMMEDIATE UI UPDATES ---
            _isUpdatingFromSelection = true;
            _textEntry.Text = selectedItem.ToString() ?? string.Empty;
            _isUpdatingFromSelection = false;

            // Close the popup now, while the control is still valid.
            if (_popup?.Parent != null && _popupContainer.IsVisible)
            {
              //  _popup.CloseAsync();
            }

            // REMOVED THE SUSPECTED LINE:
            // This line was likely causing the conflict. By removing it,
            // we allow the selection process to complete without interruption.
            // if (sender is CollectionView cv)
            // {
            //     cv.SelectedItem = null;
            // }

            // --- DELAYED DATA UPDATE ---
            // Give the UI a moment to settle before triggering the ViewModel.
            await Task.Delay(50);

            // Now, safely update the ViewModel property.
            SelectedItem = selectedItem;
        }
        private async Task SafeClosePopupAsync()
        {
            if (_popup == null)
                return;

            if (_popup.Parent == null) // already closed
                return;

            try
            {
                await _popup.CloseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Popup close error: {ex.Message}");
            }
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedItem):
                    if (!_isUpdatingFromSelection)
                    {
                        _isUpdatingFromSelection = true;
                        _textEntry.Text = SelectedItem?.ToString() ?? string.Empty;
                        _isUpdatingFromSelection = false;
                    }
                    break;

                case nameof(DropdownImageTint):
                    SetDropDownImage(_popupContainer.IsVisible);
                    break;

                case nameof(DropdownShadow):
                    if (DropdownShadow)
                    {
                        _popupContainer.Shadow = new Shadow
                        {
                            Opacity = 0.25f,
                            Offset = new Point(5, 5),
                            Radius = 1
                        };
                    }
                    else _popupContainer.Shadow = null!;
                    break;
            }
        }

        private void FilterItems(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                _filteredItems = new List<object>(_originalItems);
                return;
            }

            _filteredItems = _originalItems.Where(item =>
                item?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        private void RefreshCollectionView()
        {
            if (_popupContainer.Content is CollectionView cv)
            {
                cv.ItemsSource = null;
                cv.ItemsSource = _filteredItems;
            }
        }

        private void ShowPopup()
        {
            Debug.WriteLine("PopupComboBox: ShowPopup called.");
            if (_isToggling || (_popup?.Parent != null && _popupContainer.IsVisible))
            {
                Debug.WriteLine("PopupComboBox: ShowPopup - Already toggling or popup visible. Returning.");
                return;
            }

            _isToggling = true;

            if (_popup != null)
            {
                var bounds = GetControlBounds();
                var scrollOffset = CheckAndGetScrollOffset();
                var popupWidth = DropDownWidth > 0 ? DropDownWidth : bounds.Width;

                _popupContainer.WidthRequest = popupWidth;
                _popup.WidthRequest = popupWidth;
                _popup.Margin = new Thickness(bounds.X * 0.5, bounds.Y * 0.5 + bounds.Height - scrollOffset, 0, 0);

                if (Application.Current?.Windows[0].Page is Page currentPage)
                {
                    currentPage.ShowPopup(_popup, new PopupOptions
                    {
                        PageOverlayColor = Colors.Transparent,
                        Shape = new Rectangle
                        {
                            StrokeThickness = 0,
                            Stroke = Colors.Transparent
                        }
                    });
                }

                _popupContainer.IsVisible = true;
            }

            SetDropDownImage(_popupContainer.IsVisible);
            _isToggling = false;
            Debug.WriteLine("PopupComboBox: ShowPopup - Exiting.");
        }

        private void TogglePopup()
        {
            Debug.WriteLine("PopupComboBox: TogglePopup called.");
            if (_isToggling)
            {
                Debug.WriteLine("PopupComboBox: TogglePopup - Already toggling. Returning.");
                return;
            }

            if (_popup?.Parent != null && _popupContainer.IsVisible)
            {
                _popup.CloseAsync();
            }
            else
            {
                // Show all items when opening via arrow click
                _filteredItems = new List<object>(_originalItems);
                RefreshCollectionView();
                ShowPopup();
            }
        }

        private static void CornerRadiusChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is PopupComboBox container) container.UpdateCornerRadius();
        }

        private void UpdateCornerRadius()
        {
            ArgumentNullException.ThrowIfNull(_popupContainer);
            _popupContainer.StrokeShape = new RoundRectangle { CornerRadius = DropdownCornerRadius, StrokeThickness = 0, Stroke = Colors.Transparent };
        }

        private void SetDropDownImage(bool isOpen)
        {
            try
            {
                _arrowImage.Source = isOpen ? DropdownOpenImageSource : DropdownClosedImageSource;
                if (DropdownImageTint != null)
                {
                    _arrowImage.Behaviors.Clear();
                    _arrowImage.Behaviors.Add(new IconTintColorBehavior { TintColor = DropdownImageTint });
                }
            }
            catch
            {
                Debug.WriteLine($"Error setting dropdown image source: {(isOpen ? DropdownOpenImageSource : DropdownClosedImageSource)}");
            }
        }

        private Rect GetControlBounds()
        {
            var element = this;
            var x = element.X;
            var y = element.Y;

            var parent = element.Parent as VisualElement;
            while (parent != null)
            {
                x += parent.X;
                y += parent.Y;
                parent = parent.Parent as VisualElement;
            }

            return new Rect(x, y, Width, Height);
        }

        private double CheckAndGetScrollOffset()
        {
            var yOffset = .0;
            var parent = Parent as VisualElement;
            while (parent != null)
            {
                if (parent is ScrollView scroll)
                    yOffset += scroll.ScrollY * 0.5;

                parent = parent.Parent as VisualElement;
            }
            return yOffset;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _popup is not null)
                {
                    _popup.CloseAsync();
                }
                _disposed = true;
            }
        }
    }

    // Helper converter for inverting boolean values
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }
    }
}