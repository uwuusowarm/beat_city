using UnityEngine;

public static class CursorState
{
    public static void Refresh()
    {
        bool menuOpen = ESCMenu.isPaused || UpgradeShop.IsOpen;

        Cursor.visible = menuOpen;
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
