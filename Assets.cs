namespace HuntKdaCalculator;

/// <summary>Ресурсы, вшитые в .exe (эмблема и иконка).</summary>
static class Assets
{
    static Stream Open(string name) =>
        typeof(Assets).Assembly.GetManifestResourceStream(name) ?? throw new FileNotFoundException(name);

    public static Image Emblem()
    {
        using var s = Open("emblem.png");
        using var img = Image.FromStream(s);
        return new Bitmap(img); // копия, не зависит от потока
    }

    public static Icon AppIcon(Size size)
    {
        using var s = Open("app.ico");
        return new Icon(s, size);
    }
}
