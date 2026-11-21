using static HuntKdaCalculator.Ui;

namespace HuntKdaCalculator;

/// <summary>Окно без рамки и крестика: клик по любому пустому месту сворачивает его в трей,
/// перетаскивание (зажать и потянуть) двигает окно, правая кнопка открывает меню настроек.</summary>
sealed class MainForm : Form
{
    readonly AppState _state;
    readonly StatField _k = new("Kills"), _d = new("Deaths"), _a = new("Assists");
    readonly CardPanel _card = new();
    readonly Panel _kdaHost = new();
    readonly Label _kda = new(), _kdaSub = new(), _upHead = new(), _downHead = new();
    readonly RowsControl _up = new(3), _down = new(1);
    readonly Panel _sepC = Sep(), _sepD = Sep();

    // перетаскивание / клик
    bool _tracking, _dragging;
    Point _downCursor, _downLocation;

    /// <summary>Меню настроек (то же, что в трее) — показывается по правому клику в окне.</summary>
    public ContextMenuStrip? TrayMenu { get; set; }

    /// <summary>«Поверх всех окон» — управляется из меню, сохраняется в state.json.</summary>
    public bool AlwaysOnTop
    {
        get => TopMost;
        set { TopMost = value; _state.TopMost = value; _state.Save(); }
    }

    static Label Lbl(string text, Font font, Color color, Size size, ContentAlignment align, Point at) => new()
    {
        Text = text, Font = font, ForeColor = color, BackColor = Color.Transparent,
        AutoSize = false, Size = size, TextAlign = align, Margin = new Padding(0), Location = at
    };

    public MainForm(AppState state, Icon icon, Image emblem)
    {
        _state = state;

        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Hunt KDA";
        Icon = icon;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;   // без заголовка и крестика
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        BackColor = Theme.Bg;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // шапка: эмблема + название
        var brand = new Panel { Size = new Size(Z(288), Z(50)), BackColor = Color.Transparent, Margin = new Padding(0) };
        var crest = new PictureBox
        {
            Image = emblem, SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(Z(46), Z(46)), BackColor = Color.Transparent
        };
        var title = new Label
        {
            AutoSize = true, BackColor = Color.Transparent, ForeColor = Theme.Bone,
            Font = Fnt("Georgia", 11f), Text = Sp("HUNT · KDA", "\u200A\u200A")
        };
        brand.Controls.AddRange(new Control[] { crest, title });
        int tw = TextRenderer.MeasureText(title.Text, title.Font).Width;
        int x0 = Math.Max(0, (Z(288) - (Z(46) + Z(14) + tw)) / 2);
        crest.Location = new Point(x0, Z(2));
        title.Location = new Point(x0 + Z(60), (Z(50) - title.PreferredHeight) / 2);

        // блок KDA
        _kdaHost.Size = new Size(Z(288), Z(126));
        _kdaHost.BackColor = Color.Transparent;
        _kdaHost.Margin = new Padding(0);
        _kdaHost.Controls.Add(Lbl(Sp("KDA В ИГРЕ"), Fnt("Georgia", 8f), Theme.Muted, new Size(Z(288), Z(18)), ContentAlignment.MiddleCenter, new Point(0, 0)));
        _kda.Font = Fnt("Georgia", 40f); _kda.ForeColor = Theme.Bone;
        _kda.BackColor = Color.Transparent; _kda.AutoSize = false;
        _kda.Size = new Size(Z(288), Z(82)); _kda.Location = new Point(0, Z(16)); _kda.TextAlign = ContentAlignment.MiddleCenter;
        _kdaSub.Font = Fnt("Consolas", 10f); _kdaSub.ForeColor = Theme.Muted;
        _kdaSub.BackColor = Color.Transparent; _kdaSub.AutoSize = false;
        _kdaSub.Size = new Size(Z(288), Z(22)); _kdaSub.Location = new Point(0, Z(98)); _kdaSub.TextAlign = ContentAlignment.MiddleCenter;
        _kdaHost.Controls.AddRange(new Control[] { _kda, _kdaSub });

        // заголовки секций
        Setup(_upHead, Sp("РОСТ · УБИЙСТВ НУЖНО"));
        Setup(_downHead, Sp("ПАДЕНИЕ · СМЕРТЕЙ НУЖНО"));

        _card.Controls.AddRange(new Control[]
        {
            brand, Sep(), _k, _d, _a, Sep(),
            _kdaHost, _sepC, _upHead, _up, _sepD, _downHead, _down
        });
        _card.Watermark = emblem;
        _card.WatermarkHost = _kdaHost;
        _card.Location = Point.Empty;
        Controls.Add(_card);
        HookClicks(_card);

        // загрузка сохранённых значений (события ещё не подписаны)
        _k.Value = state.Kills;
        _d.Value = state.Deaths;
        _a.Value = state.Assists;
        TopMost = state.TopMost;
        Recalc();

        _k.ValueChanged += OnChanged;
        _d.ValueChanged += OnChanged;
        _a.ValueChanged += OnChanged;
    }

