#if WINDOWS
using Microsoft.Maui.Handlers;
using Maui.ComboBox.Interfaces;
using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.Maui.Platform;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Maui.ComboBox.Platforms.Windows
{
    public class NativeSpinnerHandler : ViewHandler<INativeSpinner, Microsoft.UI.Xaml.Controls.ComboBox>
    {
        private bool _isUpdatingSelection;

        new private static IPropertyMapper<INativeSpinner, NativeSpinnerHandler> ViewMapper = new PropertyMapper<INativeSpinner, NativeSpinnerHandler>(ViewHandler.ViewMapper)
        {
            [nameof(INativeSpinner.ItemsSource)] = MapItemsSource,
            [nameof(INativeSpinner.SelectedIndex)] = MapSelectedIndex,
            [nameof(INativeSpinner.SelectedItem)] = MapSelectedItem,
            [nameof(INativeSpinner.Placeholder)] = MapPlaceholder,
            [nameof(INativeSpinner.TextColor)] = MapTextColor,
            [nameof(INativeSpinner.FontSize)] = MapFontSize,
            [nameof(INativeSpinner.IsEnabled)] = MapIsEnabled,
        };

        new private static CommandMapper<INativeSpinner, NativeSpinnerHandler> ViewCommandMapper = new CommandMapper<INativeSpinner, NativeSpinnerHandler>(ViewHandler.ViewCommandMapper);

        public NativeSpinnerHandler() : base(ViewMapper, ViewCommandMapper) { }

        protected override Microsoft.UI.Xaml.Controls.ComboBox CreatePlatformView()
        {
            return new Microsoft.UI.Xaml.Controls.ComboBox();
        }

        protected override void ConnectHandler(Microsoft.UI.Xaml.Controls.ComboBox platformView)
        {
            base.ConnectHandler(platformView);
            platformView.SelectionChanged += OnSelectionChanged;
        }

        protected override void DisconnectHandler(Microsoft.UI.Xaml.Controls.ComboBox platformView)
        {
            platformView.SelectionChanged -= OnSelectionChanged;
            base.DisconnectHandler(platformView);
        }

        private void OnSelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection || VirtualView == null)
                return;

            VirtualView.SelectedItem = PlatformView.SelectedItem;
        }

        public static void MapItemsSource(NativeSpinnerHandler handler, INativeSpinner spinner)
        {
            handler.PlatformView.ItemsSource = spinner.ItemsSource;
        }

        public static void MapSelectedIndex(NativeSpinnerHandler handler, INativeSpinner spinner)
        {
            handler._isUpdatingSelection = true;
            handler.PlatformView.SelectedIndex = spinner.SelectedIndex;
            handler._isUpdatingSelection = false;
        }

        public static void MapSelectedItem(NativeSpinnerHandler handler, INativeSpinner spinner)
        {
            handler._isUpdatingSelection = true;
            handler.PlatformView.SelectedItem = spinner.SelectedItem;
            handler._isUpdatingSelection = false;
        }

        public static void MapPlaceholder(NativeSpinnerHandler handler, INativeSpinner spinner)
        {
            handler.PlatformView.PlaceholderText = spinner.Placeholder;
        }

        public static void MapTextColor(NativeSpinnerHandler handler, INativeSpinner spinner)
        {
            handler.PlatformView.Foreground = new SolidColorBrush(spinner.TextColor.ToWindowsColor());
        }

        public static void MapFontSize(NativeSpinnerHandler handler, INativeSpinner spinner)
        {
            handler.PlatformView.FontSize = spinner.FontSize;
        }

        public static void MapIsEnabled(NativeSpinnerHandler handler, INativeSpinner spinner)
        {
            handler.PlatformView.IsEnabled = spinner.IsEnabled;
        }
    }
}
#endif