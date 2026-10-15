using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using RustMakro;

internal sealed class AnaPencere : Form
{
    private readonly System.Windows.Forms.Timer _recoilTimer;
    private readonly ComboBox _cmbSilah;
    private readonly ComboBox _cmbDurbun;
    private readonly ComboBox _cmbTetikTusu;
    private readonly CheckBox _chkHumanizer;
    private readonly TextBox _txtBind;
    private readonly Button _btnBindKaydet;
    private readonly Button _btnToggle;
    private readonly Label _lblDurum;
    private readonly Label _lblInfo;
    private readonly Panel _panel;

    private readonly IntPtr _mouseHookHandle;
    private readonly LowLevelMouseProc _mouseHookProc;

    private const int WH_MOUSE_LL = 14;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_XBUTTONUP = 0x020C;
    private const int HOTKEY_ID = 1;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private bool _makroAktif = false;
    private Keys _bindKey = Keys.Insert;
    private bool _bindCaptureMode = false;
    private int _bulletIndex = 0;
    private long _shotStartTime = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public AnaPencere()
    {
        Text = "MADE BY IF ELSE";
        ClientSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.Black;
        DoubleBuffered = true;

        string bgPath = Path.Combine(AppContext.BaseDirectory, "arka_plan.png");
        if (File.Exists(bgPath))
        {
            BackgroundImage = Image.FromFile(bgPath);
            BackgroundImageLayout = ImageLayout.Stretch;
        }

        _panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(170, 10, 15, 30),
            Padding = new Padding(25)
        };
        Controls.Add(_panel);

