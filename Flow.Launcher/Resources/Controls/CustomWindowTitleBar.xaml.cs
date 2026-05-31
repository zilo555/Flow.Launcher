using System;
using System.Windows.Controls;
using System.Windows;

namespace Flow.Launcher.Resources.Controls
{
    public partial class CustomWindowTitleBar : UserControl
    {
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                name: nameof(IconSource),
                propertyType: typeof(string),
                ownerType: typeof(CustomWindowTitleBar),
                typeMetadata: new PropertyMetadata(defaultValue: "/Images/app.png")
            );

        public static readonly DependencyProperty MinimizeButtonVisibilityProperty =
            DependencyProperty.Register(
                name: nameof(MinimizeButtonVisibility),
                propertyType: typeof(Visibility),
                ownerType: typeof(CustomWindowTitleBar),
                typeMetadata: new PropertyMetadata(defaultValue: Visibility.Visible)
            );

        public static readonly DependencyProperty MaximizeButtonVisibilityProperty =
            DependencyProperty.Register(
                name: nameof(MaximizeButtonVisibility),
                propertyType: typeof(Visibility),
                ownerType: typeof(CustomWindowTitleBar),
                typeMetadata: new PropertyMetadata(defaultValue: Visibility.Visible)
            );

        public static readonly DependencyProperty RestoreButtonVisibilityProperty =
            DependencyProperty.Register(
                name: nameof(RestoreButtonVisibility),
                propertyType: typeof(Visibility),
                ownerType: typeof(CustomWindowTitleBar),
                typeMetadata: new PropertyMetadata(defaultValue: Visibility.Hidden)
            );

        public static readonly DependencyProperty CloseButtonVisibilityProperty =
            DependencyProperty.Register(
                name: nameof(CloseButtonVisibility),
                propertyType: typeof(Visibility),
                ownerType: typeof(CustomWindowTitleBar),
                typeMetadata: new PropertyMetadata(defaultValue: Visibility.Visible)
            );

        public event RoutedEventHandler MinimizeButtonClick;
        public event RoutedEventHandler MaximizeRestoreButtonClick;
        public event RoutedEventHandler CloseButtonClick;

        private Window _hostWindow;
        private WindowState _lastNonMinimizedWindowState = WindowState.Normal;

        public CustomWindowTitleBar()
        {
            InitializeComponent();
            Loaded += CustomWindowTitleBar_Loaded;
            Unloaded += CustomWindowTitleBar_Unloaded;
        }
        
        public string IconSource
        {
            get => (string)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public Visibility MinimizeButtonVisibility
        {
            get => (Visibility)GetValue(MinimizeButtonVisibilityProperty);
            set => SetValue(MinimizeButtonVisibilityProperty, value);
        }

        public Visibility MaximizeButtonVisibility
        {
            get => (Visibility)GetValue(MaximizeButtonVisibilityProperty);
            set => SetValue(MaximizeButtonVisibilityProperty, value);
        }

        public Visibility RestoreButtonVisibility
        {
            get => (Visibility)GetValue(RestoreButtonVisibilityProperty);
            set => SetValue(RestoreButtonVisibilityProperty, value);
        }

        public Visibility CloseButtonVisibility
        {
            get => (Visibility)GetValue(CloseButtonVisibilityProperty);
            set => SetValue(CloseButtonVisibilityProperty, value);
        }

        private void CustomWindowTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            AttachToHostWindow();
            RefreshMaximizeRestoreButton();
        }

        private void CustomWindowTitleBar_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachFromHostWindow();
        }

        private void AttachToHostWindow()
        {
            var window = Window.GetWindow(this);
            if (_hostWindow == window)
            {
                return;
            }

            DetachFromHostWindow();

            _hostWindow = window;
            if (_hostWindow == null)
            {
                return;
            }

            if (_hostWindow.WindowState != WindowState.Minimized)
            {
                _lastNonMinimizedWindowState = _hostWindow.WindowState;
            }

            _hostWindow.StateChanged += HostWindow_StateChanged;
            _hostWindow.Activated += HostWindow_Activated;
            _hostWindow.Closed += HostWindow_Closed;
        }

        private void DetachFromHostWindow()
        {
            if (_hostWindow == null)
            {
                return;
            }

            _hostWindow.StateChanged -= HostWindow_StateChanged;
            _hostWindow.Activated -= HostWindow_Activated;
            _hostWindow.Closed -= HostWindow_Closed;
            _hostWindow = null;
        }

        private void HostWindow_StateChanged(object sender, EventArgs e)
        {
            if (_hostWindow == null)
            {
                return;
            }

            if (_hostWindow.WindowState != WindowState.Minimized)
            {
                _lastNonMinimizedWindowState = _hostWindow.WindowState;
            }

            RefreshMaximizeRestoreButton();
        }

        private void HostWindow_Activated(object sender, EventArgs e)
        {
            if (_hostWindow == null)
            {
                return;
            }

            // Band-aid fix: Rare edge case where Alt+Tab activates the window but doesn't trigger StateChanged.
            if (_hostWindow.WindowState == WindowState.Minimized)
            {
                _hostWindow.WindowState = _lastNonMinimizedWindowState;
            }
        }

        private void HostWindow_Closed(object sender, EventArgs e)
        {
            DetachFromHostWindow();
        }

        private void RefreshMaximizeRestoreButton()
        {
            if (_hostWindow?.WindowState == WindowState.Maximized)
            {
                MaximizeButtonVisibility = Visibility.Hidden;
                RestoreButtonVisibility = Visibility.Visible;
            }
            else
            {
                MaximizeButtonVisibility = Visibility.Visible;
                RestoreButtonVisibility = Visibility.Hidden;
            }
        }

        private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            MinimizeButtonClick?.Invoke(this, e);

            if (_hostWindow == null)
            {
                return;
            }

            _hostWindow.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            MaximizeRestoreButtonClick?.Invoke(this, e);

            if (_hostWindow == null)
            {
                return;
            }

            _hostWindow.WindowState = _hostWindow.WindowState switch
            {
                WindowState.Maximized => WindowState.Normal,
                _ => WindowState.Maximized
            };
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            CloseButtonClick?.Invoke(this, e);

            if (_hostWindow == null)
            {
                return;
            }

            _hostWindow.Close();
        }
    }
}
