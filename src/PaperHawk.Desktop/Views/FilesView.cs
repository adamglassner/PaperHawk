using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PaperHawk.Models;
using PaperHawk.Services;

namespace PaperHawk.Views;

public enum FileFilter { All, Recent, Starred, Shared, NeedsSignature, Forms, Scans }

/// <summary>Every sample PDF as a table with working filters. Clicking a file opens it in the viewer.
/// Opening your own files, folders and cloud storage are coming soon.</summary>
public sealed class FilesView : UserControl
{
    public FileFilter Filter { get; private set; } = FileFilter.All;
    public int RowCount { get; private set; }

    private readonly UniformGrid _kpis = new() { Rows = 1, Margin = new Thickness(0, 0, -14, 18) };
    private readonly StackPanel _rows = new();
    private readonly Dictionary<FileFilter, RadioButton> _chips = [];
    private bool _refreshing;

    private static readonly (string Title, double Width)[] Columns =
    [
        ("Name", -2.6), ("Type", 104), ("Pages", 70), ("Size", 84), ("Opened", 150), ("Comments", 96), ("", 48),
    ];

    public static string Label(FileFilter f) => f switch
    {
        FileFilter.All => "All files",
        FileFilter.Recent => "Opened this week",
        FileFilter.Starred => "Starred",
        FileFilter.Shared => "Shared",
        FileFilter.NeedsSignature => "Waiting for signature",
        FileFilter.Forms => "Forms",
        _ => "Scans",
    };

    public static bool Matches(PdfDocument d, FileFilter f) => f switch
    {
        FileFilter.All => true,
        FileFilter.Recent => d.LastOpened >= DateTime.Now.AddDays(-7),
        FileFilter.Starred => d.Starred,
        FileFilter.Shared => d.Shared,
        FileFilter.NeedsSignature => d.NeedsSignature,
        FileFilter.Forms => d.IsForm,
        _ => d.IsScan,
    };

    public FilesView()
    {
        var root = new StackPanel();
        root.Children.Add(Header());
        root.Children.Add(_kpis);
        root.Children.Add(Filters());
        var table = new StackPanel();
        table.Children.Add(TableHeader());
        table.Children.Add(_rows);
        root.Children.Add(new Border { Style = Ui.S("Card"), Padding = new Thickness(0, 0, 0, 6), ClipToBounds = true, Child = table });
        root.Children.Add(DropZone());
        Content = root;
        BuildKpis();
        Refresh();
    }

    public void SetFilter(FileFilter filter)
    {
        Filter = filter;
        if (_chips.TryGetValue(filter, out var rb)) rb.IsChecked = true;
        Refresh();
    }

    private static FrameworkElement Header()
    {
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 22) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        buttons.Children.Add(Ui.SoonButton("Folders", Ic.Folder, "New folder").Also(b => b.Margin = new Thickness(0, 0, 8, 0)));
        buttons.Children.Add(Ui.SoonButton("Opening your own PDF files", Ic.Files, "Open file", "PrimaryButton"));
        DockPanel.SetDock(buttons, Dock.Right);
        header.Children.Add(buttons);

