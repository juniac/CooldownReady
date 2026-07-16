using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CooldownReady.Controls
{
    /// <summary>
    /// "제목 + 입력 영역" 형태로 반복되는 설정 섹션 컨트롤.
    /// 템플릿(암시적 스타일)은 App.xaml에 정의되어 있습니다.
    /// </summary>
    public sealed class SettingGroup : ContentControl
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(SettingGroup),
                new PropertyMetadata(string.Empty));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public SettingGroup()
        {
            DefaultStyleKey = typeof(SettingGroup);
        }
    }
}
