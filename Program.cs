using Microsoft.Win32;

namespace HuntKdaCalculator;

static class Program
{
    [STAThread]
    static void Main()
    {
        // При автозапуске рабочая папка может быть C:\Windows\System32 — привязываемся к папке exe.
        try { Directory.SetCurrentDirectory(AppContext.BaseDirectory); } catch { }

        using var mutex = new Mutex(true, @"Local\HuntKdaCalculator.SingleInstance", out bool isFirst);
        if (!isFirst) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}

/// <summary>Приложение живёт в трее; окно создаётся, но при старте не показывается.</summary>
sealed class TrayContext : ApplicationContext
{
    readonly AppState _state = AppState.Load();   // нет файла -> нули
    readonly Icon _icon = Assets.AppIcon(SystemInformation.SmallIconSize);
    readonly MainForm _form;
    readonly NotifyIcon _tray;
    readonly SessionEndingEventHandler _onSessionEnding;

    public TrayContext()
    {
        _form = new MainForm(_state, _icon, Assets.Emblem());

        var open = new ToolStripMenuItem("Открыть");
        open.Click += (_, _) => _form.ShowAndFocus();

        var onTop = new ToolStripMenuItem("Поверх всех окон") { CheckOnClick = true, Checked = _state.TopMost };
        onTop.Click += (_, _) => _form.AlwaysOnTop = onTop.Checked;

        var autostart = new ToolStripMenuItem("Автозапуск с Windows") { CheckOnClick = true };
        try { autostart.Checked = Autostart.IsEnabled(); } catch { }
        autostart.Click += (_, _) =>
        {
            try { Autostart.Set(autostart.Checked); }
            catch { autostart.Checked = !autostart.Checked; } // не удалось записать в реестр — откат галочки
        };

        var exit = new ToolStripMenuItem("Выход");
        exit.Click += (_, _) => ExitApp();

        var menu = new ContextMenuStrip
        {
            Renderer = new DarkRenderer(),
            BackColor = Theme.Card,
            ForeColor = Theme.Fg,
            Font = new Font("Segoe UI", 9.5f)
        };
        foreach (var item in new[] { open, onTop, autostart, exit }) item.ForeColor = Theme.Fg;
        menu.Items.AddRange(new ToolStripItem[] { open, new ToolStripSeparator(), onTop, autostart, new ToolStripSeparator(), exit });
        _form.TrayMenu = menu;

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "Hunt KDA",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) _form.ShowAndFocus();
        };

        // Завершение сеанса / перезагрузка / выключение ПК
        _onSessionEnding = (_, _) => _form.SaveNow();
        SystemEvents.SessionEnding += _onSessionEnding;
    }

    void ExitApp()
    {
        _form.SaveNow();
        SystemEvents.SessionEnding -= _onSessionEnding;
        _tray.Visible = false;
        _tray.Dispose();
        ExitThread();
    }
}
