using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeepUnlock
{
    internal static class Program
    {
        // SendInputで使うための構造体
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        const int INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;

        // Win32 APIのSendInput関数
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

        static readonly System.Windows.Forms.Timer timer = new() { Interval = 30 * 1000 };

        [STAThread]
        static void Main()
        {
            timer.Enabled = false;
            timer.Tick += (s, e) =>
            {
                MoveMouse(0, 0);
            };
            ApplicationConfiguration.Initialize();
            ContextMenuStrip menu = new();
            ContextMenuStrip contextMenu =
                new() { Items = { { "終了", null, (s, e) => Application.Exit() } } };

            NotifyIcon notifyIcon =
                new()
                {
                    Text = "KeepUnlock 無効",
                    Visible = true,
                    Icon = Properties.Icons._lock,
                };

            Form contextForm =
                new() { ShowInTaskbar = false, FormBorderStyle = FormBorderStyle.None };
            contextForm.Deactivate += (s, e) => contextForm.Hide(); // Formが非アクティブになったら隠す

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    timer.Enabled = !timer.Enabled;
                    notifyIcon.Text = timer.Enabled ? "KeepUnlock 有効" : "KeepUnlock 無効";
                    notifyIcon.Icon = timer.Enabled
                        ? Properties.Icons.unlock
                        : Properties.Icons._lock;
                }
            };
            Application.ApplicationExit += (s, e) => notifyIcon.Visible = false;
            SystemEvents.SessionSwitch += (s, e) =>
            {
                switch (e.Reason)
                {
                    case SessionSwitchReason.SessionLock:
                        timer.Enabled = false;
                        notifyIcon.Text = "KeepUnlock 無効";
                        notifyIcon.Icon = Properties.Icons._lock;
                        break;

                    case SessionSwitchReason.SessionRemoteControl:
                        Application.Exit();
                        break;
                }
            };
            Application.Run();
        }

        static void MoveMouse(int dx, int dy)
        {
            INPUT[] inputs = new INPUT[1];

            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = dx; // x方向の移動量
            inputs[0].mi.dy = dy; // y方向の移動量
            inputs[0].mi.mouseData = 0;
            inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE; // マウス移動のフラグ
            inputs[0].mi.time = 0;
            inputs[0].mi.dwExtraInfo = IntPtr.Zero;

            // SendInput関数でマウス入力をシミュレーション
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                Console.WriteLine(
                    "Failed to move the mouse. Error: " + Marshal.GetLastWin32Error()
                );
            }
        }
    }
}
