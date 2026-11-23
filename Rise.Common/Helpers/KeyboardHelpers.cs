using Windows.System;
using Windows.UI.Core;

namespace Rise.Common.Helpers
{
    public static class KeyboardHelpers
    {
        public static bool IsCtrlPressed()
        {
            var window = CoreWindow.GetForCurrentThread();
            if (window == null)
                return false;

            CoreVirtualKeyStates state = window.GetKeyState(VirtualKey.Control);
            return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }
    }
}
