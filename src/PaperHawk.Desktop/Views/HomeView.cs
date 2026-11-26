using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PaperHawk.Models;
using PaperHawk.Services;

namespace PaperHawk.Views;

/// <summary>Home: greeting with quick actions, documents to continue reading, things waiting for you, recent comments
/// and recommended tools. Uses the sample documents.</summary>
public sealed class HomeView : UserControl
{
    public HomeView()
    {
        var root = new StackPanel();
        root.Children.Add(Hero());

        root.Children.Add(SectionHeader("Continue reading", "All files", () => MainWindow.Instance?.GoTo("files")));
        var recent = SampleDocs.Recent.Take(4).ToList();
        var cards = new UniformGrid { Columns = recent.Count, Margin = new Thickness(0, 0, -14, 22) };
        foreach (var d in recent) cards.Children.Add(DocCard(d).Also(c => c.Margin = new Thickness(0, 0, 14, 0)));
        root.Children.Add(cards);

        var mid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        mid.ColumnDefinitions.Add(new ColumnDefinition());
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        mid.ColumnDefinitions.Add(new ColumnDefinition());
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        mid.ColumnDefinitions.Add(new ColumnDefinition());
        mid.Children.Add(WaitingForYou().At(0));
        mid.Children.Add(RecentComments().At(2));
        mid.Children.Add(Recommended().At(4));
        root.Children.Add(mid);
        Content = root;
    }

    private static FrameworkElement Hero()
    {
        var hero = new Border { CornerRadius = new CornerRadius(18), Padding = new Thickness(30, 26, 30, 26), Margin = new Thickness(0, 0, 0, 26) };
        hero.SetResourceReference(Border.BackgroundProperty, "Brush.Hero");
        var dock = new DockPanel();

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(HeroAction(Ic.Files, "Open file", () => Toaster.Soon("Opening your own PDF files")));
        actions.Children.Add(HeroAction(Ic.CreatePdf, "Create PDF", () => MainWindow.Instance?.GoTo("create")));
        actions.Children.Add(HeroAction(Ic.Combine, "Combine files", () => MainWindow.Instance?.GoTo("combine")));
        actions.Children.Add(HeroAction(Ic.Signature, "Fill & Sign", () => MainWindow.Instance?.GoTo("fillsign")));
        DockPanel.SetDock(actions, Dock.Right);
        dock.Children.Add(actions);

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        int hour = DateTime.Now.Hour;
        string greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        var title = new TextBlock { Text = greeting, FontSize = 30, FontWeight = FontWeights.Bold };
        title.SetResourceReference(TextBlock.FontFamilyProperty, "DisplayFont");
        title.SetResourceReference(TextBlock.ForegroundProperty, "Brush.HeroText");
        text.Children.Add(title);
        int comments = SampleDocs.All.Sum(d => d.CommentCount);
        int toSign = SampleDocs.All.Count(d => d.NeedsSignature);
        text.Children.Add(Ui.Text($"{DateTime.Now:dddd, MMMM d}", size: 14, brushKey: "Brush.HeroMuted").Also(t => t.Margin = new Thickness(0, 6, 0, 0)));
        text.Children.Add(Ui.Text($"{SampleDocs.All.Length} sample documents · {comments} comments · {toSign} waiting for a signature", size: 13, brushKey: "Brush.HeroMuted").Also(t => t.Margin = new Thickness(0, 3, 0, 0)));
        dock.Children.Add(text);
        hero.Child = dock;
        return hero;
    }