    static void Setup(Label l, string text)
    {
        l.Text = text; l.Font = Fnt("Georgia", 8f); l.ForeColor = Theme.Muted; l.BackColor = Color.Transparent;
        l.AutoSize = false; l.Size = new Size(Z(288), Z(20)); l.TextAlign = ContentAlignment.MiddleLeft;
        l.Margin = new Padding(0, 0, 0, Z(2));
    }

    // ---------- клик по пустому месту = свернуть; зажать и потянуть = переместить ----------

    void HookClicks(Control parent)
    {
        Attach(parent);
        foreach (Control c in parent.Controls)
        {
            if (c is Button or TextBox) continue;                       // интерактивные элементы
            if (c is StatField sf) { Attach(sf); Attach(sf.Caption); continue; }
            HookClicks(c);
        }
    }

    void Attach(Control c)
    {
        c.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _tracking = true; _dragging = false;
            _downCursor = Cursor.Position; _downLocation = Location;
        };
        c.MouseMove += (_, _) =>
        {
            if (!_tracking) return;
            var p = Cursor.Position;
            int dx = p.X - _downCursor.X, dy = p.Y - _downCursor.Y;
            if (!_dragging && Math.Abs(dx) < 5 && Math.Abs(dy) < 5) return;
            _dragging = true;
            Location = new Point(_downLocation.X + dx, _downLocation.Y + dy);
        };
        c.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right) { TrayMenu?.Show(Cursor.Position); return; }
            if (e.Button != MouseButtons.Left || !_tracking) return;
            _tracking = false;
            if (_dragging)
            {
                _state.WindowX = Location.X;
                _state.WindowY = Location.Y;
                _state.Save();
            }
            else HideToTray();
        };
    }

    // ---------- расчёты ----------

    void OnChanged(object? sender, EventArgs e)
    {
        _state.Kills = _k.Value;
        _state.Deaths = _d.Value;
        _state.Assists = _a.Value;
        _state.Save();   // немедленная запись при каждом изменении
        Recalc();
    }

    void Recalc()
    {
        var r = KdaCalc.Compute(_k.Value, _d.Value, _a.Value);
        bool show = !r.Infinite;
        _sepC.Visible = _upHead.Visible = _up.Visible = _sepD.Visible = _downHead.Visible = _down.Visible = show;

        if (r.Infinite)
        {
            _kda.Text = "∞";
            _kdaSub.Text = "0 смертей";
        }
        else
        {
            _kda.Text = KdaCalc.Fmt2(r.Display100);
            _kdaSub.Text = $"({r.Exact})";

            _up.Set(null,
                r.Up.Select(u => (KdaCalc.Fmt2(u.Target100), "+" + u.Kills + " K", "→ " + u.Exact)),
                Theme.Up);

            if (r.DownTarget100 is long t)
                _down.Set(null,
                    new[] { (KdaCalc.Fmt2(t), "+" + r.DownDeaths + " D", "→ " + r.DownExact) },
                    Theme.Down);
            else
                _down.Set("Недоступно: KDA уже 0.00", Array.Empty<(string, string, string)>(), Theme.Down);
        }
        _card.Invalidate();
    }

    // ---------- показ / скрытие / сохранение ----------

    public void SaveNow() => _state.Save();

    public void HideToTray()
    {
        SaveNow();
        Hide();
    }

    public void ShowAndFocus()
    {
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_state.WindowX is int x && _state.WindowY is int y)
        {
            var r = new Rectangle(x, y, Width, Height);
            if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(r)))
            {
                Location = new Point(x, y);
                return;
            }
        }
        CenterToScreen();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { e.Handled = true; HideToTray(); return; }
        base.OnKeyDown(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveNow(); // при Alt+F4, выключении ПК, выходе из сеанса
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true; // закрытие только прячет окно в трей
            Hide();
        }
        base.OnFormClosing(e);
    }
}
