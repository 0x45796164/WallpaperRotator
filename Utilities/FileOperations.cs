using System;
using System.Runtime.InteropServices;

namespace WallpaperRotator.Utilities
{
    public static class FileOperations
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        private const int FO_DELETE = 0x0003;
        private const int FOF_ALLOWUNDO = 0x0040; // Preserve Undo information, if possible.
        private const int FOF_NOCONFIRMATION = 0x0010; // Respond with "Yes" to All for any dialog box that is displayed.

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        public static bool DeleteToRecycleBin(string path)
        {
            try
            {
                var shf = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = path + '\0' + '\0', // Double null terminated
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION
                };

                return SHFileOperation(ref shf) == 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete to recycle bin: {ex.Message}");
                return false;
            }
        }
    }
}
