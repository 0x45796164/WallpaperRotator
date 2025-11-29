using System;
using System.Runtime.InteropServices;
using WallpaperRotator.Utilities;

namespace WallpaperRotator.Windows;

public static class WindowInjector
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public static readonly IntPtr HWND_TOP = new IntPtr(0);
    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int WS_CHILD = 0x40000000;
    public const int WS_POPUP = unchecked((int)0x80000000);
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const uint WM_SPAWN_WORKER = 0x052C;

    [DllImport("user32.dll")]
    public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    public static IntPtr GetWallpaperTarget(out IntPtr insertAfter)
    {
        insertAfter = HWND_BOTTOM; // Default

        // 1. Find Progman
        IntPtr progman = FindWindow("Progman", null);
        Logger.Info($"Progman handle: {progman}");
        
        if (progman == IntPtr.Zero)
        {
            Logger.Error("Could not find Progman window.");
            return IntPtr.Zero;
        }

        // Check if SHELLDLL_DefView is already on Progman (Windows 11 24H2 fix)
        IntPtr shellDllOnProgman = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (shellDllOnProgman != IntPtr.Zero)
        {
            Logger.Info($"SHELLDLL_DefView found on Progman ({shellDllOnProgman}). Using Progman as target and inserting behind icons.");
            insertAfter = shellDllOnProgman; // Place behind icons
            return progman;
        }

        // 2. Send message to spawn WorkerW
        // Note: We send it to Progman, which triggers the creation of a WorkerW behind the icons.
        IntPtr result = IntPtr.Zero;
        SendMessageTimeout(progman, WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero, 0, 1000, out result);
        Logger.Info($"Sent WM_SPAWN_WORKER message. Result: {result}");

        // 3. Find the WorkerW that is BEHIND the one with icons.
        // The Z-order is: WorkerW(Icons) -> WorkerW(Background) -> Desktop
        // We want to attach to WorkerW(Background).
        // FindWindowEx(0, hwnd, ...) finds the window BELOW hwnd in Z-order.
        
        IntPtr targetWorkerW = IntPtr.Zero;
        
        EnumWindows((hwnd, lParam) =>
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            
            if (sb.ToString() == "WorkerW")
            {
                // Check if this WorkerW has SHELLDLL_DefView (Icons)
                IntPtr shellDll = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                
                if (shellDll != IntPtr.Zero)
                {
                    Logger.Info($"Found WorkerW with Icons: {hwnd}");
                    // The target is the NEXT WorkerW in Z-order
                    targetWorkerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    Logger.Info($"-> Sibling WorkerW (Target): {targetWorkerW}");
                    return false; // Stop enumerating
                }
            }
            return true;
        }, IntPtr.Zero);

        if (targetWorkerW != IntPtr.Zero)
        {
            Logger.Info($"Selected WorkerW handle: {targetWorkerW}");
            insertAfter = HWND_TOP; // We want to be the top child of this background WorkerW
            return targetWorkerW;
        }

        Logger.Warn("Could not find suitable WorkerW. Falling back to Progman.");
        return progman;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr hWnd);
}
