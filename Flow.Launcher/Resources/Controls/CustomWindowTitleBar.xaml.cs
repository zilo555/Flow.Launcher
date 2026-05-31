using System;
using System.Windows.Controls;
using System.Windows;

namespace Flow.Launcher.Resources.Controls
{
    public partial class CustomWindowTitleBar : UserControl
    {
        public sealed class WindowStateChangedEventArgs : EventArgs
        {
            public WindowStateChangedEventArgs(WindowState previousState, WindowState currentState)
            {
                PreviousState = previousState;
                CurrentState = currentState;
            }

            public WindowState PreviousState { get; }

            public WindowState CurrentState { get; }
        }

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                name: nameof(IconSource),
                propertyType: typeof(string),
                ownerType: typeof(CustomWindowTitleBar),
                typeMetadata: new PropertyMetadata(defaultValue: "/Images/app.png")
            );

        public event RoutedEventHandler MinimizeButtonClick;
        public event RoutedEventHandler MaximizeRestoreButtonClick;
        public event RoutedEventHandler CloseButtonClick;
        public event EventHandler<WindowStateChangedEventArgs> LastNonMinimizedWindowStateChanged;

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

        public WindowState LastNonMinimizedWindowState {
            get => _lastNonMinimizedWindowState;
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
                UpdateLastNonMinimizedWindowState(_hostWindow.WindowState);
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
                UpdateLastNonMinimizedWindowState(_hostWindow.WindowState);
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

        private void UpdateLastNonMinimizedWindowState(WindowState state)
        {
            if (state == WindowState.Minimized || _lastNonMinimizedWindowState == state)
            {
                return;
            }

            var previousState = _lastNonMinimizedWindowState;
            _lastNonMinimizedWindowState = state;
            LastNonMinimizedWindowStateChanged?.Invoke(this,
                new WindowStateChangedEventArgs(previousState, _lastNonMinimizedWindowState)
            );
        }

        private void RefreshMaximizeRestoreButton()
        {
            if (_hostWindow?.WindowState == WindowState.Maximized)
            {
                MaximizeButton.Visibility = Visibility.Hidden;
                RestoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                MaximizeButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Hidden;
            }
        }

        private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            MinimizeButtonClick?.Invoke(this, e);

            if (e.Handled)
            {
                return;
            }

            if (_hostWindow == null)
            {
                return;
            }

            _hostWindow.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            MaximizeRestoreButtonClick?.Invoke(this, e);

            if (e.Handled)
            {
                return;
            }

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

            if (e.Handled)
            {
                return;
            }

            if (_hostWindow == null)
            {
                return;
            }

            _hostWindow.Close();
        }
    }
}
