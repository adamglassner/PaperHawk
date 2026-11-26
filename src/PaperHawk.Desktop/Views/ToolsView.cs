using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PaperHawk.Models;
using PaperHawk.Services;

namespace PaperHawk.Views;

/// <summary>One tool on the All tools page. <see cref="Module"/> opens that sidebar section; "comments" opens the
/// viewer with the comments panel; anything else just says it's coming soon.</summary>
public sealed record PdfTool(string Name, string Glyph, string Description, string? Module = null, bool Works = false);

/// <summary>All tools, grouped like Acrobat's tool center. Each card opens its section or says it's coming soon.</summary>
public sealed class ToolsView : UserControl
{
    public static readonly (string Group, PdfTool[] Tools)[] Groups =
    [
        ("Create and edit", new PdfTool[]
        {
            new("Create PDF", Ic.CreatePdf, "Turn files, scans and web pages into PDFs.", "create"),
            new("Edit PDF", Ic.Edit, "Change text and pictures right on the page.", "edit"),
            new("Combine files", Ic.Combine, "Merge several files into one PDF.", "combine"),
            new("Organize pages", Ic.Pages, "Reorder, rotate, delete and split pages.", "organize"),
            new("Export PDF", Ic.Export, "Save as Word, Excel, PowerPoint or pictures.", "export"),
            new("Compress PDF", Ic.Compress, "Make a PDF smaller to email or upload."),
        }),
        ("Review and share", new PdfTool[]
        {
            new("Comment", Ic.Comments, "Read the comments on a PDF. Adding your own is coming soon.", "comments", Works: true),
            new("Compare files", Ic.Compare, "See what changed between two versions."),
            new("Measure", Ic.Ruler, "Measure distances and areas on drawings."),
            new("Read aloud", Ic.ReadAloud, "Listen to a document with a natural voice."),
        }),
        ("Sign and protect", new PdfTool[]
        {
            new("Fill & Sign", Ic.Signature, "Fill in forms and add your signature.", "fillsign"),
            new("Request e-signatures", Ic.DocSign, "Send a document to others to sign.", "fillsign"),
            new("Prepare a form", Ic.Form, "Turn a PDF into a fillable form.", "fillsign"),
            new("Protect a PDF", Ic.Protect, "Add a password and limit what others can do.", "protect"),
            new("Redact", Ic.Redact, "Remove private details for good.", "protect"),
            new("Certificates", Ic.Certificate, "Sign and check certificate signatures."),
        }),
        ("Scan and accessibility", new PdfTool[]
        {
            new("Scan & OCR", Ic.Scan, "Scan paper and make the text searchable.", "scan"),
            new("Accessibility check", Ic.Accessibility, "Find and fix problems for screen readers."),
            new("Translate", Ic.Translate, "Translate a whole PDF and keep its layout."),
        }),
    ];

    public int ToolCount { get; }

    public ToolsView()
    {
        var root = new StackPanel();
        root.Children.Add(Header());
        foreach (var (group, tools) in Groups)
        {
            root.Children.Add(new TextBlock { Text = group.ToUpperInvariant(), Style = Ui.S("SectionLabel"), Margin = new Thickness(2, 6, 0, 10) });
            var grid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, -14, 14) };
            foreach (var t in tools) { grid.Children.Add(Card(t).Also(c => c.Margin = new Thickness(0, 0, 14, 14))); ToolCount++; }
            root.Children.Add(grid);
        }
        Content = root;
    }

    /// <summary>What clicking a tool does (also used by the self-test).</summary>
    public static void Open(PdfTool tool)
    {
        var window = MainWindow.Instance;
        if (tool.Module == "comments") window?.OpenDocument(window.CurrentDocument, null, RightPanel.Comments);
        else if (tool.Module != null) window?.GoTo(tool.Module);
        else Toaster.Soon(tool.Name);
    }

    private static FrameworkElement Header()
    {
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 22) };
        var search = Ui.SearchBox("Search tools…", 260);
        search.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(search, Dock.Right);
        header.Children.Add(search);
        var tile = new Border { Width = 56, Height = 56, CornerRadius = new CornerRadius(14), Margin = new Thickness(0, 0, 16, 0) };
        tile.SetResourceReference(Border.BackgroundProperty, "Brush.AccentSoft");
        tile.Child = Ui.Icon(Ic.Tools, 24, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center);
        header.Children.Add(tile);
        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titles.Children.Add(new TextBlock { Text = "All tools", Style = Ui.S("PageTitle") });
        titles.Children.Add(new TextBlock { Text = "Everything PaperHawk will do with a PDF. Pick a tool to see what's planned.", Style = Ui.S("Muted"), FontSize = 14, Margin = new Thickness(0, 4, 0, 0) });
        header.Children.Add(titles);
        return header;
    }

    private static FrameworkElement Card(PdfTool t)
    {
        var dock = new DockPanel();
        var tile = new Border { Width = 44, Height = 44, CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Top };
        tile.SetResourceReference(Border.BackgroundProperty, "Brush.AccentSoft");
        tile.Child = Ui.Icon(t.Glyph, 20, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center);
        DockPanel.SetDock(tile, Dock.Left);
        dock.Children.Add(tile);
        var texts = new StackPanel();
        var top = new DockPanel();
        top.Children.Add(Ui.Pill(t.Works ? "PREVIEW" : "COMING SOON").Also(p => { p.Margin = new Thickness(8, 0, 0, 0); DockPanel.SetDock(p, Dock.Right); }));
        top.Children.Add(Ui.Text(t.Name, size: 14.5, weight: FontWeights.SemiBold));
        texts.Children.Add(top);
        texts.Children.Add(Ui.Wrap(t.Description, muted: true, size: 12.5, lineHeight: 18).Also(w => w.Margin = new Thickness(0, 5, 0, 0)));
        dock.Children.Add(texts);

        var b = new Button
        {
            Style = Ui.S("CardButton"),
            Content = new Border { Style = Ui.S("Card"), Padding = new Thickness(16, 15, 16, 15), Child = dock, MinHeight = 98 },
            ToolTip = t.Module == null ? $"{t.Name} is coming soon" : t.Module == "comments" ? "Open the comments on the current document" : $"See what's planned for {t.Name}",
        };
        b.Click += (_, _) => Open(t);
        System.Windows.Automation.AutomationProperties.SetName(b, t.Name);
        return b;
    }
}
