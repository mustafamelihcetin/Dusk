using System.Windows;
using Dusk.Core;
using Forms = System.Windows.Forms;

namespace Dusk;

/// <summary>Notification-area icon: shows time left, offers the common actions.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly TimerController _timer;
    private readonly Forms.ToolStripMenuItem _status, _extend, _cancel;

    public event EventHandler? ShowRequested;
    public event EventHandler? QuitRequested;

    public TrayIcon(TimerController timer)
    {
        _timer = timer;
        var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/dusk.ico")).Stream;
        var menu = new Forms.ContextMenuStrip { ShowImageMargin = false };
        _status = new Forms.ToolStripMenuItem { Enabled = false };
        _extend = new Forms.ToolStripMenuItem(Loc.TrayExtend, null, (_, _) => _timer.Extend(10));
        _cancel = new Forms.ToolStripMenuItem(Loc.TrayCancel, null, (_, _) => _timer.Cancel("tray menu"));
        menu.Items.AddRange(
        [
            _status,
            new Forms.ToolStripSeparator(),
            new Forms.ToolStripMenuItem(Loc.TrayShow, null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty)),
            _extend,
            _cancel,
            new Forms.ToolStripSeparator(),
            new Forms.ToolStripMenuItem(Loc.TrayQuit, null, (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty)),
        ]);

        _icon = new Forms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) ShowRequested?.Invoke(this, EventArgs.Empty);
        };
        _timer.Changed += (_, _) => Refresh();
        Refresh();
    }

    private string _lastText = "";

    private void Refresh()
    {
        var c = _timer.Current;
        string text;
        if (c is null)
        {
            text = Loc.TrayIdle;
        }
        else
        {
            var at = TimeText.At(c.TargetUtc.ToLocalTime(), DateTime.Now);
            // Tooltips are limited to 127 characters; this stays well under.
            text = $"Dusk · {Loc.Left(TimeText.Clock(c.Remaining(DateTime.UtcNow)))}\n{Loc.HappensAt(c.Action, at)}";
        }
        if (text == _lastText) return;
        _lastText = text;
        _icon.Text = text;
        _status.Text = text.Replace("\n", " · ");
        _extend.Enabled = _cancel.Enabled = c is not null;
    }

    public void Balloon(string title, string text) =>
        _icon.ShowBalloonTip(5000, title, text, Forms.ToolTipIcon.None);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
