using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace CooldownReady
{
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private DispatcherQueue? _dispatcherQueue;

        public event Action<int>? KeyPressed;

        /// <summary>
        /// 저수준 훅은 좌/우 구분 코드(VK_LSHIFT 등)를 주고 XAML KeyDown은 통합 코드(VK_SHIFT 등)를 주므로,
        /// modifier 키를 통합 코드로 정규화해 양쪽을 비교할 수 있게 합니다.
        /// </summary>
        public static int NormalizeKeyCode(int vkCode)
        {
            return vkCode switch
            {
                0xA0 or 0xA1 => 0x10, // VK_LSHIFT / VK_RSHIFT -> VK_SHIFT
                0xA2 or 0xA3 => 0x11, // VK_LCONTROL / VK_RCONTROL -> VK_CONTROL
                0xA4 or 0xA5 => 0x12, // VK_LMENU / VK_RMENU -> VK_MENU(Alt)
                _ => vkCode
            };
        }

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
        }

        public void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }

        public void Start()
        {
            _hookID = SetHook(_proc);
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                var curModule = curProcess.MainModule;
                if (curModule?.ModuleName == null)
                {
                    throw new InvalidOperationException("MainModule을 가져올 수 없습니다.");
                }
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        KeyPressed?.Invoke(vkCode);
                    });
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public void Dispose()
        {
            Stop();
        }
    }
}

