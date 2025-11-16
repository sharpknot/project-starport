using UnityEngine;
using UnityEngine.Events;

namespace Starport.UI
{
    public static class UIEvents
    {
        public static UnityAction<string> ShowSessionStartCover;
        public static UnityAction HideSessionStartCover;

        public static UnityAction ShowOptionsMenu;
        public static UnityAction HiddenOptionsMenu;

        public static UnityAction<string> ShowDisconnectWindow;

        public static UnityAction<bool> ShowHUD;
    }

    
}
