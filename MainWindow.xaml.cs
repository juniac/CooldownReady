using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.System.UserProfile;
using Windows.Storage;
using Windows.UI;
using Windows.Storage.Streams;

namespace CooldownReady
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private GlobalKeyboardHook? _keyboardHook;
        private DispatcherTimer? _countdownTimer;
        private bool _isRunning = false;
        private int _targetKeyCode = 0;
        private TimeSpan _remainingTime;
        private TimeSpan _intervalSec;
        private int _alertSec;
        private bool _alertPlayed = false;
        private MediaPlayer? _mediaPlayer;
        private string _selectedSoundFile = "1.mp3";
        private AppWindow? _appWindow;
        private bool _isLoadingSettings = true;
        private bool _isLoadingSounds = true;
        private bool _isSettingsFolded = false;
        private double _currentProgress = 0;
        private const string KoreanLanguage = "ko-KR";
        private const string EnglishLanguage = "en-US";
        private bool _isApplyingLanguageSelection = false;
        private bool _languageChangedByUser = false;
        private string _selectedLanguage = KoreanLanguage;
        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LocalizedText =
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [KoreanLanguage] = new Dictionary<string, string>
                {
                    ["LanguageLabel"] = "언어",
                    ["SaveSettingsButton"] = "설정 저장",
                    ["AlwaysOnTopToolTip"] = "항상 위",
                    ["MonitoringKeyLabel"] = "모니터링 키",
                    ["KeyPlaceholder"] = "여기를 눌러 키를 눌러주세요...",
                    ["NoKeySet"] = "아직 키가 설정되지 않았습니다",
                    ["SelectedKeyDisplay"] = "설정된 키: {0} (Virtual Key: {1})",
                    ["CooldownTimeLabel"] = "쿨다운 시간",
                    ["MinuteText"] = "분",
                    ["SecondText"] = "초",
                    ["AlertTimeLabel"] = "알림 시간 (초 전에 알림)",
                    ["AlertSoundLabel"] = "알림 소리 선택",
                    ["PreventDuplicateInputLabel"] = "카운트다운 중 중복 입력 방지",
                    ["RemainingTimeLabel"] = "남은 시간",
                    ["HideSettingsButton"] = "▲ 설정접기",
                    ["ShowSettingsButton"] = "▼ 설정열기",
                    ["StartButton"] = "시작",
                    ["StopButton"] = "중지",
                    ["DialogErrorTitle"] = "오류",
                    ["DialogNoticeTitle"] = "알림",
                    ["DialogOk"] = "확인",
                    ["MissingKeyMessage"] = "키보드 키를 먼저 설정해주세요.",
                    ["MissingCooldownMessage"] = "쿨다운 시간을 설정해주세요.",
                    ["SettingsSavedMessage"] = "설정이 저장되었습니다."
                },
                [EnglishLanguage] = new Dictionary<string, string>
                {
                    ["LanguageLabel"] = "Language",
                    ["SaveSettingsButton"] = "Save Settings",
                    ["AlwaysOnTopToolTip"] = "Always on top",
                    ["MonitoringKeyLabel"] = "Monitoring Key",
                    ["KeyPlaceholder"] = "Click here and press a key...",
                    ["NoKeySet"] = "No key has been set",
                    ["SelectedKeyDisplay"] = "Selected key: {0} (Virtual Key: {1})",
                    ["CooldownTimeLabel"] = "Cooldown Time",
                    ["MinuteText"] = "min",
                    ["SecondText"] = "sec",
                    ["AlertTimeLabel"] = "Alert Time (seconds before end)",
                    ["AlertSoundLabel"] = "Alert Sound",
                    ["PreventDuplicateInputLabel"] = "Prevent duplicate input during countdown",
                    ["RemainingTimeLabel"] = "Remaining Time",
                    ["HideSettingsButton"] = "▲ Hide Settings",
                    ["ShowSettingsButton"] = "▼ Show Settings",
                    ["StartButton"] = "Start",
                    ["StopButton"] = "Stop",
                    ["DialogErrorTitle"] = "Error",
                    ["DialogNoticeTitle"] = "Notice",
                    ["DialogOk"] = "OK",
                    ["MissingKeyMessage"] = "Set a monitoring key first.",
                    ["MissingCooldownMessage"] = "Set a cooldown time.",
                    ["SettingsSavedMessage"] = "Settings saved."
                }
            };

        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow 생성자 시작");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent 완료");
                _selectedLanguage = GetDefaultLanguage();
                InitializeLanguageComboBox();
                ApplyLocalization();
                
                // 윈도우 크기 설정 및 아이콘 설정 - Activated 이벤트에서 처리
                this.Activated += MainWindow_Activated;
                
                // DispatcherQueue 설정 (Window의 DispatcherQueue 사용)
                var dispatcherQueue = this.DispatcherQueue;
                System.Diagnostics.Debug.WriteLine("DispatcherQueue 가져오기 완료");
                
                // 키보드 훅 초기화
                try
                {
                    _keyboardHook = new GlobalKeyboardHook();
                    _keyboardHook.SetDispatcherQueue(dispatcherQueue);
                    _keyboardHook.KeyPressed += OnKeyPressed;
                    System.Diagnostics.Debug.WriteLine("GlobalKeyboardHook 초기화 완료");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GlobalKeyboardHook 초기화 오류: {ex.Message}");
                    // 키보드 훅은 선택적이므로 계속 진행
                }

                // 타이머 초기화
                _countdownTimer = new DispatcherTimer();
                _countdownTimer.Interval = TimeSpan.FromSeconds(1);
                _countdownTimer.Tick += CountdownTimer_Tick;
                System.Diagnostics.Debug.WriteLine("DispatcherTimer 초기화 완료");

                // 사운드 재생을 위한 MediaPlayer 초기화
                _mediaPlayer = new MediaPlayer();
                _mediaPlayer.AudioCategory = MediaPlayerAudioCategory.Alerts;
                System.Diagnostics.Debug.WriteLine("MediaPlayer 초기화 완료");

                // 사운드 파일 목록 로드
                _ = LoadSoundFilesAsync();

                // 설정값 로드
                _ = LoadSettingsAsync();

                // 초기 카운트다운 표시 (00:00)
                _remainingTime = TimeSpan.Zero;
                UpdateCountdownDisplay();

                // 윈도우 닫힘 이벤트 처리
                this.Closed += MainWindow_Closed;
                
                // 타이틀 바 아이콘 설정 (초기화 직후)
                _ = SetWindowIconAsync();
                
                System.Diagnostics.Debug.WriteLine("MainWindow 생성자 완료");
            }
            catch (Exception ex)
            {
                var errorMsg = $"MainWindow Constructor Exception: {ex.Message}\nStack Trace: {ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                
                // 파일로도 로그 남기기
                try
                {
                    var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CooldownReady", "error.log");
                    var logDir = System.IO.Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    File.AppendAllText(logPath, $"[{DateTime.Now}] {errorMsg}\n\n");
                }
                catch { }
                
                throw;
            }
        }

        private void InitializeLanguageComboBox()
        {
            _isApplyingLanguageSelection = true;
            LanguageComboBox.Items.Clear();
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "한국어", Tag = KoreanLanguage });
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = EnglishLanguage });
            LanguageComboBox.SelectedIndex = _selectedLanguage == KoreanLanguage ? 0 : 1;
            _isApplyingLanguageSelection = false;
        }

        private void SelectLanguage(string language)
        {
            string nextLanguage = NormalizeLanguage(language);
            _selectedLanguage = nextLanguage;

            _isApplyingLanguageSelection = true;
            for (int i = 0; i < LanguageComboBox.Items.Count; i++)
            {
                if (LanguageComboBox.Items[i] is ComboBoxItem item && item.Tag as string == nextLanguage)
                {
                    LanguageComboBox.SelectedIndex = i;
                    break;
                }
            }
            _isApplyingLanguageSelection = false;

            ApplyLocalization();
        }

        private string T(string key)
        {
            if (LocalizedText.TryGetValue(_selectedLanguage, out var selectedLanguage)
                && selectedLanguage.TryGetValue(key, out var selectedValue))
            {
                return selectedValue;
            }

            if (LocalizedText.TryGetValue(EnglishLanguage, out var fallbackLanguage)
                && fallbackLanguage.TryGetValue(key, out var fallbackValue))
            {
                return fallbackValue;
            }

            return key;
        }

        private string FormatText(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        private string NormalizeLanguage(string? language)
        {
            return TryGetSupportedLanguage(language) ?? EnglishLanguage;
        }

        private string? TryGetSupportedLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return null;

            string normalizedLanguage = language.Trim().Replace('_', '-');
            if (LocalizedText.ContainsKey(normalizedLanguage))
                return normalizedLanguage;

            string primaryLanguage = normalizedLanguage.Split('-')[0].ToLowerInvariant();
            return primaryLanguage switch
            {
                "ko" => KoreanLanguage,
                "en" => EnglishLanguage,
                _ => null
            };
        }

        private string GetDefaultLanguage()
        {
            try
            {
                foreach (string language in GlobalizationPreferences.Languages)
                {
                    string? supportedLanguage = TryGetSupportedLanguage(language);
                    if (supportedLanguage != null)
                        return supportedLanguage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"시스템 언어 로드 오류: {ex.Message}");
            }

            return TryGetSupportedLanguage(CultureInfo.CurrentUICulture.Name)
                ?? TryGetSupportedLanguage(CultureInfo.CurrentCulture.Name)
                ?? EnglishLanguage;
        }

        private string GetLanguageSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CooldownReady",
                "language.txt");
        }

        private string? LoadLanguageSettingFile()
        {
            try
            {
                string languageSettingsPath = GetLanguageSettingsPath();
                if (!File.Exists(languageSettingsPath))
                    return null;

                return NormalizeLanguage(File.ReadAllText(languageSettingsPath).Trim());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"언어 설정 파일 로드 오류: {ex.Message}");
                return null;
            }
        }

        private void SaveLanguageSettingFile()
        {
            try
            {
                string languageSettingsPath = GetLanguageSettingsPath();
                string? languageSettingsDirectory = Path.GetDirectoryName(languageSettingsPath);
                if (!string.IsNullOrEmpty(languageSettingsDirectory) && !Directory.Exists(languageSettingsDirectory))
                {
                    Directory.CreateDirectory(languageSettingsDirectory);
                }

                File.WriteAllText(languageSettingsPath, _selectedLanguage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"언어 설정 파일 저장 오류: {ex.Message}");
            }
        }

        private string GetPreventDuplicateInputSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CooldownReady",
                "prevent-duplicate-input.txt");
        }

        private bool? LoadPreventDuplicateInputSettingFile()
        {
            try
            {
                string settingsPath = GetPreventDuplicateInputSettingsPath();
                if (!File.Exists(settingsPath))
                    return null;

                string value = File.ReadAllText(settingsPath).Trim();
                return bool.TryParse(value, out bool preventDuplicateInput)
                    ? preventDuplicateInput
                    : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"중복 입력 방지 설정 파일 로드 오류: {ex.Message}");
                return null;
            }
        }

        private void SavePreventDuplicateInputSettingFile()
        {
            try
            {
                string settingsPath = GetPreventDuplicateInputSettingsPath();
                string? settingsDirectory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(settingsDirectory) && !Directory.Exists(settingsDirectory))
                {
                    Directory.CreateDirectory(settingsDirectory);
                }

                File.WriteAllText(settingsPath, (PreventDuplicateInputCheckBox.IsChecked ?? false).ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"중복 입력 방지 설정 파일 저장 오류: {ex.Message}");
            }
        }

        private void ApplyLocalization()
        {
            AutomationProperties.SetName(LanguageComboBox, T("LanguageLabel"));
            SaveSettingsButton.Content = T("SaveSettingsButton");
            ToolTipService.SetToolTip(AlwaysOnTopToggleButton, T("AlwaysOnTopToolTip"));
            MonitoringKeyLabel.Text = T("MonitoringKeyLabel");
            KeyboardHookTextBox.PlaceholderText = T("KeyPlaceholder");
            CooldownTimeLabel.Text = T("CooldownTimeLabel");
            IntervalMinuteBox.PlaceholderText = T("MinuteText");
            IntervalMinuteUnitText.Text = T("MinuteText");
            IntervalSecondBox.PlaceholderText = T("SecondText");
            IntervalSecondUnitText.Text = T("SecondText");
            AlertTimeLabel.Text = T("AlertTimeLabel");
            AlertSecondBox.PlaceholderText = T("SecondText");
            AlertSoundLabel.Text = T("AlertSoundLabel");
            PreventDuplicateInputCheckBox.Content = T("PreventDuplicateInputLabel");
            RemainingTimeLabel.Text = T("RemainingTimeLabel");
            FoldSettingsButton.Content = _isSettingsFolded ? T("ShowSettingsButton") : T("HideSettingsButton");
            StartStopButton.Content = _isRunning ? T("StopButton") : T("StartButton");
            UpdateKeyboardHookDisplayText();
        }

        private void UpdateKeyboardHookDisplayText()
        {
            if (_targetKeyCode == 0)
            {
                KeyboardHookDisplayText.Text = T("NoKeySet");
                return;
            }

            string keyName = KeyboardHookTextBox.Text;
            if (string.IsNullOrWhiteSpace(keyName))
            {
                keyName = GetKeyName((VirtualKey)_targetKeyCode);
                KeyboardHookTextBox.Text = keyName;
            }

            KeyboardHookDisplayText.Text = FormatText("SelectedKeyDisplay", keyName, _targetKeyCode);
        }

        private async Task ShowMessageDialogAsync(string titleKey, string contentKey)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = T(titleKey),
                Content = T(contentKey),
                CloseButtonText = T("DialogOk"),
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Assets 폴더의 파일 경로를 가져옵니다. 패키징된 앱과 직접 실행 파일 모두 지원합니다.
        /// </summary>
        private string GetAssetsPath(string relativePath)
        {
            try
            {
                // 패키징된 앱인지 확인
                var packageLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                if (packageLocation != null)
                {
                    var packagePath = Path.Combine(packageLocation.Path, relativePath);
                    if (File.Exists(packagePath) || Directory.Exists(packagePath))
                    {
                        return packagePath;
                    }
                }
            }
            catch
            {
                // 패키징되지 않은 앱인 경우
            }

            // 직접 실행 파일의 경우
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                // 단일 파일로 빌드된 경우
                assemblyDirectory = AppContext.BaseDirectory;
            }
            return Path.Combine(assemblyDirectory, relativePath);
        }

        /// <summary>
        /// Assets 폴더의 StorageFolder를 가져옵니다. 패키징된 앱과 직접 실행 파일 모두 지원합니다.
        /// </summary>
        private async Task<StorageFolder?> GetAssetsFolderAsync(string relativePath)
        {
            try
            {
                // 패키징된 앱인지 확인
                var packageLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                if (packageLocation != null)
                {
                    try
                    {
                        var folderPath = Path.Combine(packageLocation.Path, relativePath);
                        var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                        return folder;
                    }
                    catch
                    {
                        // 폴더를 찾을 수 없는 경우
                    }
                }
            }
            catch
            {
                // 패키징되지 않은 앱인 경우
            }

            // 직접 실행 파일의 경우
            var assetsPath = GetAssetsPath(relativePath);
            if (Directory.Exists(assetsPath))
            {
                try
                {
                    return await StorageFolder.GetFolderFromPathAsync(assetsPath);
                }
                catch
                {
                    // 폴더를 열 수 없는 경우
                }
            }

            return null;
        }

        private async Task SetWindowIconAsync()
        {
            try
            {
                await Task.Delay(200); // 윈도우 핸들이 준비될 때까지 대기
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);
                if (_appWindow != null)
                {
                    string iconPath = GetAssetsPath("Assets\\cooldown.ico");
                    if (File.Exists(iconPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"아이콘 파일 경로: {iconPath}");
                        _appWindow.SetIcon(iconPath);
                        System.Diagnostics.Debug.WriteLine("아이콘 설정 완료");
                    }
                    else
                    {
                        // ms-appx URI 시도 (패키징된 앱의 경우)
                        try
                        {
                            var iconUri = new Uri("ms-appx:///Assets/cooldown.ico");
                            var iconFile = await StorageFile.GetFileFromApplicationUriAsync(iconUri);
                            if (iconFile != null && !string.IsNullOrEmpty(iconFile.Path))
                            {
                                System.Diagnostics.Debug.WriteLine($"아이콘 파일 경로 (ms-appx): {iconFile.Path}");
                                _appWindow.SetIcon(iconFile.Path);
                                System.Diagnostics.Debug.WriteLine("아이콘 설정 완료");
                            }
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"아이콘 파일을 찾을 수 없습니다. 경로: {iconPath}, 오류: {ex2.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"아이콘 설정 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                try
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    _appWindow = AppWindow.GetFromWindowId(windowId);
                    if (_appWindow != null)
                    {
                        _appWindow.Resize(new Windows.Graphics.SizeInt32(450, 700));
                        // 윈도우 사이즈 고정
                        var presenter = _appWindow.Presenter as OverlappedPresenter;
                        if (presenter != null)
                        {
                            presenter.IsResizable = false;
                        }
                        
                        // 타이틀 바 아이콘 설정 (Activated에서도 한 번 더 시도)
                        try
                        {
                            var iconUri = new Uri("ms-appx:///Assets/cooldown.ico");
                            var iconFile = StorageFile.GetFileFromApplicationUriAsync(iconUri).GetAwaiter().GetResult();
                            if (iconFile != null && !string.IsNullOrEmpty(iconFile.Path))
                            {
                                System.Diagnostics.Debug.WriteLine($"아이콘 파일 경로 (Activated): {iconFile.Path}");
                                _appWindow.SetIcon(iconFile.Path);
                                System.Diagnostics.Debug.WriteLine("아이콘 설정 완료 (Activated)");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"아이콘 설정 오류 (Activated): {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                        }
                    }
                }
                catch
                {
                    // 크기 설정 실패 시 무시
                }
                // 한 번만 실행되도록 이벤트 제거
                this.Activated -= MainWindow_Activated;
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            StopMonitoring();
            _keyboardHook?.Dispose();
            _mediaPlayer?.Dispose();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingLanguageSelection)
                return;

            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string language)
            {
                _languageChangedByUser = true;
                SelectLanguage(language);
                SaveLanguageSetting();
            }
        }

        private void KeyboardHookTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
            
            int vkCode = (int)e.Key;
            _targetKeyCode = vkCode;
            
            // 키 이름 표시
            string keyName = GetKeyName(e.Key);
            KeyboardHookTextBox.Text = keyName;
            UpdateKeyboardHookDisplayText();
        }

        private string GetKeyName(VirtualKey key)
        {
            // 주요 키 이름 매핑
            return key switch
            {
                VirtualKey.Space => "Space",
                VirtualKey.Enter => "Enter",
                VirtualKey.Tab => "Tab",
                VirtualKey.Escape => "Escape",
                VirtualKey.Back => "Backspace",
                VirtualKey.Delete => "Delete",
                VirtualKey.Left => "Left Arrow",
                VirtualKey.Right => "Right Arrow",
                VirtualKey.Up => "Up Arrow",
                VirtualKey.Down => "Down Arrow",
                VirtualKey.Shift => "Shift",
                VirtualKey.Control => "Control",
                VirtualKey.Menu => "Alt",
                >= VirtualKey.Number0 and <= VirtualKey.Number9 => key.ToString().Replace("Number", ""),
                >= VirtualKey.A and <= VirtualKey.Z => key.ToString(),
                >= VirtualKey.F1 and <= VirtualKey.F24 => key.ToString(),
                _ => key.ToString()
            };
        }

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
            // 입력 검증
            if (_targetKeyCode == 0)
            {
                _ = ShowMessageDialogAsync("DialogErrorTitle", "MissingKeyMessage");
                return;
            }

            double intervalMin = IntervalMinuteBox.Value;
            double intervalSec = IntervalSecondBox.Value;
            
            if (intervalMin == 0 && intervalSec == 0)
            {
                _ = ShowMessageDialogAsync("DialogErrorTitle", "MissingCooldownMessage");
                return;
            }

            _intervalSec = TimeSpan.FromMinutes(intervalMin).Add(TimeSpan.FromSeconds(intervalSec));
            _alertSec = (int)AlertSecondBox.Value;

            // 모니터링 시작
            _isRunning = true;
            StartStopButton.Content = T("StopButton");
            StartStopButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Red);
            _alertPlayed = false;

            // 키보드 훅 시작
            _keyboardHook?.Start();
        }

        private void StopMonitoring()
        {
            _isRunning = false;
            StartStopButton.Content = T("StartButton");
            StartStopButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Green);
            _countdownTimer?.Stop();
            _keyboardHook?.Stop();
            _remainingTime = TimeSpan.Zero;
            _alertPlayed = false;
            UpdateCountdownDisplay(); // 00:00으로 표시
        }

        private void OnKeyPressed(int vkCode)
        {
            if (!_isRunning || vkCode != _targetKeyCode)
                return;

            if (PreventDuplicateInputCheckBox.IsChecked == true && _remainingTime.TotalSeconds > 0)
                return;

            // 키가 눌렸을 때 카운트다운 시작
            StartCountdown();
        }

        private void StartCountdown()
        {
            // 기존 타이머가 실행 중이면 먼저 중지
            _countdownTimer?.Stop();
            
            _remainingTime = _intervalSec;
            _alertPlayed = false;
            UpdateCountdownDisplay();
            
            // 타이머 시작
            if (_countdownTimer != null && _intervalSec.TotalSeconds > 0)
            {
                _countdownTimer.Start();
            }
        }

        private void CountdownTimer_Tick(object? sender, object e)
        {
            if (_remainingTime.TotalSeconds > 0)
            {
                _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
                UpdateCountdownDisplay();

                // alertSec 전에 사운드 재생
                if (_remainingTime.TotalSeconds <= _alertSec && !_alertPlayed)
                {
                    PlayAlertSound();
                    _alertPlayed = true;
                }
            }
            else
            {
                // 카운트다운 완료
                _countdownTimer?.Stop();
                _remainingTime = TimeSpan.Zero;
                _alertPlayed = false;
                UpdateCountdownDisplay(); // 00:00으로 표시
            }
        }

        private void UpdateCountdownDisplay()
        {
            if (CountdownText == null)
                return;

            int minutes = (int)_remainingTime.TotalMinutes;
            int seconds = _remainingTime.Seconds;
            string timeText = $"{minutes:D2}:{seconds:D2}";
            
            // 텍스트 업데이트
            CountdownText.Text = timeText;

            // 색상 변경 로직
            double remainingSeconds = _remainingTime.TotalSeconds;
            SolidColorBrush colorBrush;
            
            if (remainingSeconds <= _alertSec && remainingSeconds > 0)
            {
                // 알림시간 이후 핑크색
                colorBrush = new SolidColorBrush(Microsoft.UI.Colors.Pink);
            }
            else if (remainingSeconds <= _alertSec + 3 && remainingSeconds > _alertSec)
            {
                // 알림시간 3초 전 노란색
                colorBrush = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
            }
            else
            {
                // 기본 흰색
                colorBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
            }
            
            CountdownText.Foreground = colorBrush;

            // ProgressBar 업데이트 (카운트다운 동작 중일 때만)
            if (CountdownProgressBarContainer != null && CountdownProgressBar != null)
            {
                if (_isRunning && _intervalSec.TotalSeconds > 0)
                {
                    CountdownProgressBarContainer.Visibility = Visibility.Visible;
                    // 경과 시간 비율로 진행률 계산 (왼쪽에서 오른쪽으로 채워지게, 100% = 시간 종료, 0% = 시작)
                    double elapsedSeconds = _intervalSec.TotalSeconds - remainingSeconds;
                    _currentProgress = (elapsedSeconds / _intervalSec.TotalSeconds);
                    _currentProgress = Math.Max(0, Math.Min(1, _currentProgress)); // 0~1 사이로 제한
                    
                    // ProgressBar 너비를 진행률에 따라 조절
                    UpdateProgressBarWidth();
                    
                    // ProgressBar 색상을 텍스트 색상과 동일하게 설정
                    CountdownProgressBar.Fill = colorBrush;
                }
                else
                {
                    CountdownProgressBarContainer.Visibility = Visibility.Collapsed;
                    CountdownProgressBar.Width = 0;
                    _currentProgress = 0;
                }
            }
        }

        private void UpdateProgressBarWidth()
        {
            if (CountdownProgressBarContainer != null && CountdownProgressBar != null)
            {
                double containerWidth = CountdownProgressBarContainer.ActualWidth;
                if (containerWidth > 0)
                {
                    CountdownProgressBar.Width = containerWidth * _currentProgress;
                }
            }
        }

        private void CountdownProgressBarContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProgressBarWidth();
        }

        private async Task LoadSoundFilesAsync()
        {
            _isLoadingSounds = true;
            try
            {
                var soundsFolder = await GetAssetsFolderAsync("Assets\\sounds");
                if (soundsFolder == null)
                {
                    System.Diagnostics.Debug.WriteLine("sounds 폴더를 찾을 수 없습니다.");
                    _isLoadingSounds = false;
                    return;
                }

                var files = await soundsFolder.GetFilesAsync();
                
                // mp3 파일만 필터링하고 정렬
                var mp3Files = files.Where(f => f.FileType.ToLower() == ".mp3")
                                    .OrderBy(f => f.Name)
                                    .ToList();
                
                AlertSoundComboBox.Items.Clear();
                
                foreach (var file in mp3Files)
                {
                    string fileName = file.Name;
                    // 확장자 제거
                    string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    // 첫 글자를 대문자로 변환
                    string displayName = char.ToUpper(nameWithoutExtension[0]) + nameWithoutExtension.Substring(1);
                    
                    var item = new ComboBoxItem
                    {
                        Content = displayName, // 확장자 제외, 첫 글자 대문자
                        Tag = fileName // Tag는 실제 파일명
                    };
                    AlertSoundComboBox.Items.Add(item);
                }
                
                // 기본 선택 설정 (첫 번째 항목)
                if (AlertSoundComboBox.Items.Count > 0)
                {
                    AlertSoundComboBox.SelectedIndex = 0;
                    // 기본 선택된 파일명 설정 (실제 파일명 사용)
                    if (AlertSoundComboBox.SelectedItem is ComboBoxItem firstItem && firstItem.Tag is string firstFileName)
                    {
                        _selectedSoundFile = firstFileName;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"사운드 파일 로드 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
            finally
            {
                _isLoadingSounds = false;
            }
        }

        private void AlertSoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AlertSoundComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string soundFileName)
            {
                _selectedSoundFile = soundFileName; // Tag에서 실제 파일명 가져오기
                // 초기 로드가 완료되고 사용자가 직접 변경했을 때만 사운드 재생
                if (!_isLoadingSettings && !_isLoadingSounds)
                {
                    PlayAlertSound();
                }
            }
        }

        private async void PlayAlertSound()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    string soundPath = GetAssetsPath($"Assets\\sounds\\{_selectedSoundFile}");
                    MediaSource source;

                    if (File.Exists(soundPath))
                    {
                        // 직접 실행 파일인 경우 파일 경로 사용
                        var soundFile = await StorageFile.GetFileFromPathAsync(soundPath);
                        source = MediaSource.CreateFromStorageFile(soundFile);
                    }
                    else
                    {
                        // 패키징된 앱인 경우 ms-appx URI 사용
                        var soundUri = new Uri($"ms-appx:///Assets/sounds/{_selectedSoundFile}");
                        source = MediaSource.CreateFromUri(soundUri);
                    }

                    _mediaPlayer.Source = source;
                    _mediaPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"사운드 재생 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        private void FoldSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _isSettingsFolded = !_isSettingsFolded;
            
            if (_isSettingsFolded)
            {
                SettingsSection.Visibility = Visibility.Collapsed;
                FoldSettingsButton.Content = T("ShowSettingsButton");
                // 윈도우 높이를 400으로 조절
                ResizeWindow(350);
            }
            else
            {
                SettingsSection.Visibility = Visibility.Visible;
                FoldSettingsButton.Content = T("HideSettingsButton");
                // 윈도우 높이를 원래 크기(700)로 복원
                ResizeWindow(700);
            }
        }

        private void ResizeWindow(int height)
        {
            try
            {
                if (_appWindow == null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    _appWindow = AppWindow.GetFromWindowId(windowId);
                }

                if (_appWindow != null)
                {
                    var currentSize = _appWindow.Size;
                    _appWindow.Resize(new Windows.Graphics.SizeInt32(currentSize.Width, height));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"윈도우 크기 조절 오류: {ex.Message}");
            }
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
                if (_appWindow == null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    _appWindow = AppWindow.GetFromWindowId(windowId);
                }

                if (_appWindow != null)
                {
                    var presenter = _appWindow.Presenter as OverlappedPresenter;
                    if (presenter != null)
                    {
                        // WinUI 3에서 항상 위 설정
                        presenter.IsAlwaysOnTop = alwaysOnTop;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"항상 위 설정 오류: {ex.Message}");
            }
        }

        private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveSettings();
            await ShowMessageDialogAsync("DialogNoticeTitle", "SettingsSavedMessage");
        }

        private async Task SaveSettings()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                var composite = new Windows.Storage.ApplicationDataCompositeValue();

                // 설정값 저장
                composite["TargetKeyCode"] = _targetKeyCode;
                composite["KeyboardHookText"] = KeyboardHookTextBox.Text;
                composite["KeyboardHookDisplayText"] = KeyboardHookDisplayText.Text;
                composite["IntervalMinute"] = IntervalMinuteBox.Value;
                composite["IntervalSecond"] = IntervalSecondBox.Value;
                composite["AlertSecond"] = AlertSecondBox.Value;
                composite["SelectedSoundFile"] = _selectedSoundFile;
                composite["SelectedSoundIndex"] = AlertSoundComboBox.SelectedIndex;
                composite["AlwaysOnTop"] = AlwaysOnTopToggleButton.IsChecked ?? false;
                composite["PreventDuplicateInput"] = PreventDuplicateInputCheckBox.IsChecked ?? false;
                composite["SelectedLanguage"] = _selectedLanguage;

                localSettings.Values["CooldownReadySettings"] = composite;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 저장 오류: {ex.Message}");
            }

            SaveLanguageSettingFile();
            SavePreventDuplicateInputSettingFile();
        }

        private void SaveLanguageSetting()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                var composite = localSettings.Values["CooldownReadySettings"] as Windows.Storage.ApplicationDataCompositeValue
                    ?? new Windows.Storage.ApplicationDataCompositeValue();

                composite["SelectedLanguage"] = _selectedLanguage;
                localSettings.Values["CooldownReadySettings"] = composite;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"언어 설정 저장 오류: {ex.Message}");
            }

            SaveLanguageSettingFile();
        }

        private async Task LoadSettingsAsync()
        {
            string? persistedLanguage = LoadLanguageSettingFile();
            bool? persistedPreventDuplicateInput = LoadPreventDuplicateInputSettingFile();
            string selectedLanguage = persistedLanguage == null
                ? GetDefaultLanguage()
                : NormalizeLanguage(persistedLanguage);

            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                
                if (localSettings.Values.ContainsKey("CooldownReadySettings"))
                {
                    var composite = localSettings.Values["CooldownReadySettings"] as Windows.Storage.ApplicationDataCompositeValue;
                    
                    if (composite != null)
                    {
                        if (persistedLanguage == null && composite.ContainsKey("SelectedLanguage"))
                            selectedLanguage = NormalizeLanguage(composite["SelectedLanguage"] as string);

                        // 설정값 로드
                        if (composite.ContainsKey("TargetKeyCode"))
                            _targetKeyCode = (int)composite["TargetKeyCode"];

                        if (composite.ContainsKey("KeyboardHookText"))
                            KeyboardHookTextBox.Text = composite["KeyboardHookText"] as string ?? "";

                        if (composite.ContainsKey("IntervalMinute"))
                            IntervalMinuteBox.Value = (double)composite["IntervalMinute"];

                        if (composite.ContainsKey("IntervalSecond"))
                            IntervalSecondBox.Value = (double)composite["IntervalSecond"];

                        if (composite.ContainsKey("AlertSecond"))
                            AlertSecondBox.Value = (double)composite["AlertSecond"];

                        if (composite.ContainsKey("SelectedSoundFile"))
                        {
                            _selectedSoundFile = composite["SelectedSoundFile"] as string ?? "";
                            // ComboBox에서 선택된 사운드 찾기 (Tag로 비교)
                            for (int i = 0; i < AlertSoundComboBox.Items.Count; i++)
                            {
                                if (AlertSoundComboBox.Items[i] is ComboBoxItem item && item.Tag as string == _selectedSoundFile)
                                {
                                    AlertSoundComboBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                        else if (composite.ContainsKey("SelectedSoundIndex"))
                        {
                            int index = (int)composite["SelectedSoundIndex"];
                            if (index >= 0 && index < AlertSoundComboBox.Items.Count)
                            {
                                AlertSoundComboBox.SelectedIndex = index;
                                if (AlertSoundComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string soundFileName)
                                {
                                    _selectedSoundFile = soundFileName;
                                }
                            }
                        }

                        // 항상 위 설정 로드
                        if (composite.ContainsKey("AlwaysOnTop"))
                        {
                            bool alwaysOnTop = (bool)composite["AlwaysOnTop"];
                            AlwaysOnTopToggleButton.IsChecked = alwaysOnTop;
                            SetAlwaysOnTop(alwaysOnTop);
                        }

                        if (composite.ContainsKey("PreventDuplicateInput"))
                            PreventDuplicateInputCheckBox.IsChecked = persistedPreventDuplicateInput
                                ?? (bool)composite["PreventDuplicateInput"];
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 로드 오류: {ex.Message}");
            }
            finally
            {
                if (!_languageChangedByUser)
                {
                    SelectLanguage(selectedLanguage);
                }

                if (persistedPreventDuplicateInput != null)
                {
                    PreventDuplicateInputCheckBox.IsChecked = persistedPreventDuplicateInput.Value;
                }

                // 설정 로드 완료 후 플래그 해제
                _isLoadingSettings = false;
            }
        }
    }
}
