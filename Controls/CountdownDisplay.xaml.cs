using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CooldownReady.Controls
{
    /// <summary>
    /// 남은 시간 텍스트와 진행바를 함께 표시하는 카운트다운 컴포넌트.
    /// 남은 시간에 따라 색상(흰색 → 노란색 → 핑크색)이 바뀝니다.
    /// </summary>
    public sealed partial class CountdownDisplay : UserControl
    {
        private double _currentProgress;

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
        /// 00:00으로 초기화하고 진행바를 숨깁니다.
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
            int minutes = (int)remaining.TotalMinutes;
            int seconds = remaining.Seconds;
            CountdownText.Text = $"{minutes:D2}:{seconds:D2}";

            double remainingSeconds = remaining.TotalSeconds;
            SolidColorBrush colorBrush;

            if (remainingSeconds <= alertSeconds && remainingSeconds > 0)
            {
                // 알림시간 이후 핑크색
                colorBrush = new SolidColorBrush(Microsoft.UI.Colors.Pink);
            }
            else if (remainingSeconds <= alertSeconds + 3 && remainingSeconds > alertSeconds)
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

            if (showProgress && interval.TotalSeconds > 0)
            {
                ProgressBarContainer.Visibility = Visibility.Visible;
                // 경과 시간 비율로 진행률 계산 (100% = 시간 종료, 0% = 시작)
                double elapsedSeconds = interval.TotalSeconds - remainingSeconds;
                _currentProgress = Math.Clamp(elapsedSeconds / interval.TotalSeconds, 0, 1);
                UpdateProgressBarWidth();
                ProgressBar.Fill = colorBrush;
            }
            else
            {
                ProgressBarContainer.Visibility = Visibility.Collapsed;
                ProgressBar.Width = 0;
                _currentProgress = 0;
            }
        }

        private void UpdateProgressBarWidth()
        {
            double containerWidth = ProgressBarContainer.ActualWidth;
            if (containerWidth > 0)
            {
                ProgressBar.Width = containerWidth * _currentProgress;
            }
        }

        private void ProgressBarContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProgressBarWidth();
        }
    }
}
