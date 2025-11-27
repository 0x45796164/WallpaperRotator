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
        IntPtr result = IntPtr.Zero;
        SendMessageTimeout(progman, WM_SPAWN_WORKER, IntPtr.Zero, IntPtr.Zero, 0, 1000, out result);
        Logger.Info($"Sent WM_SPAWN_WORKER message. Result: {result}");

        // 3. Inspect all WorkerW windows
        IntPtr targetWorkerW = IntPtr.Zero;
        
        Logger.Info("Enumerating all WorkerW windows...");
        EnumWindows((hwnd, lParam) =>
        {
            // Get class name
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            
            if (sb.ToString() == "WorkerW")
            {
                // Check children
                IntPtr shellDll = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                Logger.Info($"Found WorkerW: {hwnd}. Has SHELLDLL_DefView: {shellDll != IntPtr.Zero}");
                
                if (shellDll != IntPtr.Zero)
                {
                    // This is the one with icons. We want its sibling.
                    targetWorkerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    Logger.Info($"-> Found SHELLDLL_DefView parent. Sibling WorkerW is: {targetWorkerW}");
                }
            }
            return true;
        }, IntPtr.Zero);

        // Fallback for hidden icons:
        if (targetWorkerW == IntPtr.Zero)
        {
            Logger.Warn("Could not find WorkerW with SHELLDLL_DefView (Icons might be hidden). Looking for any valid WorkerW...");
            
            EnumWindows((hwnd, lParam) =>
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
                GetClassName(hwnd, sb, sb.Capacity);
                
                if (sb.ToString() == "WorkerW")
                {
                     if (IsWindowVisible(hwnd))
                     {
                         Logger.Info($"Found visible WorkerW: {hwnd}. Using as target.");
                         targetWorkerW = hwnd;
                         return false; // Stop
                     }
                }
                return true;
            }, IntPtr.Zero);
        }

        if (targetWorkerW == IntPtr.Zero)
        {
            Logger.Warn("Could not find any suitable WorkerW. Falling back to Progman (Desktop Icons might be hidden).");
            targetWorkerW = progman;
            insertAfter = HWND_BOTTOM;
        }
        else
        {
            Logger.Info($"Selected WorkerW handle: {targetWorkerW}");
            insertAfter = HWND_TOP; // On dedicated WorkerW, we can be top
        }

        return targetWorkerW;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr hWnd);
}
