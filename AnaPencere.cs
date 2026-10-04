using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RustMakro;

internal sealed class AnaPencere : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ComboBox _cmbMod;
    private readonly ComboBox _cmbHiz;
    private readonly TextBox _txtBind;
    private readonly Button _btnBindKaydet;
    private readonly Button _btnToggle;
    private readonly Label _lblDurum;
    private readonly Panel _panel;
    private readonly IntPtr _mouseHookHandle;
    private readonly LowLevelMouseProc _mouseHookProc;

    private const int WH_MOUSE_LL = 14;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_XBUTTONUP = 0x020C;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private bool _makroAktif = false;
    private Keys _bindKey = Keys.Insert;
    private bool _bindCaptureMode = false;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public AnaPencere()
    {
        Text = "Rust Makro";
        ClientSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.Black;
        Padding = new Padding(0);
        DoubleBuffered = true;

        BackgroundImage = Image.FromFile(
            Path.Combine(AppContext.BaseDirectory, "arka plan.png"));
        BackgroundImageLayout = ImageLayout.Stretch;

        _panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 0, 0, 0),
            Padding = new Padding(25)
        };
        Controls.Add(_panel);

        _lblDurum = new Label
        {
            Text = "Durum: Kapalı",
            Location = new Point(30, 70),
            Size = new Size(260, 28),
            ForeColor = Color.FromArgb(255, 255, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Padding = new Padding(0)
        };
        _panel.Controls.Add(_lblDurum);

        var lblMod = new Label
        {
            Text = "Mod:",
            Location = new Point(30, 165),
            Size = new Size(60, 25),
            ForeColor = Color.FromArgb(200, 220, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _panel.Controls.Add(lblMod);

        _cmbMod = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(95, 160),
            Size = new Size(180, 30),
            BackColor = Color.FromArgb(245, 247, 250),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbMod.Items.AddRange(new object[]
        {
            "Aşağı Git",
            "Yukarı Git",
            "Sağa Git",
            "Sola Git",
            "Sol Tık",
            "Sağ Tık"
        });
        _cmbMod.SelectedIndex = 0;
        _panel.Controls.Add(_cmbMod);

        var lblHiz = new Label
        {
            Text = "Hız:",
            Location = new Point(290, 165),
            Size = new Size(45, 25),
            ForeColor = Color.FromArgb(200, 220, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _panel.Controls.Add(lblHiz);

        _cmbHiz = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(340, 160),
            Size = new Size(90, 30),
            BackColor = Color.FromArgb(245, 247, 250),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbHiz.Items.AddRange(new object[] { 25, 50, 75, 100 });
        _cmbHiz.SelectedIndex = 0;
        _panel.Controls.Add(_cmbHiz);

        var lblBind = new Label
        {
            Text = "Bind:",
            Location = new Point(30, 215),
            Size = new Size(60, 25),
            ForeColor = Color.FromArgb(200, 220, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _panel.Controls.Add(lblBind);

        _txtBind = new TextBox
        {
            Text = "Insert",
            ReadOnly = true,
            Location = new Point(95, 210),
            Size = new Size(180, 30),
            BackColor = Color.FromArgb(245, 247, 250),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _panel.Controls.Add(_txtBind);

        _btnBindKaydet = new Button
        {
            Text = "Bind Seç",
            Location = new Point(290, 210),
            Size = new Size(130, 32),
            BackColor = Color.FromArgb(90, 140, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnBindKaydet.FlatAppearance.BorderSize = 0;
        _btnBindKaydet.Click += (_, __) =>
        {
            _bindCaptureMode = true;
            _txtBind.Text = "Tuş bekleniyor...";
            _lblDurum.Text = "Bind: tuş bekleniyor";
        };
        _panel.Controls.Add(_btnBindKaydet);

        _btnToggle = new Button
        {
            Text = "Aç / Kapat",
            Location = new Point(30, 270),
            Size = new Size(220, 42),
            BackColor = Color.FromArgb(30, 160, 90),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnToggle.FlatAppearance.BorderSize = 0;
        _btnToggle.MouseEnter += (_, __) =>
        {
            _btnToggle.FlatAppearance.BorderSize = 1;
            _btnToggle.FlatAppearance.BorderColor = Color.FromArgb(120, 210, 255);
        };
        _btnToggle.MouseLeave += (_, __) =>
        {
            _btnToggle.FlatAppearance.BorderSize = 0;
            _btnToggle.FlatAppearance.BorderColor = Color.FromArgb(0, 0, 0, 0);
        };
        _btnToggle.Click += (_, __) =>
        {
            _makroAktif = !_makroAktif;
            _lblDurum.Text = _makroAktif ? "Durum: Açık" : "Durum: Kapalı";
        };
        _panel.Controls.Add(_btnToggle);

        _btnBindKaydet.MouseEnter += (_, __) =>
        {
            _btnBindKaydet.FlatAppearance.BorderSize = 1;
            _btnBindKaydet.FlatAppearance.BorderColor = Color.FromArgb(180, 220, 255);
        };
        _btnBindKaydet.MouseLeave += (_, __) =>
        {
            _btnBindKaydet.FlatAppearance.BorderSize = 0;
            _btnBindKaydet.FlatAppearance.BorderColor = Color.FromArgb(0, 0, 0, 0);
        };

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 100;
        _timer.Tick += (_, __) =>
        {
            if (!_makroAktif) return;

            int hiz = _cmbHiz.SelectedItem is int value ? value : 25;

            switch (_cmbMod.SelectedItem?.ToString())
            {
                case "Aşağı Git":
                    Makro.MouseAsagiGit(hiz);
                    break;
                case "Yukarı Git":
                    Makro.MouseYukariGit(hiz);
                    break;
                case "Sağa Git":
                    Makro.MouseSagaGit(hiz);
                    break;
                case "Sola Git":
                    Makro.MouseSolaGit(hiz);
                    break;
                case "Sol Tık":
                    Makro.SolTiklama();
                    break;
                case "Sağ Tık":
                    Makro.SagTiklama();
                    break;
            }
        };

        _timer.Start();

        _mouseHookProc = MouseHookCallback;
        _mouseHookHandle = SetWindowsHookEx(
            WH_MOUSE_LL,
            _mouseHookProc,
            Marshal.GetHINSTANCE(typeof(AnaPencere).Module),
            0);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (_bindCaptureMode)
            {
                _bindKey = e.KeyCode;
                _bindCaptureMode = false;
                _txtBind.Text = _bindKey.ToString();
                _lblDurum.Text = "Durum: Kapalı | Bind: " + _bindKey;
                e.Handled = true;
                return;
            }

            if (e.KeyCode == _bindKey)
            {
                _makroAktif = !_makroAktif;
                _lblDurum.Text = _makroAktif ? "Durum: Açık" : "Durum: Kapalı";
                e.Handled = true;
            }
        };

        MouseDown += (_, e) =>
        {
            if (_bindCaptureMode)
            {
                if (e.Button == MouseButtons.XButton1)
                    _bindKey = Keys.XButton1;
                else if (e.Button == MouseButtons.XButton2)
                    _bindKey = Keys.XButton2;
                else
                    return;

                _bindCaptureMode = false;
                _txtBind.Text = _bindKey.ToString();
                _lblDurum.Text = "Durum: Kapalı | Bind: " + _bindKey;
            }
        };
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_XBUTTONDOWN)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var button = (ushort)((hookStruct.mouseData >> 16) & 0xFFFF);
            var key = button switch
            {
                1 => Keys.XButton1,
                2 => Keys.XButton2,
                _ => Keys.None
            };

            if (key == Keys.None)
                return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);

            if (_bindCaptureMode)
            {
                _bindKey = key;
                _bindCaptureMode = false;
                if (InvokeRequired)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _txtBind.Text = _bindKey.ToString();
                        _lblDurum.Text = "Durum: Kapalı | Bind: " + _bindKey;
                    }));
                }
                else
                {
                    _txtBind.Text = _bindKey.ToString();
                    _lblDurum.Text = "Durum: Kapalı | Bind: " + _bindKey;
                }
            }
            else if (_bindKey == key)
            {
                if (InvokeRequired)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _makroAktif = !_makroAktif;
                        _lblDurum.Text = _makroAktif ? "Durum: Açık" : "Durum: Kapalı";
                    }));
                }
                else
                {
                    _makroAktif = !_makroAktif;
                    _lblDurum.Text = _makroAktif ? "Durum: Açık" : "Durum: Kapalı";
                }
            }
        }

        return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        if (_mouseHookHandle != IntPtr.Zero)
            UnhookWindowsHookEx(_mouseHookHandle);
    }
}