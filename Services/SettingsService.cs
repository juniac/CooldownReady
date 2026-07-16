using System;
using System.IO;
using System.Text.Json;

namespace CooldownReady.Services
{
    /// <summary>
    /// 설정을 %LOCALAPPDATA%\CooldownReady\settings.json 한 곳에만 저장합니다.
    /// 구버전 저장소의 값은 최초 실행 시 마이그레이션합니다.
    /// - v0: ApplicationData LocalSettings composite + language.txt / prevent-duplicate-input.txt
    /// - v1: settings.json의 단일 키 평면 구조 (TargetKeyCode 등이 최상위 속성)
    /// - v2(현재): Bindings 배열로 여러 키 지원
    /// </summary>
    public static class SettingsService
    {
        private const string LegacyCompositeKey = "CooldownReadySettings";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        public static string SettingsPath => Path.Combine(ErrorLogger.AppDataDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        // v1(단일 키 평면 구조) 파일이면 Bindings 배열로 변환
                        if (settings.Bindings.Count == 0)
                        {
                            var legacyBinding = TryParseFlatBinding(json);
                            if (legacyBinding != null)
                            {
                                settings.Bindings.Add(legacyBinding);
                                Save(settings);
                            }
                        }
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("설정 로드 오류", ex);
            }

            var migrated = MigrateLegacySettings();
            if (migrated != null)
            {
                Save(migrated);
                CleanupLegacySettings();
                return migrated;
            }

            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(ErrorLogger.AppDataDirectory);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("설정 저장 오류", ex);
            }
        }

        /// <summary>
        /// v1 settings.json의 최상위 단일 키 설정을 KeyBindingSettings로 변환합니다.
        /// </summary>
        private static KeyBindingSettings? TryParseFlatBinding(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (!root.TryGetProperty("TargetKeyCode", out var targetKeyCode))
                    return null;

                var binding = new KeyBindingSettings
                {
                    TargetKeyCode = targetKeyCode.ValueKind == JsonValueKind.Number ? targetKeyCode.GetInt32() : 0
                };

                if (root.TryGetProperty("KeyboardHookText", out var keyName) && keyName.ValueKind == JsonValueKind.String)
                    binding.KeyName = keyName.GetString() ?? "";
                if (root.TryGetProperty("IntervalMinute", out var intervalMinute) && intervalMinute.ValueKind == JsonValueKind.Number)
                    binding.IntervalMinute = intervalMinute.GetDouble();
                if (root.TryGetProperty("IntervalSecond", out var intervalSecond) && intervalSecond.ValueKind == JsonValueKind.Number)
                    binding.IntervalSecond = intervalSecond.GetDouble();
                if (root.TryGetProperty("AlertSecond", out var alertSecond) && alertSecond.ValueKind == JsonValueKind.Number)
                    binding.AlertSecond = alertSecond.GetDouble();
                if (root.TryGetProperty("SelectedSoundFile", out var soundFile) && soundFile.ValueKind == JsonValueKind.String)
                    binding.SelectedSoundFile = soundFile.GetString() ?? "";
                if (root.TryGetProperty("PreventDuplicateInput", out var preventDuplicate)
                    && (preventDuplicate.ValueKind == JsonValueKind.True || preventDuplicate.ValueKind == JsonValueKind.False))
                    binding.PreventDuplicateInput = preventDuplicate.GetBoolean();

                return binding;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"v1 설정 변환 오류: {ex.Message}");
                return null;
            }
        }

        private static string LegacyLanguagePath => Path.Combine(ErrorLogger.AppDataDirectory, "language.txt");
        private static string LegacyPreventDuplicateInputPath => Path.Combine(ErrorLogger.AppDataDirectory, "prevent-duplicate-input.txt");

        private static AppSettings? MigrateLegacySettings()
        {
            AppSettings? settings = null;
            KeyBindingSettings? binding = null;

            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                if (localSettings.Values[LegacyCompositeKey] is Windows.Storage.ApplicationDataCompositeValue composite)
                {
                    settings = new AppSettings();
                    binding = new KeyBindingSettings();

                    if (composite.TryGetValue("TargetKeyCode", out var targetKeyCode) && targetKeyCode is int targetKeyCodeValue)
                        binding.TargetKeyCode = targetKeyCodeValue;
                    if (composite.TryGetValue("KeyboardHookText", out var keyboardHookText) && keyboardHookText is string keyboardHookTextValue)
                        binding.KeyName = keyboardHookTextValue;
                    if (composite.TryGetValue("IntervalMinute", out var intervalMinute) && intervalMinute is double intervalMinuteValue)
                        binding.IntervalMinute = intervalMinuteValue;
                    if (composite.TryGetValue("IntervalSecond", out var intervalSecond) && intervalSecond is double intervalSecondValue)
                        binding.IntervalSecond = intervalSecondValue;
                    if (composite.TryGetValue("AlertSecond", out var alertSecond) && alertSecond is double alertSecondValue)
                        binding.AlertSecond = alertSecondValue;
                    if (composite.TryGetValue("SelectedSoundFile", out var selectedSoundFile) && selectedSoundFile is string selectedSoundFileValue)
                        binding.SelectedSoundFile = selectedSoundFileValue;
                    if (composite.TryGetValue("AlwaysOnTop", out var alwaysOnTop) && alwaysOnTop is bool alwaysOnTopValue)
                        settings.AlwaysOnTop = alwaysOnTopValue;
                    if (composite.TryGetValue("PreventDuplicateInput", out var preventDuplicateInput) && preventDuplicateInput is bool preventDuplicateInputValue)
                        binding.PreventDuplicateInput = preventDuplicateInputValue;
                    if (composite.TryGetValue("SelectedLanguage", out var selectedLanguage) && selectedLanguage is string selectedLanguageValue)
                        settings.SelectedLanguage = selectedLanguageValue;
                }
            }
            catch (Exception ex)
            {
                // unpackaged 실행에서는 ApplicationData를 사용할 수 없을 수 있음
                System.Diagnostics.Debug.WriteLine($"기존 LocalSettings 마이그레이션 건너뜀: {ex.Message}");
            }

            try
            {
                if (File.Exists(LegacyLanguagePath))
                {
                    settings ??= new AppSettings();
                    settings.SelectedLanguage = File.ReadAllText(LegacyLanguagePath).Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기존 언어 설정 마이그레이션 오류: {ex.Message}");
            }

            try
            {
                if (File.Exists(LegacyPreventDuplicateInputPath)
                    && bool.TryParse(File.ReadAllText(LegacyPreventDuplicateInputPath).Trim(), out bool preventDuplicateInput))
                {
                    settings ??= new AppSettings();
                    binding ??= new KeyBindingSettings();
                    binding.PreventDuplicateInput = preventDuplicateInput;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기존 중복 입력 방지 설정 마이그레이션 오류: {ex.Message}");
            }

            if (settings != null && binding != null)
            {
                settings.Bindings.Add(binding);
            }

            return settings;
        }

        private static void CleanupLegacySettings()
        {
            try
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove(LegacyCompositeKey);
            }
            catch
            {
                // unpackaged 실행에서는 접근 불가할 수 있음
            }

            TryDelete(LegacyLanguagePath);
            TryDelete(LegacyPreventDuplicateInputPath);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // 정리 실패는 무시
            }
        }
    }
}
