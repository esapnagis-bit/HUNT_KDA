using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HuntKdaCalculator;

static class Theme
{
    public static readonly Color Bg = Color.FromArgb(9, 8, 7);
    public static readonly Color Card = Color.FromArgb(16, 14, 13);
    public static readonly Color Field = Color.FromArgb(22, 19, 17);
    public static readonly Color FieldFocus = Color.FromArgb(27, 23, 20);
    public static readonly Color Border = Color.FromArgb(42, 38, 34);
    public static readonly Color Border2 = Color.FromArgb(58, 52, 45);
    public static readonly Color Fg = Color.FromArgb(216, 208, 192);
    public static readonly Color Muted = Color.FromArgb(111, 103, 91);
    public static readonly Color Bone = Color.FromArgb(207, 195, 168);
    public static readonly Color Blood = Color.FromArgb(168, 54, 43);
    public static readonly Color Up = Color.FromArgb(125, 154, 92);
    public static readonly Color Down = Color.FromArgb(184, 64, 47);
}

static class Ui
{
    /// <summary>Общий масштаб интерфейса (1.0 = исходный размер, 1.5 = +50%).</summary>
    public const float Zoom = 1.0f;

    /// <summary>Ширина содержимого карточки (в «исходных» пикселях).</summary>
    public const int W = 320;

    public static int Z(int v) => (int)Math.Round(v * Zoom);
    public static Font Fnt(string name, float pt, FontStyle style = FontStyle.Regular) => new(name, pt * Zoom, style);

    /// <summary>Имитация разрядки букв (letter-spacing) тонкими пробелами.</summary>
    public static string Sp(string s, string gap = "\u200A") => string.Join(gap, s.Select(c => c.ToString()));

    public static Panel Sep() => new()
    {
        Size = new Size(Z(W), 1),
        BackColor = Theme.Border,
        Margin = new Padding(0, Z(10), 0, Z(10))
    };
}

/// <summary>Карточка: двойная рамка, красные угловые засечки, водяной знак за блоком KDA.</summary>
sealed class CardPanel : FlowLayoutPanel
{
    public Image? Watermark { get; set; }
    public Control? WatermarkHost { get; set; }

    public CardPanel()
    {
        DoubleBuffered = true;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(Ui.Z(24));
        BackColor = Theme.Bg;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        float k = DeviceDpi / 96f * Ui.Zoom;
        int m = (int)(7 * k), t = (int)(14 * k);
        var r = ClientRectangle;

        using (var outer = new Pen(Theme.Border))
            g.DrawRectangle(outer, 1, 1, r.Width - 3, r.Height - 3);

        var card = new Rectangle(m, m, r.Width - 2 * m, r.Height - 2 * m);
        using (var fill = new SolidBrush(Theme.Card)) g.FillRectangle(fill, card);
        using (var edge = new Pen(Theme.Border2)) g.DrawRectangle(edge, card.X, card.Y, card.Width - 1, card.Height - 1);

        if (Watermark != null && WatermarkHost is { Visible: true } host)
        {
            var hb = host.Bounds;
            int size = (int)(hb.Height * 1.5);
            var dest = new Rectangle(hb.X + (hb.Width - size) / 2, hb.Y + (hb.Height - size) / 2, size, size);
            var oldClip = g.Clip;
            g.SetClip(hb);
            using var attrs = new ImageAttributes();
            attrs.SetColorMatrix(new ColorMatrix { Matrix33 = 0.16f });
            g.DrawImage(Watermark, dest, 0, 0, Watermark.Width, Watermark.Height, GraphicsUnit.Pixel, attrs);
            g.Clip = oldClip;
        }

        using var tick = new Pen(Theme.Blood, 2 * k);
        g.DrawLines(tick, new[] { new Point(card.Left, card.Top + t), new Point(card.Left, card.Top), new Point(card.Left + t, card.Top) });
        g.DrawLines(tick, new[] { new Point(card.Right - 1 - t, card.Bottom - 1), new Point(card.Right - 1, card.Bottom - 1), new Point(card.Right - 1, card.Bottom - 1 - t) });
    }
}

/// <summary>Таблица результатов: цель слева, сколько нужно справа. Размеры привязаны к Height/Width (DPI-безопасно).</summary>
sealed class RowsControl : Control
{
    readonly int _capacity;
    readonly List<(string A, string B)> _rows = new();
    string? _note;
    Color _accent = Theme.Up;

    static readonly Font Big = Ui.Fnt("Consolas", 11.5f, FontStyle.Bold);
    static readonly Font Small = Ui.Fnt("Consolas", 9.5f);

    public RowsControl(int capacity)
    {
        _capacity = capacity;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Size = new Size(Ui.Z(Ui.W), Ui.Z(26) * capacity);
        Margin = new Padding(0);
    }

    public void Set(string? note, IEnumerable<(string, string)> rows, Color accent)
    {
        _note = note;
        _accent = accent;
        _rows.Clear();
        _rows.AddRange(rows);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var f = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;
        int rowH = Height / _capacity;
        int wA = Width / 2, wB = Width - wA;

        if (_note != null)
        {
            TextRenderer.DrawText(e.Graphics, _note, Small, new Rectangle(0, 0, Width, rowH), Theme.Muted, f | TextFormatFlags.Left);
            return;
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            int y = i * rowH;
            var (a, b) = _rows[i];
            TextRenderer.DrawText(e.Graphics, a, Big, new Rectangle(0, y, wA, rowH), Theme.Fg, f | TextFormatFlags.Left);
            TextRenderer.DrawText(e.Graphics, b, Big, new Rectangle(wA, y, wB, rowH), _accent, f | TextFormatFlags.Right);
        }
    }
}

/// <summary>Невидимая «заглушка» фокуса: на неё встаёт фокус при показе окна, чтобы ни одно поле не подсвечивалось.</summary>
sealed class FocusSink : Control
{
    public FocusSink()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = false;
        Size = new Size(1, 1);
        Location = Point.Empty;
    }
}

/// <summary>Тёмное контекстное меню трея.</summary>
sealed class DarkColors : ProfessionalColorTable
{
    static readonly Color Sel = Color.FromArgb(40, 28, 26);
    public override Color ToolStripDropDownBackground => Theme.Card;
    public override Color ImageMarginGradientBegin => Theme.Card;
    public override Color ImageMarginGradientMiddle => Theme.Card;
    public override Color ImageMarginGradientEnd => Theme.Card;
    public override Color MenuBorder => Theme.Border2;
    public override Color MenuItemBorder => Theme.Blood;
    public override Color MenuItemSelected => Sel;
    public override Color MenuItemSelectedGradientBegin => Sel;
    public override Color MenuItemSelectedGradientEnd => Sel;
    public override Color MenuItemPressedGradientBegin => Sel;
    public override Color MenuItemPressedGradientEnd => Sel;
    public override Color SeparatorDark => Theme.Border;
    public override Color SeparatorLight => Theme.Border;
    public override Color CheckBackground => Sel;
    public override Color CheckSelectedBackground => Sel;
    public override Color CheckPressedBackground => Sel;
}

sealed class DarkRenderer : ToolStripProfessionalRenderer
{
    public DarkRenderer() : base(new DarkColors()) { RoundedEdges = false; }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var r = e.ImageRectangle;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var p = new Pen(Theme.Bone, 2f);
        g.DrawLines(p, new[]
        {
            new Point(r.Left + 3, r.Top + r.Height / 2),
            new Point(r.Left + r.Width / 2 - 1, r.Bottom - 4),
            new Point(r.Right - 3, r.Top + 4)
        });
    }
}