    private static Button HeroAction(string glyph, string label, Action click)
    {
        var b = new Button { Style = Ui.S("BaseButton"), Width = 108, Padding = new Thickness(8, 12, 8, 10), Margin = new Thickness(8, 0, 0, 0) };
        b.SetResourceReference(BackgroundProperty, "Brush.SidebarCard");
        var sp = new StackPanel();
        sp.Children.Add(Ui.Icon(glyph, 22, "Brush.Highlight").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center));
        sp.Children.Add(Ui.Text(label, size: 12.5, brushKey: "Brush.HeroText").Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 7, 0, 0); }));
        b.Content = sp;
        b.Click += (_, _) => click();
        System.Windows.Automation.AutomationProperties.SetName(b, label);
        return b;
    }

    private static DockPanel SectionHeader(string title, string? link, Action? onLink)
    {
        var dock = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        if (link != null)
        {
            var b = new Button { Style = Ui.S("LinkButton"), Content = link };
            b.Click += (_, _) => onLink?.Invoke();
            DockPanel.SetDock(b, Dock.Right);
            dock.Children.Add(b);
        }
        dock.Children.Add(new TextBlock { Text = title, Style = Ui.S("CardTitle"), VerticalAlignment = VerticalAlignment.Center });
        return dock;
    }

    /// <summary>A card with the document's first page, where you left off and a progress bar.</summary>
    private static FrameworkElement DocCard(PdfDocument d)
    {
        var sp = new StackPanel();
        var preview = new Border { Height = 150, CornerRadius = new CornerRadius(10), ClipToBounds = true, Margin = new Thickness(0, 0, 0, 14) };
        preview.SetResourceReference(Border.BackgroundProperty, "Brush.Canvas");
        var page = PageRenderer.Render(d.Pages[Math.Clamp(d.LastPage, 1, d.Pages.Length) - 1], interactive: false).Paper;
        var thumb = new Viewbox { Width = 150, Stretch = System.Windows.Media.Stretch.Uniform, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 14, 0, 0), Child = page };
        thumb.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 1.5, Opacity = 0.2, Direction = 270 };
        preview.Child = thumb;
        sp.Children.Add(preview);

        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        top.Children.Add(Ui.Dot(d.Color, 8).Also(x => { x.Margin = new Thickness(0, 0, 8, 0); DockPanel.SetDock(x, Dock.Left); }));
        if (d.Starred) top.Children.Add(Ui.Icon(Ic.Star, 13, "Brush.Amber", filled: true).Also(i => DockPanel.SetDock(i, Dock.Right)));
        top.Children.Add(Ui.Text(d.Kind, muted: true, size: 11.5));
        sp.Children.Add(top);
        sp.Children.Add(Ui.Text(d.Title, size: 14.5, weight: FontWeights.SemiBold));
        sp.Children.Add(Ui.Text(d.FileName, muted: true, size: 11.5).Also(t => t.Margin = new Thickness(0, 2, 0, 10)));
        sp.Children.Add(Ui.Progress(100.0 * d.LastPage / d.Pages.Length).Also(p => p.Margin = new Thickness(0, 0, 0, 8)));
        var foot = new DockPanel();
        foot.Children.Add(Ui.Text(When.Ago(d.LastOpened), muted: true, size: 11.5).Also(t => DockPanel.SetDock(t, Dock.Right)));
        foot.Children.Add(Ui.Text($"Page {d.LastPage} of {d.Pages.Length}", size: 11.5, brushKey: "Brush.Accent"));
        sp.Children.Add(foot);

        var button = new Button { Style = Ui.S("CardButton"), Content = new Border { Style = Ui.S("Card"), Padding = new Thickness(14, 14, 14, 14), Child = sp }, ToolTip = $"Open on page {d.LastPage}" };
        button.Click += (_, _) => MainWindow.Instance?.OpenDocument(d, d.LastPage);
        System.Windows.Automation.AutomationProperties.SetName(button, d.Title);
        return button;
    }

    private static Button Row(string glyph, string glyphBrush, string title, string subtitle, string right, Action click, string? rightBrush = null, bool filled = false)
    {
        var b = new Button { Style = Ui.S("BaseButton"), HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(-10, 0, -10, 0) };
        var row = new DockPanel();
        row.Children.Add(Ui.Icon(glyph, 16, glyphBrush, filled).Also(i => { i.Margin = new Thickness(0, 0, 11, 0); DockPanel.SetDock(i, Dock.Left); }));
        row.Children.Add(Ui.Text(right, muted: rightBrush == null, size: 12, brushKey: rightBrush).Also(x => { x.Margin = new Thickness(10, 0, 0, 0); DockPanel.SetDock(x, Dock.Right); }));
        var texts = new StackPanel();
        texts.Children.Add(Ui.Text(title, size: 13.5));
        texts.Children.Add(Ui.Text(subtitle, muted: true, size: 11.5));
        row.Children.Add(texts);
        b.Content = row;
        b.Click += (_, _) => click();
        System.Windows.Automation.AutomationProperties.SetName(b, title);
        return b;
    }

    private static FrameworkElement WaitingForYou()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Signature, "Waiting for you"));
        var list = SampleDocs.All.Where(d => d.NeedsSignature || d.IsForm || d.IsScan).ToList();
        foreach (var d in list)
        {
            var (what, brush) = d.NeedsSignature ? ("Sign", "Brush.Warn") : d.IsForm ? ("Fill in", "Brush.Accent") : ("Recognize text", "Brush.TextMuted");
            int page = d.NeedsSignature
                ? d.Pages.First(p => p.Blocks.Any(b => b.Kind == BlockKind.Signature && b.Value.Length == 0)).Number
                : 1;
            sp.Children.Add(Row(d.IsScan ? Ic.Scan : d.NeedsSignature ? Ic.Signature : Ic.Form, brush, d.Title, $"{d.Kind} · {d.PageCountText}", what, () => MainWindow.Instance?.OpenDocument(d, page), brush));
        }
        if (list.Count == 0) sp.Children.Add(Ui.Text("Nothing waiting. Nice.", muted: true, size: 13));
        return Ui.Card(sp);
    }

    private static FrameworkElement RecentComments()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Comments, "Recent comments"));
        foreach (var a in SampleDocs.All.SelectMany(d => d.Annotations).OrderByDescending(x => x.Date).Take(4))
        {
            var b = new Button { Style = Ui.S("BaseButton"), HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(-10, 0, -10, 0) };
            var row = new DockPanel();
            row.Children.Add(ViewerView.Avatar(a.Author, 28).Also(x => { x.Margin = new Thickness(0, 0, 11, 0); x.VerticalAlignment = VerticalAlignment.Top; DockPanel.SetDock(x, Dock.Left); }));
            var texts = new StackPanel();
            var head = new DockPanel();
            head.Children.Add(Ui.Text(When.Ago(a.Date), muted: true, size: 11.5).Also(t => { t.Margin = new Thickness(8, 0, 0, 0); DockPanel.SetDock(t, Dock.Right); }));
            head.Children.Add(Ui.Text(a.Author, size: 13, weight: FontWeights.SemiBold));
            texts.Children.Add(head);
            texts.Children.Add(Ui.Text(a.Text.Length > 0 ? a.Text : "Highlighted text", size: 12.5));
            texts.Children.Add(Ui.Text($"{a.Page.Document.Title} · page {a.Page.Number}", muted: true, size: 11.5));
            row.Children.Add(texts);
            b.Content = row;
            var target = a;
            b.Click += (_, _) => MainWindow.Instance?.OpenComment(target);
            System.Windows.Automation.AutomationProperties.SetName(b, $"Comment from {a.Author}");
            sp.Children.Add(b);
        }
        return Ui.Card(sp);
    }

    private static FrameworkElement Recommended()
    {
        var sp = new StackPanel();
        sp.Children.Add(Ui.CardHeader(Ic.Sparkle, "Tools", null, new Button { Style = Ui.S("LinkButton"), Content = "All tools" }
            .Also(b => b.Click += (_, _) => MainWindow.Instance?.GoTo("tools"))));
        var picks = new[] { "Edit PDF", "Fill & Sign", "Combine files", "Export PDF" };
        foreach (var name in picks)
        {
            var tool = ToolsView.Groups.SelectMany(g => g.Tools).First(t => t.Name == name);
            sp.Children.Add(Row(tool.Glyph, "Brush.Accent", tool.Name, tool.Description, "Soon", () => ToolsView.Open(tool)));
        }
        return Ui.Card(sp);
    }
}
