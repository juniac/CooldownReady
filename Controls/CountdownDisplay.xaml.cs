using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CooldownReady.Controls
{
    /// <summary>
    /// 키 라벨 영역 전체를 진행바 배경으로 쓰는 카운트다운 컴포넌트.
    /// 남은 시간은 초 단위(최대 999)로 표시하고,
    /// 남은 시간에 따라 색상(흰색 → 노란색 → 핑크색)이 바뀝니다.
    /// </summary>
    public sealed partial class CountdownDisplay : UserControl
    {
        private const int MaxDisplaySeconds = 999;

        private double _currentProgress;
        private double _alertFraction; // 알림 시점의 진행률 위치 (0~1), 0이면 표시 안 함

        /// <summary>
        /// 설정된 쿨다운/알림 시간으로 알림 위치 세로선을 갱신합니다.
        /// 알림이 설정되어 있으면 카운트다운 여부와 관계없이 항상 표시됩니다.
        /// </summary>
        public void SetAlertConfig(double intervalSeconds, double alertSeconds)
        {
            _alertFraction = (intervalSeconds > 0 && alertSeconds > 0)
                ? Math.Clamp(1 - alertSeconds / intervalSeconds, 0, 1)
                : 0;
            AlertMarker.Visibility = _alertFraction > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateProgressBarWidth();
        }

        /// <summary>남은 시간을 밀리초까지 표시할지 여부</summary>
        public bool ShowMilliseconds { get; set; }

        public CountdownDisplay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 이 카운트다운이 어떤 키의 것인지 표시합니다.
        /// </summary>
        public void SetKeyName(string? keyName)
        {
            KeyNameText.Text = string.IsNullOrWhiteSpace(keyName) ? "—" : keyName;
        }

        /// <summary>
        /// 0초로 초기화하고 진행바를 비웁니다.
        /// </summary>
        public void Reset()
        {
            Update(TimeSpan.Zero, TimeSpan.Zero, 0, showProgress: false);
        }

        /// <summary>
        /// 카운트다운 표시를 갱신합니다.
        /// </summary>
        /// <param name="remaining">남은 시간</param>
        /// <param name="interval">전체 쿨다운 시간</param>
        /// <param name="alertSeconds">알림 시점(초)</param>
        /// <param name="showProgress">진행바 표시 여부</param>
        public void Update(TimeSpan remaining, TimeSpan interval, int alertSeconds, bool showProgress)
        {
            double remainingSeconds = remaining.TotalSeconds;
            double cappedSeconds = Math.Min(remainingSeconds, MaxDisplaySeconds);
            SecondsText.Text = ShowMilliseconds
                ? cappedSeconds.ToString("0.00")
                : Math.Ceiling(cappedSeconds).ToString("0");

            Brush colorBrush;
            if (remainingSeconds <= alertSeconds && remainingSeconds > 0)
            {
                // 알림 구간은 핑크색
                colorBrush = new SolidColorBrush(Microsoft.UI.Colors.Pink);
            }
            else
            {
                // 기본은 테마 전경색 (다크 모드 흰색, 라이트 모드 검은색)
                colorBrush = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            }

            SecondsText.Foreground = colorBrush;

            if (showProgress && interval.TotalSeconds > 0)
            {
                // 경과 시간 비율로 진행률 계산 (100% = 시간 종료, 0% = 시작)
                double elapsedSeconds = interval.TotalSeconds - remainingSeconds;
                _currentProgress = Math.Clamp(elapsedSeconds / interval.TotalSeconds, 0, 1);
                ProgressBar.Fill = colorBrush;
            }
            else
            {
                _currentProgress = 0;
            }

            // 알림 위치 세로선은 SetAlertConfig가 관리하므로 여기서는 건드리지 않는다
            UpdateProgressBarWidth();
        }

        private void UpdateProgressBarWidth()
        {
            double containerWidth = BarContainer.ActualWidth;
            if (containerWidth > 0)
            {
                ProgressBar.Width = containerWidth * _currentProgress;
                AlertMarker.Margin = new Thickness(containerWidth * _alertFraction, 0, 0, 0);
            }
        }

        private void BarContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProgressBarWidth();
        }
    }
}
