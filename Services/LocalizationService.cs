using System;
using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.System.UserProfile;

namespace CooldownReady.Services
{
    /// <summary>
    /// Strings\{언어}\Resources.resw 리소스에서 번역 문자열을 조회하고
    /// 런타임 언어 전환을 지원합니다.
    /// </summary>
    public class LocalizationService
    {
        public const string Korean = "ko-KR";
        public const string English = "en-US";

        private readonly ResourceManager _resourceManager = new();
        private readonly ResourceContext _resourceContext;

        public string CurrentLanguage { get; private set; } = English;

        public LocalizationService(string? language = null)
        {
            _resourceContext = _resourceManager.CreateResourceContext();
            SetLanguage(language ?? GetDefaultLanguage());
        }

        public void SetLanguage(string? language)
        {
            CurrentLanguage = NormalizeLanguage(language);
            _resourceContext.QualifierValues["Language"] = CurrentLanguage;
        }

        public string GetString(string key)
        {
            try
            {
                var candidate = _resourceManager.MainResourceMap.TryGetValue($"Resources/{key}", _resourceContext);
                if (candidate != null)
                {
                    return candidate.ValueAsString;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"리소스 조회 오류 ({key}): {ex.Message}");
            }

            return key;
        }

        public string Format(string key, params object[] args)
        {
            return string.Format(GetString(key), args);
        }

        public static string NormalizeLanguage(string? language)
        {
            return TryGetSupportedLanguage(language) ?? English;
        }

        public static string? TryGetSupportedLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return null;

            string normalizedLanguage = language.Trim().Replace('_', '-');
            if (normalizedLanguage.Equals(Korean, StringComparison.OrdinalIgnoreCase))
                return Korean;
            if (normalizedLanguage.Equals(English, StringComparison.OrdinalIgnoreCase))
                return English;

            string primaryLanguage = normalizedLanguage.Split('-')[0].ToLowerInvariant();
            return primaryLanguage switch
            {
                "ko" => Korean,
                "en" => English,
                _ => null
            };
        }

        public static string GetDefaultLanguage()
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
                ?? English;
        }
    }
}
