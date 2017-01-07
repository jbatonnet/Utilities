using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.Win32
{
    public class KeyboardHook
    {
        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;
        public event KeyPressEventHandler KeyPress;

        private int hookId;
        private User32.HookProc hookProc;

        public KeyboardHook()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                throw new NotSupportedException("This class is not available for the current platform");

            hookProc = KeyboardHookCallback;

            using (Process process = Process.GetCurrentProcess())
                hookId = User32.SetWindowsHookEx(WH.KEYBOARD_LL, hookProc, Kernel32.GetModuleHandle(process.MainModule.ModuleName), 0);

            if (hookId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
        }

        private int KeyboardHookCallback(int nCode, int wParam, IntPtr lParam)
        {
            bool handled = false;

            if ((nCode >= 0) && (KeyDown != null || KeyUp != null || KeyPress != null))
            {
                // Récupération de l'état du clavier
                KeyboardHookStruct keyboardHook = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                bool shift = (User32.GetKeyState(VK.SHIFT) & 0x80) == 0x80;
                bool capsLock = User32.GetKeyState(VK.CAPITAL) != 0;
                bool control = User32.GetKeyState(VK.CONTROL) != 0;

                // KeyDown
                if (KeyDown != null && (wParam == WM.KEYDOWN || wParam == WM.SYSKEYDOWN))
                {
                    Keys keyData = (Keys)keyboardHook.vkCode;
                    if (control) keyData |= Keys.Control;
                    if (shift) keyData |= Keys.Shift;

                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyDown(this, e);

                    handled = handled || e.Handled;
                }

                // KeyPress
                if (KeyPress != null && wParam == WM.KEYDOWN)
                {
                    byte[] keyState = new byte[256];
                    User32.GetKeyboardState(keyState);
                    byte[] inBuffer = new byte[2];

                    if (User32.ToAscii(keyboardHook.vkCode, keyboardHook.scanCode, keyState, inBuffer, keyboardHook.flags) == 1)
                    {
                        char key = (char)inBuffer[0];
                        if ((capsLock ^ shift) && Char.IsLetter(key))
                            key = Char.ToUpper(key);
                        KeyPressEventArgs e = new KeyPressEventArgs(key);

                        KeyPress(this, e);
                        handled = handled || e.Handled;
                    }
                }

                // KeyUp
                if (KeyUp != null && (wParam == WM.KEYUP || wParam == WM.SYSKEYUP))
                {
                    Keys keyData = (Keys)keyboardHook.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);

                    KeyUp(this, e);
                    handled = handled || e.Handled;
                }
            }

            // Si handled est a true, on ne transmet pas le message au destinataire
            if (handled)
                return 1;
            else
                return User32.CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
}