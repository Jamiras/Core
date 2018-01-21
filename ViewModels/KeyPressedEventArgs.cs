using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace Jamiras.ViewModels
{
    public class KeyPressedEventArgs : EventArgs
    {
        public KeyPressedEventArgs(Key key, ModifierKeys modifiers = ModifierKeys.None)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public Key Key { get; private set; }

        public ModifierKeys Modifiers { get; private set; }

        public bool Handled { get; set; }

        public char GetChar()
        {
            // https://stackoverflow.com/questions/5825820/how-to-capture-the-character-on-different-locale-keyboards-in-wpf-c
            // map the Key enum to a virtual key
            int virtualKey = KeyInterop.VirtualKeyFromKey(Key);

            // map the virtual key to a scan code
            const uint MAPVK_VK_TO_VSC = 0;
            uint scanCode = MapVirtualKey((uint)virtualKey, MAPVK_VK_TO_VSC);

            // get the state of keys (shift/caps/etc)
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            // convert the scan code and key state to a character
            StringBuilder buffer = new StringBuilder(2);
            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, buffer, buffer.Capacity, 0);

            switch (result)
            {
                case -1: // accent or diacritic
                    return '\0';
                case 0: // key does not map to a character
                    return '\0';
                case 1: // single character
                    return buffer[0];
                default: // more than one character
                    return buffer[0];
            }
        }

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode,byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff,
            int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}
