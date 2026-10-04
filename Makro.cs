using System;
using System.Runtime.InteropServices;

namespace RustMakro
{
    public static class Makro
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
        }

        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        public static void MouseAsagiGit(int piksel = 10)
        {
            GetCursorPos(out Point konum);
            SetCursorPos(konum.X, konum.Y + piksel);
        }

        public static void MouseYukariGit(int piksel = 10)
        {
            GetCursorPos(out Point konum);
            SetCursorPos(konum.X, konum.Y - piksel);
        }

        public static void MouseSagaGit(int piksel = 10)
        {
            GetCursorPos(out Point konum);
            SetCursorPos(konum.X + piksel, konum.Y);
        }

        public static void MouseSolaGit(int piksel = 10)
        {
            GetCursorPos(out Point konum);
            SetCursorPos(konum.X - piksel, konum.Y);
        }

        public static void SolTiklama()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        public static void SagTiklama()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }
}