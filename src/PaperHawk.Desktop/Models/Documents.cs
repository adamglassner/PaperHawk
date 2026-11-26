namespace PaperHawk.Models;

/// <summary>What one piece of a page is. The viewer draws each kind (see Views/PageRenderer.cs).</summary>
public enum BlockKind
{
    /// <summary>Big document title. Text.</summary>
    Title,
    /// <summary>Muted line under the title. Text.</summary>
    Subtitle,
    /// <summary>Section heading. Text.</summary>
    Heading,
    /// <summary>Small heading. Text.</summary>
    Subheading,
    /// <summary>Body text. Text.</summary>
    Paragraph,
    /// <summary>Bulleted list. Items.</summary>
    Bullets,
    /// <summary>Numbered list. Items.</summary>
    Numbered,
    /// <summary>Table; the first row is the header. Rows.</summary>
    Table,
    /// <summary>Bar chart. Items are "Label|value". Text is the caption.</summary>
    Chart,
    /// <summary>Picture placeholder. Text is the caption, Height the height.</summary>
    Figure,
    /// <summary>Row of big numbers. Items are "value|label".</summary>
    Stats,
    /// <summary>Boxed note with an accent bar. Text.</summary>
    Callout,
    /// <summary>Label/value pairs in two columns. Items are "label|value".</summary>
    KeyValues,
    /// <summary>Form text field. Text is the label, Value what's filled in (empty: not filled in yet).</summary>
    Field,
    /// <summary>Form check boxes. Items start with "[x] " when ticked, "[ ] " when not.</summary>
    Checkboxes,
    /// <summary>Signature line. Text is the signer's role, Value the signed name (empty: waiting for a signature).</summary>
    Signature,
    /// <summary>Thin horizontal rule.</summary>
    Divider,
    /// <summary>Empty space. Height.</summary>
    Spacer,
    /// <summary>Monospaced lines, like a till receipt. Items.</summary>
    Mono,
}

/// <summary>One block of content on a page.</summary>
public sealed class Block
{
    public required BlockKind Kind { get; init; }
    public string Text { get; init; } = "";
    public string Value { get; init; } = "";
    public string[] Items { get; init; } = [];
    public string[][] Rows { get; init; } = [];
    public double Height { get; init; }
    /// <summary>Text for search, comments and the self-test.</summary>
    public string PlainText => Kind switch
    {
        BlockKind.Table => string.Join(" ", Rows.SelectMany(r => r)),
        _ => string.Join(" ", new[] { Text, Value }.Concat(Items).Where(s => s.Length > 0)),
    };

    public static Block Title(string t) => new() { Kind = BlockKind.Title, Text = t };
    public static Block Subtitle(string t) => new() { Kind = BlockKind.Subtitle, Text = t };
    public static Block H(string t) => new() { Kind = BlockKind.Heading, Text = t };
    public static Block H2(string t) => new() { Kind = BlockKind.Subheading, Text = t };
    public static Block P(string t) => new() { Kind = BlockKind.Paragraph, Text = t };
    public static Block Bullets(params string[] items) => new() { Kind = BlockKind.Bullets, Items = items };
    public static Block Numbered(params string[] items) => new() { Kind = BlockKind.Numbered, Items = items };
    public static Block Table(params string[][] rows) => new() { Kind = BlockKind.Table, Rows = rows };
    public static Block Chart(string caption, params string[] items) => new() { Kind = BlockKind.Chart, Text = caption, Items = items };
    public static Block Figure(string caption, double height = 190) => new() { Kind = BlockKind.Figure, Text = caption, Height = height };
    public static Block Stats(params string[] items) => new() { Kind = BlockKind.Stats, Items = items };
    public static Block Callout(string t) => new() { Kind = BlockKind.Callout, Text = t };
    public static Block KeyValues(params string[] items) => new() { Kind = BlockKind.KeyValues, Items = items };
    public static Block Field(string label, string value = "") => new() { Kind = BlockKind.Field, Text = label, Value = value };
    public static Block Checks(params string[] items) => new() { Kind = BlockKind.Checkboxes, Items = items };
    public static Block Sign(string role, string signedBy = "") => new() { Kind = BlockKind.Signature, Text = role, Value = signedBy };
    public static Block Divider() => new() { Kind = BlockKind.Divider };
    public static Block Space(double h) => new() { Kind = BlockKind.Spacer, Height = h };
    public static Block Mono(params string[] lines) => new() { Kind = BlockKind.Mono, Items = lines };
}