        var lblTitle = new Label
        {
            Text = "RUST RECOIL CONTROL",
            Location = new Point(30, 25),
            Size = new Size(400, 35),
            ForeColor = Color.Cyan,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 14, FontStyle.Bold)
        };
        _panel.Controls.Add(lblTitle);

        _lblDurum = new Label
        {
            Text = "Durum: [ KAPALI ]",
            Location = new Point(30, 75),
            Size = new Size(300, 30),
            ForeColor = Color.Red,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        _panel.Controls.Add(_lblDurum);

        var lblSilah = new Label
        {
            Text = "Silah:",
            Location = new Point(30, 135),
            Size = new Size(80, 25),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _panel.Controls.Add(lblSilah);

        _cmbSilah = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(140, 130),
            Size = new Size(190, 30),
            BackColor = Color.FromArgb(30, 30, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbSilah.Items.AddRange(new object[] { "AK-47", "LR-300", "MP5A4", "Custom SMG", "Thompson" });
        _cmbSilah.SelectedIndex = 0;
        _panel.Controls.Add(_cmbSilah);

        var lblDurbun = new Label
        {
            Text = "Eklenti:",
            Location = new Point(30, 180),
            Size = new Size(80, 25),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _panel.Controls.Add(lblDurbun);

        _cmbDurbun = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(140, 175),
            Size = new Size(190, 30),
            BackColor = Color.FromArgb(30, 30, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbDurbun.Items.AddRange(new object[] { "Normal (Iron Sights)", "Holo Sight (80%)", "8X Scope (35%)" });
        _cmbDurbun.SelectedIndex = 0;
        _panel.Controls.Add(_cmbDurbun);

        // Yeni Eklenen Özellik: Makro Çalışma Tetikleyicisi (Envanter Koruması İçin)
        var lblTetik = new Label
        {
            Text = "Çalışma Modu:",
            Location = new Point(30, 225),
            Size = new Size(105, 25),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _panel.Controls.Add(lblTetik);

        _cmbTetikTusu = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(140, 220),
            Size = new Size(190, 30),
            BackColor = Color.FromArgb(30, 30, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbTetikTusu.Items.AddRange(new object[] { "Sadece Sol Tık (Normal)", "Sol Tık + Yan Tuş Basılıyken", "Sadece Sağ Tık Basılıyken" });
        _cmbTetikTusu.SelectedIndex = 1; // Varsayılan olarak güvenli mod (Yan tuş / kombinasyon)
        _panel.Controls.Add(_cmbTetikTusu);

        _chkHumanizer = new CheckBox
        {
            Text = "Anti-Cheat Humanizer (Doğal Sapma)",
            Location = new Point(30, 270),
            Size = new Size(300, 25),
            ForeColor = Color.LightGreen,
            BackColor = Color.Transparent,
            Checked = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        _panel.Controls.Add(_chkHumanizer);

        var lblBind = new Label
        {
            Text = "Açma Tuşu:",
            Location = new Point(30, 315),
            Size = new Size(90, 25),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _panel.Controls.Add(lblBind);

        _txtBind = new TextBox
        {
            Text = "Insert",
            ReadOnly = true,
            Location = new Point(140, 310),
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(30, 30, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _panel.Controls.Add(_txtBind);

        _btnBindKaydet = new Button
        {
            Text = "Tuş Ata",
            Location = new Point(250, 310),
            Size = new Size(80, 32),
            BackColor = Color.FromArgb(60, 90, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnBindKaydet.FlatAppearance.BorderSize = 0;
        _btnBindKaydet.Click += (_, __) =>
        {
            _bindCaptureMode = true;
            UnregisterHotKey(Handle, HOTKEY_ID);
            _txtBind.Text = "...";
            _lblDurum.Text = "Durum: [ TUŞ BEKLENİYOR ]";
        };
        _panel.Controls.Add(_btnBindKaydet);

        _btnToggle = new Button
        {
            Text = "MAKROYU AKTİFLEŞTİR",
            Location = new Point(30, 370),
            Size = new Size(300, 45),
            BackColor = Color.FromArgb(200, 40, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnToggle.FlatAppearance.BorderSize = 0;
        _btnToggle.Click += (_, __) => ToggleMacro();
        _panel.Controls.Add(_btnToggle);

        _lblInfo = new Label
        {
            Text = "Bilgi: Çalışma modunu 'Sol Tık + Yan Tuş' yaparak envanterde eşya düzenlerken makronun devreye girmesini tamamen engelleyebilirsin.",
            Location = new Point(30, 435),
            Size = new Size(450, 50),
            ForeColor = Color.DarkGray,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9, FontStyle.Italic)
        };
        _panel.Controls.Add(_lblInfo);

        _recoilTimer = new System.Windows.Forms.Timer { Interval = 10 };
        _recoilTimer.Tick += RecoilLoopTick;
        _recoilTimer.Start();

        _mouseHookProc = MouseHookCallback;
        _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, Marshal.GetHINSTANCE(typeof(AnaPencere).Module), 0);

        UpdateKeyboardHotkey();

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (_bindCaptureMode)
            {
                _bindKey = e.KeyCode;
                _bindCaptureMode = false;
                _txtBind.Text = _bindKey.ToString();
                UpdateKeyboardHotkey();
                _lblDurum.Text = "Durum: [ KAPALI ]";
                e.Handled = true;
            }
        };
    }

    private void ToggleMacro()
    {
        _makroAktif = !_makroAktif;
        if (_makroAktif)
        {
            _lblDurum.Text = "Durum: [ AKTİF ]";
            _lblDurum.ForeColor = Color.LimeGreen;
            _btnToggle.BackColor = Color.FromArgb(40, 180, 80);
            _btnToggle.Text = "MAKROYU KAPAT";
        }
        else
        {
            _lblDurum.Text = "Durum: [ KAPALI ]";
            _lblDurum.ForeColor = Color.Red;
            _btnToggle.BackColor = Color.FromArgb(200, 40, 40);
            _btnToggle.Text = "MAKROYU AKTİFLEŞTİR";
            _bulletIndex = 0;
        }
    }

    private void RecoilLoopTick(object? sender, EventArgs e)
    {
        if (!_makroAktif)
        {
            _bulletIndex = 0;
            return;
        }

        bool triggerCondition = false;
        int modeIndex = _cmbTetikTusu.SelectedIndex;

        bool leftDown = Makro.IsLeftMouseButtonDown();
        bool rightDown = (NativeMethods.GetAsyncKeyState(0x02) & 0x8000) != 0;
        bool xButtonDown = (NativeMethods.GetAsyncKeyState(0x05) & 0x8000) != 0; // XButton1 (Mouse yan tuşu)

        // Çalışma moduna göre tetik koşulu
        if (modeIndex == 0) // Sadece Sol Tık
        {
            triggerCondition = leftDown;
        }
        else if (modeIndex == 1) // Sol Tık + Fare Yan Tuşu basılıyken (Envanter korumalı mod)
        {
            triggerCondition = leftDown && xButtonDown;
        }
        else if (modeIndex == 2) // Sadece Sağ Tık (Target / ADS durumunda)
        {
            triggerCondition = leftDown && rightDown;
        }

        if (triggerCondition)
        {
            if (_bulletIndex == 0) _shotStartTime = Environment.TickCount;

            long elapsed = Environment.TickCount - _shotStartTime;
            _bulletIndex = (int)(elapsed / 100) + 1;

            ApplyWeaponRecoil();
        }
        else
        {
            _bulletIndex = 0;
        }
    }

    private void ApplyWeaponRecoil()
    {
        string silah = _cmbSilah.SelectedItem?.ToString() ?? "AK-47";
        double multiplier = 1.0;

        if (_cmbDurbun.SelectedIndex == 1) multiplier = 0.8;
        else if (_cmbDurbun.SelectedIndex == 2) multiplier = 0.35;

        double xMove = 0;
        double yMove = 0;

        switch (silah)
        {
            case "AK-47":
                yMove = (_bulletIndex < 5 ? 14.5 : 11.2) * multiplier;
                xMove = (_bulletIndex % 2 == 0 ? 3.5 : -3.5) * multiplier;
                break;
            case "LR-300":
                yMove = 9.5 * multiplier;
                xMove = (_bulletIndex % 2 == 0 ? 1.5 : -1.5) * multiplier;
                break;
            case "MP5A4":
                yMove = 7.0 * multiplier;
                xMove = (_bulletIndex % 2 == 0 ? 2.0 : -2.0) * multiplier;
                break;
            case "Custom SMG":
                yMove = 6.0 * multiplier;
                xMove = 1.0 * multiplier;
                break;
            case "Thompson":
                yMove = 8.5 * multiplier;
                xMove = (_bulletIndex % 2 == 0 ? 2.5 : -2.5) * multiplier;
                break;
        }

        bool humanizer = _chkHumanizer.Checked;
        Makro.MoveSmooth(xMove, yMove, humanizer);
    }

    private void UpdateKeyboardHotkey()
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        if (!RegisterHotKey(Handle, HOTKEY_ID, 0, (uint)_bindKey))
            _lblDurum.Text = "Hata: Tuş kullanımda!";
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312 && m.WParam == (IntPtr)HOTKEY_ID && !_bindCaptureMode)
        {
            ToggleMacro();
            return;
        }
        base.WndProc(ref m);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_XBUTTONDOWN)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            int button = (ushort)((hookStruct.mouseData >> 16) & 0xFFFF);
            Keys key = button == 1 ? Keys.XButton1 : button == 2 ? Keys.XButton2 : Keys.None;

            if (key != Keys.None && _bindCaptureMode)
            {
                _bindKey = key;
                _bindCaptureMode = false;
                Invoke((Action)(() =>
                {
                    _txtBind.Text = _bindKey.ToString();
                    _lblDurum.Text = "Durum: [ KAPALI ]";
                }));
            }
            else if (key != Keys.None && _bindKey == key)
            {
                Invoke((Action)ToggleMacro);
            }
        }
        return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        UnregisterHotKey(Handle, HOTKEY_ID);
        if (_mouseHookHandle != IntPtr.Zero)
            UnhookWindowsHookEx(_mouseHookHandle);
    }
}
