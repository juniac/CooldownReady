using System;
using System.Collections.Generic;
using System.IO;
using CooldownReady.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace CooldownReady.Controls
{
    /// <summary>
    /// 키 하나의 쿨다운 설정(키·알림음·쿨다운 시간·알림 시간·재누름 방지)을 편집하는 행.
    /// 값이 바뀌면 연결된 <see cref="KeyBindingSettings"/>에 즉시 반영됩니다.
    /// </summary>
    public sealed partial class KeyBindingRow : UserControl
    {
        public KeyBindingSettings Binding { get; }

        /// <summary>삭제 버튼이 눌렸을 때</summary>
        public event Action<KeyBindingRow>? RemoveRequested;
        /// <summary>모니터링 키가 바뀌었을 때</summary>
        public event Action<KeyBindingRow>? KeyChanged;
        /// <summary>사용 여부가 바뀌었을 때</summary>
        public event Action<KeyBindingRow>? EnabledChanged;
        /// <summary>ms 표시 여부가 바뀌었을 때</summary>
        public event Action<KeyBindingRow>? ShowMillisecondsChanged;
        /// <summary>쿨다운/알림 시간이 바뀌었을 때</summary>
        public event Action<KeyBindingRow>? TimingChanged;
        /// <summary>사용자가 알림음을 바꿔 미리듣기가 필요할 때</summary>
        public event Action<string>? SoundPreviewRequested;

        private bool _isInitializing = true;

        public KeyBindingRow(KeyBindingSettings binding)
        {
            InitializeComponent();
            Binding = binding;

            // 엔터 입력 시 NumberBox 포커스를 거둬들일 대상
            IsTabStop = true;

            EnabledCheckBox.IsChecked = binding.Enabled;
            KeyBox.Text = binding.KeyName;
            SecondBox.Value = binding.IntervalSecond;
            AlertBox.Value = Math.Min(binding.AlertSecond, binding.IntervalSecond);
            PreventCheckBox.IsChecked = binding.PreventDuplicateInput;
            ShowMsCheckBox.IsChecked = binding.ShowMilliseconds;

            _isInitializing = false;
        }

        public void ApplyLocalization(LocalizationService localization)
        {
            KeyBox.PlaceholderText = localization.GetString("KeyPlaceholderShort");
            SecondUnitText.Text = localization.GetString("SecondText");
            AlertUnitText.Text = localization.GetString("AlertShortLabel");
            PreventCheckBox.Content = localization.GetString("PreventDuplicateInputLabel");
            ShowMsCheckBox.Content = localization.GetString("ShowMillisecondsLabel");
            ToolTipService.SetToolTip(RemoveButton, localization.GetString("RemoveKeyToolTip"));
            ToolTipService.SetToolTip(EnabledCheckBox, localization.GetString("EnabledToolTip"));
        }

        /// <summary>
        /// 알림음 목록을 채우고 저장된 값(없으면 첫 항목)을 선택합니다.
        /// </summary>
        public void SetSoundOptions(IReadOnlyList<string> fileNames)
        {
            _isInitializing = true;
            try
            {
                SoundComboBox.Items.Clear();

                int selectedIndex = 0;
                for (int i = 0; i < fileNames.Count; i++)
                {
                    string fileName = fileNames[i];
                    string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    string displayName = nameWithoutExtension.Length > 0
                        ? char.ToUpper(nameWithoutExtension[0]) + nameWithoutExtension.Substring(1)
                        : fileName;

                    SoundComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = displayName,
                        Tag = fileName
                    });

                    if (fileName == Binding.SelectedSoundFile)
                    {
                        selectedIndex = i;
                    }
                }

                if (SoundComboBox.Items.Count > 0)
                {
                    SoundComboBox.SelectedIndex = selectedIndex;
                    if (SoundComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string selectedFileName)
                    {
                        Binding.SelectedSoundFile = selectedFileName;
                    }
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void KeyBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            // modifier 키는 좌/우 구분 코드로 들어올 수 있으므로 통합 코드로 정규화
            int keyCode = GlobalKeyboardHook.NormalizeKeyCode((int)e.Key);
            Binding.TargetKeyCode = keyCode;
            Binding.KeyName = GetKeyName((VirtualKey)keyCode);
            KeyBox.Text = Binding.KeyName;
            KeyChanged?.Invoke(this);
        }

        private void SoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string soundFileName)
            {
                Binding.SelectedSoundFile = soundFileName;
                if (!_isInitializing)
                {
                    SoundPreviewRequested?.Invoke(soundFileName);
                }
            }
        }

        private void EnabledCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            Binding.Enabled = EnabledCheckBox.IsChecked ?? false;
            if (!_isInitializing)
            {
                EnabledChanged?.Invoke(this);
            }
        }

        private void SecondBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            Binding.IntervalSecond = double.IsNaN(sender.Value) ? 0 : sender.Value;

            // 알림 시간은 쿨다운 시간을 넘을 수 없다
            AlertBox.Maximum = Binding.IntervalSecond;
            if (AlertBox.Value > Binding.IntervalSecond)
            {
                AlertBox.Value = Binding.IntervalSecond;
            }

            if (!_isInitializing)
            {
                TimingChanged?.Invoke(this);
            }
        }

        private void AlertBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            double value = double.IsNaN(sender.Value) ? 0 : sender.Value;

            // 알림 시간은 쿨다운 시간을 넘을 수 없다
            if (value > Binding.IntervalSecond)
            {
                sender.Value = Binding.IntervalSecond; // 값 보정 시 ValueChanged가 다시 호출된다
                return;
            }

            Binding.AlertSecond = value;

            if (!_isInitializing)
            {
                TimingChanged?.Invoke(this);
            }
        }

        private void PreventCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            Binding.PreventDuplicateInput = PreventCheckBox.IsChecked ?? false;
        }

        private void ShowMsCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            Binding.ShowMilliseconds = ShowMsCheckBox.IsChecked ?? false;
            if (!_isInitializing)
            {
                ShowMillisecondsChanged?.Invoke(this);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveRequested?.Invoke(this);
        }

        private void NumberBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            // 엔터로 값을 확정하면 입력 상자의 포커스를 해제한다
            if (e.Key == VirtualKey.Enter)
            {
                Focus(FocusState.Programmatic);
            }
        }

        internal static string GetKeyName(VirtualKey key)
        {
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
                _ => key.ToString()
            };
        }
    }
}