public enum AnnotationKind { Highlight, Note }

/// <summary>A comment on a page: a highlight over a block (with an optional comment) or a sticky note beside it.</summary>
public sealed class Annotation
{
    public required AnnotationKind Kind { get; init; }
    public required string Author { get; init; }
    public string Text { get; init; } = "";
    /// <summary>Which block on the page it belongs to (index into <see cref="PdfPage.Blocks"/>).</summary>
    public required int Block { get; init; }
    public DateTime Date { get; init; }
    public string[] Replies { get; init; } = [];
    public PdfPage Page { get; internal set; } = null!;
}

public sealed class PdfPage
{
    public required int Number { get; init; }
    public required Block[] Blocks { get; init; }
    public Annotation[] Annotations { get; init; } = [];
    public bool Landscape { get; init; }
    public PdfDocument Document { get; internal set; } = null!;

    /// <summary>Page size in device-independent pixels at 100% (US Letter: 8.5 × 11 in at 96 per inch).</summary>
    public double Width => Landscape ? 1056 : 816;
    public double Height => Landscape ? 816 : 1056;
}

/// <summary>A bookmark in the document outline.</summary>
public sealed record OutlineItem(string Title, int Page, int Level = 0);

public sealed class PdfDocument
{
    public required string FileName { get; init; }
    public required string Title { get; init; }
    public required string Kind { get; init; }
    public required string Author { get; init; }
    public string Subject { get; init; } = "";
    public string Folder { get; init; } = "Documents";
    /// <summary>Accent color used in the document's own design and for its dot in lists.</summary>
    public required string Color { get; init; }
    /// <summary>Font family the pages are set in.</summary>
    public string Font { get; init; } = "Segoe UI";
    public required double SizeKb { get; init; }
    public required DateTime Created { get; init; }
    public required DateTime Modified { get; init; }
    public DateTime LastOpened { get; init; }
    /// <summary>Page the reader stopped on last time (1-based).</summary>
    public int LastPage { get; init; } = 1;
    public bool Starred { get; init; }
    public bool Shared { get; init; }
    public bool IsScan { get; init; }
    public bool IsForm => Pages.Any(p => p.Blocks.Any(b => b.Kind is BlockKind.Field or BlockKind.Checkboxes));
    public bool NeedsSignature => Pages.Any(p => p.Blocks.Any(b => b.Kind == BlockKind.Signature && b.Value.Length == 0));
    public string PdfVersion { get; init; } = "1.7";
    public string Producer { get; init; } = "PaperHawk 0.1";
    public string Security { get; init; } = "No security";
    public required PdfPage[] Pages { get; init; }
    public OutlineItem[] Outline { get; init; } = [];

    public IEnumerable<Annotation> Annotations => Pages.SelectMany(p => p.Annotations);
    public int CommentCount => Annotations.Count();
    public string SizeText => SizeKb >= 1024 ? $"{SizeKb / 1024:0.0} MB" : $"{SizeKb:0} KB";
    public string PageCountText => Pages.Length == 1 ? "1 page" : $"{Pages.Length} pages";
    public string PageSizeText => Pages[0].Landscape ? "11 × 8.5 in (Letter, landscape)" : "8.5 × 11 in (Letter)";

    /// <summary>Hooks every page and annotation back to its owner.</summary>
    internal PdfDocument Link()
    {
        foreach (var p in Pages)
        {
            p.Document = this;
            foreach (var a in p.Annotations) a.Page = p;
        }
        return this;
    }
}

/// <summary>Short, friendly dates in 12-hour time.</summary>
public static class When
{
    public static string Ago(DateTime d)
    {
        var span = DateTime.Now - d;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (d.Date == DateTime.Today) return "Today, " + d.ToString("h:mm tt");
        if (d.Date == DateTime.Today.AddDays(-1)) return "Yesterday, " + d.ToString("h:mm tt");
        if (span.TotalDays < 7) return d.ToString("dddd, h:mm tt");
        return d.Year == DateTime.Today.Year ? d.ToString("MMM d") : d.ToString("MMM d, yyyy");
    }

    public static string Full(DateTime d) => d.ToString("MMMM d, yyyy, h:mm tt");
}
