using static HuntKdaCalculator.Ui;

namespace HuntKdaCalculator;

/// <summary>Подпись + [−] + поле + [+]. Цифры, ↑/↓ (Shift = ±10), колесо, удержание кнопки = автоповтор.</summary>
sealed class StatField : Panel
{
    const int MaxValue = 999_999;
    readonly TextBox _box = new();
    readonly Panel _wrap = new();
    readonly Label _caption = new();
    readonly System.Windows.Forms.Timer _repeat = new();
    int _repeatDelta;
    bool _silent;

    public event EventHandler? ValueChanged;

    /// <summary>Подпись поля (нужна, чтобы клик по ней тоже сворачивал окно).</summary>
    public Control Caption => _caption;

    public StatField(string caption)
    {
        Size = new Size(Z(W), Z(52));
        Margin = new Padding(0, 0, 0, Z(8));
        DoubleBuffered = true;

        _caption.Text = Sp(caption.ToUpperInvariant());
        _caption.Location = new Point(0, 0);
        _caption.Size = new Size(Z(100), Z(52));
        _caption.TextAlign = ContentAlignment.MiddleLeft;
        _caption.ForeColor = Theme.Muted;
        _caption.BackColor = Color.Transparent;
        _caption.Font = Fnt("Georgia", 8.5f);
        Controls.Add(_caption);
        Controls.Add(MakeButton("−", Z(106), -1));
        Controls.Add(MakeButton("+", Z(W - 48), +1));

        _wrap.Location = new Point(Z(160), Z(2));
        _wrap.Size = new Size(Z(W - 214), Z(48));
        _wrap.BackColor = Theme.Field;
        _wrap.Paint += (_, e) =>
        {
            using var p = new Pen(_box.Focused ? Theme.Bone : Theme.Border2);
            e.Graphics.DrawRectangle(p, 0, 0, _wrap.Width - 1, _wrap.Height - 1);
        };
        _wrap.Click += (_, _) => _box.Focus();

        _box.BorderStyle = BorderStyle.None;
        _box.Font = Fnt("Georgia", 20f);
        _box.BackColor = Theme.Field;
        _box.ForeColor = Theme.Bone;
        _box.TextAlign = HorizontalAlignment.Center;
        _box.MaxLength = 6;
        _box.Text = "0";
        _box.Width = Z(W - 214 - 8);
        _box.Location = new Point(Z(4), (Z(48) - _box.Height) / 2);
        _wrap.Controls.Add(_box);
        Controls.Add(_wrap);

        _box.KeyPress += (_, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsAsciiDigit(e.KeyChar)) e.Handled = true;
        };
        _box.TextChanged += (_, _) =>
        {
            if (_silent) return;
            var digits = new string(_box.Text.Where(char.IsAsciiDigit).ToArray());
            if (digits != _box.Text)
            {
                _silent = true;
                _box.Text = digits;
                _box.SelectionStart = digits.Length;
                _silent = false;
            }
            ValueChanged?.Invoke(this, EventArgs.Empty);
        };
        _box.KeyDown += (_, e) =>
        {
            int big = e.Shift ? 10 : 1;
            if (e.KeyCode == Keys.Up) { Step(+big); e.SuppressKeyPress = e.Handled = true; }
            else if (e.KeyCode == Keys.Down) { Step(-big); e.SuppressKeyPress = e.Handled = true; }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = e.Handled = true;
                FindForm()?.SelectNextControl(_box, true, true, true, true);
            }
        };
        _box.MouseWheel += (_, e) =>
        {
            Step(e.Delta > 0 ? +1 : -1);
            if (e is HandledMouseEventArgs h) h.Handled = true;
        };
        _box.Enter += (_, _) =>
        {
            _wrap.BackColor = _box.BackColor = Theme.FieldFocus;
            _wrap.Invalidate();
            BeginInvoke(new Action(_box.SelectAll));
        };
        _box.Leave += (_, _) =>
        {
            _wrap.BackColor = _box.BackColor = Theme.Field;
            _wrap.Invalidate();
            Value = Value; // "" -> "0", "007" -> "7"
        };

        _repeat.Tick += (_, _) => { _repeat.Interval = 70; Step(_repeatDelta); };
    }

    public int Value
    {
        get => int.TryParse(_box.Text, out var v) ? v : 0;
        set
        {
            _silent = true;
            _box.Text = Math.Clamp(value, 0, MaxValue).ToString();
            _silent = false;
        }
    }

    void Step(int delta)
    {
        int old = Value;
        Value = old + delta;
        if (Value != old) ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    Button MakeButton(string text, int x, int delta)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, Z(2)),
            Size = new Size(Z(48), Z(48)),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Field,
            ForeColor = Theme.Fg,
            Font = Fnt("Georgia", 16f),
            TabStop = false,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderColor = Theme.Border2;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = Theme.Field;
        b.FlatAppearance.MouseDownBackColor = Color.FromArgb(42, 21, 18);
        b.MouseEnter += (_, _) => b.FlatAppearance.BorderColor = Theme.Blood;
        b.MouseLeave += (_, _) => { b.FlatAppearance.BorderColor = Theme.Border2; _repeat.Stop(); };
        b.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _repeatDelta = delta;
            Step(delta);
            _repeat.Interval = 400;
            _repeat.Start();
        };
        b.MouseUp += (_, _) => _repeat.Stop();
        return b;
    }
}
