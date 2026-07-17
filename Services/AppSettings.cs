using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CooldownReady.Services
{
    /// <summary>
    /// 키 하나에 대한 쿨다운 설정.
    /// </summary>
    public class KeyBindingSettings
    {
        public bool Enabled { get; set; } = true;
        public int TargetKeyCode { get; set; }
        public string KeyName { get; set; } = "";

        /// <summary>구버전(분 단위 입력) 마이그레이션 전용. 로드 시 IntervalSecond로 합산된다.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double IntervalMinute { get; set; } = 0;

        public double IntervalSecond { get; set; } = 5;
        public double AlertSecond { get; set; } = 1;
        public string SelectedSoundFile { get; set; } = "";
        public bool PreventDuplicateInput { get; set; }
        public bool ShowMilliseconds { get; set; }
    }

    /// <summary>
    /// 앱의 모든 사용자 설정. settings.json 한 곳에 저장됩니다.
    /// </summary>
    public class AppSettings
    {
        public bool AlwaysOnTop { get; set; }
        public string? SelectedLanguage { get; set; }
        public List<KeyBindingSettings> Bindings { get; set; } = new();
    }
}
