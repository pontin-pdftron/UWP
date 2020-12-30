using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompleteReader.Utilities
{
    public class HotKeyHandler
    {
        private static HotKeyHandler _Current = null;
        public static HotKeyHandler Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new HotKeyHandler();
                }
                return _Current;
            }
        }

        private HotKeyHandler()
        {
            Windows.UI.Xaml.Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.KeyStatus.WasKeyDown || args.VirtualKey == Windows.System.VirtualKey.Menu || args.VirtualKey == Windows.System.VirtualKey.Control)
            {
                return;
            }
            Windows.UI.Core.CoreVirtualKeyStates ctrlState = Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control);
            if ((ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
            {
                Windows.UI.Core.CoreVirtualKeyStates altState = Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Menu);
                if ((altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                {
                    if (AltHotKeyPressedEvent != null)
                    {
                        AltHotKeyPressedEvent(sender, args);
                    }
                }
                else
                {
                    if (HotKeyPressedEvent != null)
                    {
                        HotKeyPressedEvent(sender, args);
                    }
                    CheckforEasterEgg(args.VirtualKey);
                }
            }
            else
            {
                if (KeyPressedEvent != null)
                {
                    KeyPressedEvent(sender, args);
                }
            }
        }

        public event Windows.Foundation.TypedEventHandler<Windows.UI.Core.CoreWindow, Windows.UI.Core.KeyEventArgs> KeyPressedEvent;

        public event Windows.Foundation.TypedEventHandler<Windows.UI.Core.CoreWindow, Windows.UI.Core.KeyEventArgs> HotKeyPressedEvent;

        public event Windows.Foundation.TypedEventHandler<Windows.UI.Core.CoreWindow, Windows.UI.Core.KeyEventArgs> AltHotKeyPressedEvent;

        public bool IsShiftDown
        {
            get
            {
                Windows.UI.Core.CoreVirtualKeyStates shiftState = Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Shift);
                return (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
            } 
        }

        private const string EASTER_EGG_STRING = "CRASHMENOW";
        private static int _CurrentEasterEggPosition = -1;
        private static string _EasterEggCrashString = null;

        private void CheckforEasterEgg(Windows.System.VirtualKey key)
        {
            int curr_index = _CurrentEasterEggPosition;
            if (curr_index >= 0 && curr_index < EASTER_EGG_STRING.Length)
            {
                curr_index++;
                string nextPosition = EASTER_EGG_STRING.Substring(curr_index, 1);
                string keyString = key.ToString();
                if (nextPosition.Equals(keyString, StringComparison.OrdinalIgnoreCase))
                {
                    _CurrentEasterEggPosition = curr_index;
                    if (keyString == "_")
                    {
                        _EasterEggCrashString = "LALA";
                    }
                    if (_CurrentEasterEggPosition == EASTER_EGG_STRING.Length - 1)
                    {
                        curr_index = _EasterEggCrashString.IndexOf("_");
                    }
                }
                else
                {
                    _CurrentEasterEggPosition = -1;
                }
            }

            if (key == Windows.System.VirtualKey.C)
            {
                _CurrentEasterEggPosition = 0;
            }

            System.Diagnostics.Debug.WriteLineIf(_CurrentEasterEggPosition >= 0 && _CurrentEasterEggPosition < EASTER_EGG_STRING.Length, _CurrentEasterEggPosition);
        }
    }
}