        var tile = new Border { Width = 56, Height = 56, CornerRadius = new CornerRadius(14), Margin = new Thickness(0, 0, 16, 0) };
        tile.SetResourceReference(Border.BackgroundProperty, "Brush.AccentSoft");
        tile.Child = Ui.Icon(Ic.Files, 24, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center);
        header.Children.Add(tile);
        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock { Text = "Files", Style = Ui.S("PageTitle") });
        titleRow.Children.Add(Ui.Pill("PREVIEW").Also(p => p.Margin = new Thickness(12, 0, 0, 0)));
        titles.Children.Add(titleRow);
        titles.Children.Add(new TextBlock { Text = "Every PDF you've opened. Click one to read it.", Style = Ui.S("Muted"), FontSize = 14, Margin = new Thickness(0, 4, 0, 0) });
        header.Children.Add(titles);
        return header;
    }

    private void BuildKpis()
    {
        var docs = SampleDocs.All;
        Kpi("Documents", docs.Length.ToString(), null);
        Kpi("Pages", docs.Sum(d => d.Pages.Length).ToString(), null);
        Kpi("Comments", docs.Sum(d => d.CommentCount).ToString(), "Brush.Accent");
        Kpi("Waiting for signature", docs.Count(d => d.NeedsSignature).ToString(), "Brush.Warn");
    }

    private void Kpi(string label, string value, string? brush)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = label, Style = Ui.S("Muted"), FontSize = 13 });
        var number = new TextBlock { Text = value, Style = Ui.S("BigNumber") };
        if (brush != null) number.SetResourceReference(TextBlock.ForegroundProperty, brush);
        sp.Children.Add(number);
        _kpis.Children.Add(Ui.Card(sp).Also(c => c.Margin = new Thickness(0, 0, 14, 0)));
    }

    private FrameworkElement Filters()
    {
        var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        foreach (var f in Enum.GetValues<FileFilter>())
        {
            int count = SampleDocs.All.Count(d => Matches(d, f));
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(new TextBlock { Text = Label(f), VerticalAlignment = VerticalAlignment.Center });
            content.Children.Add(new TextBlock { Text = count.ToString(), Opacity = 0.65, Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            var rb = new RadioButton { Style = Ui.S("ChipRadio"), Content = content, GroupName = "FilesFilter" + GetHashCode(), IsChecked = f == Filter };
            var filter = f;
            rb.Checked += (_, _) => { if (!_refreshing) { Filter = filter; Refresh(); } };
            System.Windows.Automation.AutomationProperties.SetName(rb, Label(f));
            _chips[f] = rb;
            wrap.Children.Add(rb);
        }
        return wrap;
    }

    private static Grid ColumnGrid()
    {
        var g = new Grid();
        foreach (var (_, w) in Columns)
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = w < 0 ? new GridLength(-w, GridUnitType.Star) : new GridLength(w) });
        return g;
    }

    private static FrameworkElement TableHeader()
    {
        var border = new Border { CornerRadius = new CornerRadius(14, 14, 0, 0) };
        border.SetResourceReference(Border.BackgroundProperty, "Brush.SurfaceAlt");
        var g = ColumnGrid();
        for (int i = 0; i < Columns.Length; i++)
        {
            var t = Ui.Text(Columns[i].Title.ToUpperInvariant(), muted: true, size: 11, weight: FontWeights.SemiBold);
            t.Margin = new Thickness(i == 0 ? 20 : 8, 12, 8, 12);
            g.Children.Add(t.At(i));
        }
        border.Child = g;
        return border;
    }

    private void Refresh()
    {
        _refreshing = true;
        try
        {
            _rows.Children.Clear();
            RowCount = 0;
            foreach (var d in SampleDocs.Recent.Where(d => Matches(d, Filter)))
            {
                _rows.Children.Add(Row(d));
                RowCount++;
            }
            if (RowCount == 0)
            {
                var empty = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 40) };
                empty.Children.Add(Ui.Icon(Ic.Files, 26, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center));
                empty.Children.Add(Ui.Text("No files here", size: 15, weight: FontWeights.SemiBold).Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 8, 0, 0); }));
                _rows.Children.Add(empty);
            }
        }
        finally { _refreshing = false; }
    }

    private static FrameworkElement Row(PdfDocument d)
    {
        var g = ColumnGrid();
        var name = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        name.Children.Add(Ui.Icon(d.IsScan ? Ic.Scan : Ic.Pdf, 20, null, filled: true).Also(i => { i.Foreground = Ui.Hex(d.Color); i.Margin = new Thickness(0, 0, 12, 0); }));
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(Ui.Text(d.Title, size: 13.5, weight: FontWeights.SemiBold));
        var sub = new StackPanel { Orientation = Orientation.Horizontal };
        sub.Children.Add(Ui.Text(d.FileName, muted: true, size: 11.5));
        if (d.NeedsSignature) sub.Children.Add(Tag("Sign", "Brush.Warn"));
        else if (d.IsForm) sub.Children.Add(Tag("Form", "Brush.Accent"));
        if (d.Shared) sub.Children.Add(Ui.Icon(Ic.People, 12, "Brush.TextMuted").Also(i => { i.Margin = new Thickness(8, 0, 0, 0); i.ToolTip = "Shared"; }));
        texts.Children.Add(sub);
        name.Children.Add(texts);
        g.Children.Add(name.At(0));

        g.Children.Add(Cell(d.Kind).At(1));
        g.Children.Add(Cell(d.Pages.Length.ToString()).At(2));
        g.Children.Add(Cell(d.SizeText).At(3));
        g.Children.Add(Cell(When.Ago(d.LastOpened)).At(4));
        g.Children.Add(Cell(d.CommentCount == 0 ? "—" : d.CommentCount.ToString(), muted: d.CommentCount == 0).At(5));
        g.Children.Add(Ui.Icon(Ic.Star, 15, d.Starred ? "Brush.Amber" : "Brush.Skeleton", filled: d.Starred).Also(i => { i.HorizontalAlignment = HorizontalAlignment.Center; i.ToolTip = d.Starred ? "Starred" : "Not starred"; }).At(6));

        var b = new Button { Style = Ui.S("RowButton"), Content = g, MinHeight = 56, Padding = new Thickness(0, 6, 0, 6), ToolTip = $"Open {d.FileName}" };
        b.Click += (_, _) => MainWindow.Instance?.OpenDocument(d);
        System.Windows.Automation.AutomationProperties.SetName(b, d.Title);
        return b;
    }

    private static Border Tag(string text, string brush)
    {
        var t = Ui.Text(text, size: 10.5, weight: FontWeights.SemiBold, brushKey: brush);
        var pill = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(7, 1, 7, 1), BorderThickness = new Thickness(1), Margin = new Thickness(8, 0, 0, 0), Child = t, VerticalAlignment = VerticalAlignment.Center };
        pill.SetResourceReference(Border.BorderBrushProperty, brush);
        return pill;
    }

    private static TextBlock Cell(string text, bool muted = false) => Ui.Text(text, muted, size: 12.5).Also(t => t.Margin = new Thickness(8, 0, 8, 0));

    private static FrameworkElement DropZone()
    {
        var zone = new Border { CornerRadius = new CornerRadius(14), Padding = new Thickness(24, 22, 24, 22), Background = System.Windows.Media.Brushes.Transparent };
        var dashed = new System.Windows.Shapes.Rectangle { RadiusX = 14, RadiusY = 14, StrokeThickness = 1.5, StrokeDashArray = new System.Windows.Media.DoubleCollection { 5, 4 }, IsHitTestVisible = false };
        dashed.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "Brush.Highlight");
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(Ui.Icon(Ic.Upload, 24, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center));
        sp.Children.Add(Ui.Text("Drop PDFs here to open them", size: 14, weight: FontWeights.SemiBold).Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 8, 0, 2); }));
        sp.Children.Add(Ui.Text("Or Word, Excel, PowerPoint and picture files to turn them into PDFs. Coming soon.", muted: true, size: 12.5).Also(t => t.HorizontalAlignment = HorizontalAlignment.Center));
        zone.Child = sp;
        var grid = new Grid { Margin = new Thickness(0, 18, 0, 0), Cursor = System.Windows.Input.Cursors.Hand };
        grid.Children.Add(dashed);
        grid.Children.Add(zone);
        grid.MouseLeftButtonUp += (_, _) => Toaster.Soon("Opening your own PDF files");
        return grid;
    }
}
