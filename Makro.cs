using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RustMakro
{
    public static class Makro
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private static readonly Random Rnd = new Random();

        public static void MoveSmooth(double x, double y, bool randomize = true)
        {
            int finalX = (int)Math.Round(x);
            int finalY = (int)Math.Round(y);

            if (randomize)
            {
                // Anti-Cheat bypass için mikro sapmalar (humanizer)
                finalX += Rnd.Next(-1, 2);
                finalY += Rnd.Next(-1, 2);
            }

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = finalX;
            inputs[0].mi.dy = finalY;
            inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE;
            inputs[0].mi.time = 0;
            inputs[0].mi.dwExtraInfo = IntPtr.Zero;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static bool IsLeftMouseButtonDown()
        {
            // Win32 GetAsyncKeyState ile sol tık kontrolü (0x01 = VK_LBUTTON)
            return (NativeMethods.GetAsyncKeyState(0x01) & 0x8000) != 0;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }
}