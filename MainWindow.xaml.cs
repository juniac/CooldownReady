using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CooldownReady.Controls;
using CooldownReady.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace CooldownReady
{
    /// <summary>
    /// 메인 윈도우. 키 바인딩 행 목록·키보드 훅·키별 카운트다운을 조율합니다.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const int WindowWidth = 450;
        private const int SettingRowHeight = 150;
        private const int CountdownRowHeight = 55;
        private const int MinExpandedHeight = 620;
        private const int MaxExpandedHeight = 900;
        private const int MinFoldedHeight = 320;
        private const int MaxFoldedHeight = 640;

        private readonly AppSettings _settings;
        private readonly LocalizationService _localization;
        private readonly AlertSoundService _soundService;
        private GlobalKeyboardHook? _keyboardHook;

        private readonly List<KeyBindingRow> _rows = new();
        private readonly Dictionary<KeyBindingSettings, CountdownDisplay> _displays = new();
        private readonly Dictionary<KeyBindingSettings, CooldownRuntime> _runtimes = new();
        private IReadOnlyList<string> _soundFileNames = Array.Empty<string>();

        private bool _isRunning = false;
        private AppWindow? _appWindow;
        private bool _isSettingsFolded = false;
        private bool _isApplyingLanguageSelection = false;
        private bool _renderTickerHooked = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                _settings = SettingsService.Load();
                _localization = new LocalizationService(_settings.SelectedLanguage);
                _soundService = new AlertSoundService();

                InitializeLanguageComboBox();

                // 윈도우 크기·아이콘 설정 - Activated 이벤트에서 처리
                this.Activated += MainWindow_Activated;

                // 키보드 훅 초기화 (선택적이므로 실패해도 계속 진행)
                try
                {
                    _keyboardHook = new GlobalKeyboardHook();
                    _keyboardHook.SetDispatcherQueue(this.DispatcherQueue);
                    _keyboardHook.KeyPressed += OnKeyPressed;
                }
                catch (Exception ex)
                {
                    ErrorLogger.Log("GlobalKeyboardHook 초기화 오류", ex);
                }

                AlwaysOnTopToggleButton.IsChecked = _settings.AlwaysOnTop;

                // 저장된 바인딩으로 행 구성 (없으면 기본 행 하나)
                if (_settings.Bindings.Count == 0)
                {
                    _settings.Bindings.Add(new KeyBindingSettings());
                }
                foreach (var binding in _settings.Bindings)
                {
                    AddRow(binding);
                }

                ApplyLocalization();
                _ = LoadSoundListAsync();

                this.Closed += MainWindow_Closed;
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("MainWindow Constructor Exception", ex);
                throw;
            }
        }

        #region 키 바인딩 행 관리

        private void AddKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var binding = new KeyBindingSettings();

            // 시간 설정은 마지막 행의 입력값을 그대로 이어받는다
            var lastBinding = _settings.Bindings.LastOrDefault();
            if (lastBinding != null)
            {
                binding.IntervalSecond = lastBinding.IntervalSecond;
                binding.AlertSecond = lastBinding.AlertSecond;
            }

            _settings.Bindings.Add(binding);
            var row = AddRow(binding);
            row.ApplyLocalization(_localization);
            row.SetSoundOptions(_soundFileNames);
            UpdateWindowHeight();
        }

        private KeyBindingRow AddRow(KeyBindingSettings binding)
        {
            var row = new KeyBindingRow(binding);
            row.RemoveRequested += OnRowRemoveRequested;
            row.KeyChanged += r => UpdateCountdownKeyName(r.Binding);
            row.EnabledChanged += OnRowEnabledChanged;
            row.ShowMillisecondsChanged += OnRowShowMillisecondsChanged;
            row.TimingChanged += OnRowTimingChanged;
            row.SoundPreviewRequested += fileName => _ = _soundService.PlayAsync(fileName);

            _rows.Add(row);
            BindingRowsPanel.Children.Add(row);

            var display = new CountdownDisplay();
            display.SetKeyName(binding.KeyName);
            display.ShowMilliseconds = binding.ShowMilliseconds;
            display.Reset();
            display.SetAlertConfig(binding.IntervalSecond, binding.AlertSecond);
            display.Opacity = binding.Enabled ? 1.0 : 0.4;
            _displays[binding] = display;
            CountdownListPanel.Children.Add(display);

            return row;
        }

        private void OnRowRemoveRequested(KeyBindingRow row)
        {
            _rows.Remove(row);
            BindingRowsPanel.Children.Remove(row);
            _settings.Bindings.Remove(row.Binding);

            if (_runtimes.Remove(row.Binding, out var runtime))
            {
                runtime.Stop();
            }
            if (_displays.Remove(row.Binding, out var display))
            {
                CountdownListPanel.Children.Remove(display);
            }

            UpdateWindowHeight();
        }

        private void UpdateCountdownKeyName(KeyBindingSettings binding)
        {
            if (_displays.TryGetValue(binding, out var display))
            {
                display.SetKeyName(binding.KeyName);
            }
        }

        private void OnRowEnabledChanged(KeyBindingRow row)
        {
            var binding = row.Binding;

            if (_displays.TryGetValue(binding, out var display))
            {
                display.Opacity = binding.Enabled ? 1.0 : 0.4;
            }

            // 모니터링 중 비활성화되면 해당 키의 카운트다운을 멈춘다
            if (!binding.Enabled && _runtimes.TryGetValue(binding, out var runtime))
            {
                runtime.Stop();
            }
        }

        private void OnRowShowMillisecondsChanged(KeyBindingRow row)
        {
            if (_displays.TryGetValue(row.Binding, out var display))
            {
                display.ShowMilliseconds = row.Binding.ShowMilliseconds;
            }
        }

        private void OnRowTimingChanged(KeyBindingRow row)
        {
            if (_displays.TryGetValue(row.Binding, out var display))
            {
                display.SetAlertConfig(row.Binding.IntervalSecond, row.Binding.AlertSecond);
            }
        }

        #endregion

        #region 설정

        private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.AlwaysOnTop = AlwaysOnTopToggleButton.IsChecked ?? false;
            _settings.SelectedLanguage = _localization.CurrentLanguage;
            SettingsService.Save(_settings);
            await ShowMessageDialogAsync("DialogNoticeTitle", "SettingsSavedMessage");
        }

        #endregion

        #region 언어

        private void InitializeLanguageComboBox()
        {
            _isApplyingLanguageSelection = true;
            LanguageComboBox.Items.Clear();
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "한국어", Tag = LocalizationService.Korean });
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = LocalizationService.English });
            LanguageComboBox.SelectedIndex = _localization.CurrentLanguage == LocalizationService.Korean ? 0 : 1;
            _isApplyingLanguageSelection = false;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingLanguageSelection)
                return;

            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string language)
            {
                _localization.SetLanguage(language);
                ApplyLocalization();

                _settings.SelectedLanguage = _localization.CurrentLanguage;
                SettingsService.Save(_settings);
            }
        }

        private void ApplyLocalization()
        {
            AutomationProperties.SetName(LanguageComboBox, _localization.GetString("LanguageLabel"));
            SaveSettingsButton.Content = _localization.GetString("SaveSettingsButton");
            ToolTipService.SetToolTip(AlwaysOnTopToggleButton, _localization.GetString("AlwaysOnTopToolTip"));
            BindingsGroup.Header = _localization.GetString("MonitoringKeyLabel");
            AddKeyButton.Content = _localization.GetString("AddKeyButton");
            RemainingTimeLabel.Text = _localization.GetString("RemainingTimeLabel");
            FoldSettingsButton.Content = _localization.GetString(_isSettingsFolded ? "ShowSettingsButton" : "HideSettingsButton");
            StartStopButton.Content = _localization.GetString(_isRunning ? "StopButton" : "StartButton");

            foreach (var row in _rows)
            {
                row.ApplyLocalization(_localization);
            }
        }

        #endregion

        /// <summary>
        /// 빈 영역을 클릭하면 입력 상자의 포커스를 해제한다.
        /// (입력 컨트롤 위 클릭은 해당 컨트롤이 이벤트를 소비하므로 여기까지 오지 않는다)
        /// </summary>
        private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FocusSink.Focus(FocusState.Programmatic);
        }

        #region 모니터링 / 카운트다운

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                StartMonitoring();
            }
            else
            {
                StopMonitoring();
            }
        }

        private void StartMonitoring()
        {
            var bindingsWithKey = _rows.Select(r => r.Binding).Where(b => b.Enabled && b.TargetKeyCode != 0).ToList();
            if (bindingsWithKey.Count == 0)
            {
                _ = ShowMessageDialogAsync("DialogErrorTitle", "MissingKeyMessage");
                return;
            }

            var validBindings = bindingsWithKey.Where(b => b.IntervalSecond > 0).ToList();
            if (validBindings.Count == 0)
            {
                _ = ShowMessageDialogAsync("DialogErrorTitle", "MissingCooldownMessage");
                return;
            }

            foreach (var binding in validBindings)
            {
                _runtimes[binding] = new CooldownRuntime(
                    binding,
                    _displays[binding],
                    fileName => _ = _soundService.PlayAsync(fileName));
            }

            _isRunning = true;
            StartStopButton.Content = _localization.GetString("StopButton");
            StartStopButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Red);

            _keyboardHook?.Start();
        }

        private void StopMonitoring()
        {
            _isRunning = false;
            StartStopButton.Content = _localization.GetString("StartButton");
            StartStopButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Green);
            _keyboardHook?.Stop();
            StopRenderTicker();

            foreach (var runtime in _runtimes.Values)
            {
                runtime.Stop();
            }
            _runtimes.Clear();

            foreach (var display in _displays.Values)
            {
                display.Reset(); // 00:00으로 표시
            }
        }

        private void OnKeyPressed(int vkCode)
        {
            if (!_isRunning)
                return;

            // 훅은 좌/우 구분 modifier 코드를 주므로 통합 코드로 정규화해 비교
            int normalizedKeyCode = GlobalKeyboardHook.NormalizeKeyCode(vkCode);

            foreach (var runtime in _runtimes.Values)
            {
                if (!runtime.Binding.Enabled || runtime.Binding.TargetKeyCode != normalizedKeyCode)
                    continue;

                if (runtime.Binding.PreventDuplicateInput && runtime.IsCountingDown)
                    continue;

                runtime.StartCountdown();
            }

            EnsureRenderTicker();
        }

        /// <summary>
        /// 카운트다운 진행 중에만 매 렌더 프레임마다 모든 카운트다운을 갱신한다.
        /// ms 표시 여부와 무관하게 모든 진행바가 동일한 프레임레이트로 부드럽게 채워진다.
        /// </summary>
        private void EnsureRenderTicker()
        {
            if (!_renderTickerHooked)
            {
                CompositionTarget.Rendering += OnRenderTick;
                _renderTickerHooked = true;
            }
        }

        private void StopRenderTicker()
        {
            if (_renderTickerHooked)
            {
                CompositionTarget.Rendering -= OnRenderTick;
                _renderTickerHooked = false;
            }
        }

        private void OnRenderTick(object? sender, object e)
        {
            bool anyCounting = false;
            foreach (var runtime in _runtimes.Values)
            {
                runtime.Tick();
                anyCounting |= runtime.IsCountingDown;
            }

            if (!anyCounting)
            {
                StopRenderTicker();
            }
        }

        /// <summary>
        /// 키 바인딩 하나의 카운트다운 상태(남은 시간·알림 재생)를 관리합니다.
        /// 갱신은 MainWindow의 렌더 프레임 티커가 Tick()을 호출해 일어납니다.
        /// </summary>
        private sealed class CooldownRuntime
        {
            public KeyBindingSettings Binding { get; }

            private readonly CountdownDisplay _display;
            private readonly Action<string> _playSound;
            private DateTime _endTimeUtc;
            private TimeSpan _remaining;
            private TimeSpan _interval;
            private int _alertSec;
            private bool _alertPlayed;
            private bool _isCounting;

            public CooldownRuntime(KeyBindingSettings binding, CountdownDisplay display, Action<string> playSound)
            {
                Binding = binding;
                _display = display;
                _playSound = playSound;
            }

            public bool IsCountingDown => _isCounting;

            public void StartCountdown()
            {
                _interval = TimeSpan.FromSeconds(Binding.IntervalSecond);
                _alertSec = (int)Binding.AlertSecond;
                _remaining = _interval;
                _endTimeUtc = DateTime.UtcNow + _interval;
                _alertPlayed = false;
                _display.ShowMilliseconds = Binding.ShowMilliseconds;
                _isCounting = _interval.TotalSeconds > 0;
                UpdateDisplay(showProgress: _isCounting);
            }

            /// <summary>매 렌더 프레임마다 호출되어 남은 시간을 갱신합니다.</summary>
            public void Tick()
            {
                if (!_isCounting)
                    return;

                _remaining = _endTimeUtc - DateTime.UtcNow;

                if (_remaining <= TimeSpan.Zero)
                {
                    _isCounting = false;
                    _remaining = TimeSpan.Zero;

                    // 알림 시간이 0초면 종료 시점에 재생
                    if (!_alertPlayed)
                    {
                        _playSound(Binding.SelectedSoundFile);
                    }

                    _alertPlayed = false;
                    UpdateDisplay(showProgress: false); // 0초로 표시
                    return;
                }

                UpdateDisplay(showProgress: true);

                // 알림 시점에 사운드 재생
                if (_remaining.TotalSeconds <= _alertSec && !_alertPlayed)
                {
                    _playSound(Binding.SelectedSoundFile);
                    _alertPlayed = true;
                }
            }

            /// <summary>카운트다운을 멈추고 표시를 0초로 되돌립니다.</summary>
            public void Stop()
            {
                _isCounting = false;
                _remaining = TimeSpan.Zero;
                _interval = TimeSpan.Zero;
                _alertPlayed = false;
                UpdateDisplay(showProgress: false);
            }

            private void UpdateDisplay(bool showProgress)
            {
                _display.Update(_remaining, _interval, _alertSec, showProgress && _interval.TotalSeconds > 0);
            }
        }

        #endregion

        #region 알림 소리

        private async Task LoadSoundListAsync()
        {
            try
            {
                _soundFileNames = await _soundService.GetSoundFileNamesAsync();
                foreach (var row in _rows)
                {
                    row.SetSoundOptions(_soundFileNames);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("사운드 목록 로드 오류", ex);
            }
        }

        #endregion

        #region 윈도우 관리

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
                return;

            try
            {
                var appWindow = GetAppWindow();
                if (appWindow != null)
                {
                    // 주의: 창이 표시되기 전에 IsResizable을 먼저 바꾸면 창이 영영 표시되지 않는다.
                    // 반드시 Resize → IsResizable 순서를 지키고, Show()로 표시를 보장한다.
                    UpdateWindowHeight();

                    if (appWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.IsResizable = false;
                    }

                    appWindow.Show();

                    _ = SetWindowIconAsync(appWindow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"윈도우 초기 설정 오류: {ex.Message}");
            }

            // 한 번만 실행되도록 이벤트 제거
            this.Activated -= MainWindow_Activated;
        }

        /// <summary>
        /// 행 수에 맞춰 창 높이를 조절합니다. 상한을 넘으면 본문에 스크롤이 생깁니다.
        /// </summary>
        private void UpdateWindowHeight()
        {
            int rows = Math.Max(_rows.Count, 1);
            int height = _isSettingsFolded
                ? Math.Clamp(230 + rows * CountdownRowHeight, MinFoldedHeight, MaxFoldedHeight)
                : Math.Clamp(300 + rows * (SettingRowHeight + CountdownRowHeight), MinExpandedHeight, MaxExpandedHeight);

            try
            {
                GetAppWindow()?.Resize(new Windows.Graphics.SizeInt32(WindowWidth, height));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"윈도우 크기 조절 오류: {ex.Message}");
            }
        }

        private async Task SetWindowIconAsync(AppWindow appWindow)
        {
            try
            {
                string iconPath = AssetLocator.GetAssetsPath("Assets\\cooldown.ico");
                if (!File.Exists(iconPath))
                {
                    // 패키징된 앱인 경우 ms-appx URI 시도
                    var iconFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/cooldown.ico"));
                    iconPath = iconFile?.Path ?? "";
                }

                if (!string.IsNullOrEmpty(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"아이콘 설정 오류: {ex.Message}");
            }
        }

        private AppWindow? GetAppWindow()
        {
            if (_appWindow == null)
            {
                try
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    _appWindow = AppWindow.GetFromWindowId(windowId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AppWindow 가져오기 오류: {ex.Message}");
                }
            }

            return _appWindow;
        }

        private void FoldSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _isSettingsFolded = !_isSettingsFolded;

            BindingsGroup.Visibility = _isSettingsFolded ? Visibility.Collapsed : Visibility.Visible;
            FoldSettingsButton.Content = _localization.GetString(_isSettingsFolded ? "ShowSettingsButton" : "HideSettingsButton");
            UpdateWindowHeight();
        }

        private void AlwaysOnTopToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SetAlwaysOnTop(true);
        }

        private void AlwaysOnTopToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAlwaysOnTop(false);
        }

        private void SetAlwaysOnTop(bool alwaysOnTop)
        {
            try
            {
                if (GetAppWindow()?.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsAlwaysOnTop = alwaysOnTop;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"항상 위 설정 오류: {ex.Message}");
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            StopMonitoring();
            _keyboardHook?.Dispose();
            _soundService.Dispose();
        }

        #endregion

        private async Task ShowMessageDialogAsync(string titleKey, string contentKey)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = _localization.GetString(titleKey),
                Content = _localization.GetString(contentKey),
                CloseButtonText = _localization.GetString("DialogOk"),
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
