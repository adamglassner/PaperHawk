using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using PaperHawk.Models;
using Block = PaperHawk.Models.Block;

namespace PaperHawk.Views;

/// <summary>A drawn page: the sheet of paper and the element each comment is attached to.</summary>
public sealed class RenderedPage
{
    public required PdfPage Page { get; init; }
    public required Border Paper { get; init; }
    public Dictionary<Annotation, Border> Marks { get; } = [];
}

/// <summary>
/// Draws a sample page as a sheet of paper at 100% (816 × 1056 for US Letter). Pages are always white with dark ink,
/// in light and dark mode alike, the way PDF readers show them. The viewer scales the result for zoom and rotation.
/// </summary>
public static class PageRenderer
{
    private static SolidColorBrush B(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }

    private static readonly Brush Ink = B("#1E1A1C");
    private static readonly Brush Muted = B("#6B6467");
    private static readonly Brush Rule = B("#DAD3D6");
    private static readonly Brush Tint = B("#F6F3F4");
    private static readonly Brush FieldEmpty = B("#E6EEFD");
    private static readonly Brush FieldEmptyBorder = B("#9DB6E6");
    private static readonly Brush FieldFilled = B("#F3F6FC");
    private static readonly Brush Marker = B("#FFEF8A");
    private static readonly Brush StickyBg = B("#FFD24D");
    private static readonly Brush StickyInk = B("#5A4300");
    private static readonly Brush SignInk = B("#1E3A8A");
    private static readonly Brush ScanPaper = B("#F3F0E8");
    private static readonly Brush ScanInk = B("#3B3936");

    public const double MarginX = 92;

    /// <param name="interactive">False for thumbnails: no tooltips, no clicks.</param>
    /// <param name="onAnnotation">Called when a highlight or sticky note on the page is clicked.</param>
    public static RenderedPage Render(PdfPage page, bool interactive, Action<Annotation>? onAnnotation = null)
    {
        var doc = page.Document;
        var accent = B(doc.Color);
        var paper = new Border
        {
            Width = page.Width,
            Height = page.Height,
            Background = doc.IsScan ? ScanPaper : Brushes.White,
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            IsHitTestVisible = interactive,
        };
        var result = new RenderedPage { Page = page, Paper = paper };

        var layer = new Grid();
        var content = new StackPanel { Margin = new Thickness(MarginX, 84, MarginX, 70) };
        TextElement.SetFontFamily(content, new FontFamily(doc.Font));
        TextElement.SetForeground(content, doc.IsScan ? ScanInk : Ink);

        for (int i = 0; i < page.Blocks.Length; i++)
        {
            var block = page.Blocks[i];
            var element = BuildBlock(block, accent);
            var notes = page.Annotations.Where(a => a.Block == i).ToList();
            var wrap = new Border
            {
                Child = element,
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(-5, -2, -5, -2),
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1.5),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
            };
            UIElement row = wrap;
            foreach (var a in notes)
            {
                result.Marks[a] = wrap;
                if (a.Kind == AnnotationKind.Highlight)
                {
                    wrap.Background = Marker;
                    if (interactive)
                    {
                        wrap.Cursor = System.Windows.Input.Cursors.Hand;
                        wrap.ToolTip = Tip(a);
                        var target = a;
                        wrap.MouseLeftButtonUp += (_, e) => { e.Handled = true; onAnnotation?.Invoke(target); };
                    }
                }
            }
            var stickies = notes.Where(a => a.Kind == AnnotationKind.Note || a.Text.Length > 0).ToList();
            if (stickies.Count > 0)
            {
                // Sticky-note icons sit in the right margin next to the block they belong to.
                var grid = new Grid();
                grid.Children.Add(wrap);
                var icons = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, -MarginX + 26, 0) };
                foreach (var a in stickies) icons.Children.Add(Sticky(a, interactive, onAnnotation));
                grid.Children.Add(icons);
                row = grid;
            }
            content.Children.Add(row);
        }

        if (doc.IsScan)
        {
            // Scans are pictures of paper: slightly crooked, with a soft shadow at the edges.
            content.RenderTransformOrigin = new Point(0.5, 0.3);
            content.RenderTransform = new RotateTransform(-0.7);
            layer.Children.Add(content);
            var vignette = new Border
            {
                IsHitTestVisible = false,
                Background = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(40, 60, 50, 30)) { RadiusX = 0.75, RadiusY = 0.75 },
            };
            layer.Children.Add(vignette);
        }
        else
        {
            layer.Children.Add(content);
            var footer = new DockPanel { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(MarginX, 0, MarginX, 34) };
            footer.Children.Add(new TextBlock { Text = $"{page.Number} / {doc.Pages.Length}", FontSize = 11, Foreground = Muted, FontFamily = new FontFamily(doc.Font) }.Also(t => DockPanel.SetDock(t, Dock.Right)));
            footer.Children.Add(new TextBlock { Text = doc.Title, FontSize = 11, Foreground = Muted, FontFamily = new FontFamily(doc.Font), TextTrimming = TextTrimming.CharacterEllipsis });
            layer.Children.Add(footer);
        }
        paper.Child = layer;
        return result;
    }

    private static ToolTip Tip(Annotation a) => new()
    {
        Content = new StackPanel
        {
            MaxWidth = 280,
            Children =
            {
                new TextBlock { Text = $"{a.Author} · {When.Ago(a.Date)}", FontWeight = FontWeights.SemiBold },
                new TextBlock { Text = a.Text.Length > 0 ? a.Text : "Highlighted text", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) },
            },
        },
    };

    private static FrameworkElement Sticky(Annotation a, bool interactive, Action<Annotation>? onAnnotation)
    {
        var icon = new TextBlock { Text = a.Kind == AnnotationKind.Note ? Ic.Note : Ic.Comment, FontSize = 15, Foreground = StickyInk, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        icon.SetResourceReference(TextBlock.FontFamilyProperty, "IconFontFilled");
        var b = new Border
        {
            Width = 28, Height = 28, CornerRadius = new CornerRadius(5), Background = StickyBg, Margin = new Thickness(0, 2, 0, 6), Child = icon,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 6, ShadowDepth = 1.5, Opacity = 0.25 },
        };
        if (interactive)
        {
            b.Cursor = System.Windows.Input.Cursors.Hand;
            b.ToolTip = Tip(a);
            b.MouseLeftButtonUp += (_, e) => { e.Handled = true; onAnnotation?.Invoke(a); };
            System.Windows.Automation.AutomationProperties.SetName(b, $"Comment from {a.Author}");
        }
        return b;
    }

    private static TextBlock T(string text, double size, FontWeight? weight = null, Brush? brush = null, Thickness? margin = null)
    {
        var t = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            Margin = margin ?? new Thickness(0),
        };
        if (brush != null) t.Foreground = brush;
        return t;
    }

    private static FrameworkElement BuildBlock(Block b, Brush accent)
    {
        switch (b.Kind)
        {
            case BlockKind.Title:
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                sp.Children.Add(T(b.Text, 34, FontWeights.Bold));
                sp.Children.Add(new Border { Width = 64, Height = 5, CornerRadius = new CornerRadius(2.5), Background = accent, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0) });
                return sp;
            }
            case BlockKind.Subtitle:
                return T(b.Text, 15, brush: Muted, margin: new Thickness(0, 4, 0, 14));
            case BlockKind.Heading:
                return T(b.Text, 23, FontWeights.Bold, accent, new Thickness(0, 10, 0, 10));
            case BlockKind.Subheading:
                return T(b.Text, 16.5, FontWeights.SemiBold, margin: new Thickness(0, 10, 0, 6));
            case BlockKind.Paragraph:
                return T(b.Text, 14.5, margin: new Thickness(0, 0, 0, 12)).Also(t => { t.LineHeight = 23; t.TextAlignment = TextAlignment.Justify; });
            case BlockKind.Bullets:
            case BlockKind.Numbered:
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                for (int i = 0; i < b.Items.Length; i++)
                {
                    var row = new DockPanel { Margin = new Thickness(6, 0, 0, 6) };
                    var marker = b.Kind == BlockKind.Bullets
                        ? T("•", 15, FontWeights.Bold, accent)
                        : T($"{i + 1}.", 14.5, FontWeights.SemiBold, accent);
                    marker.Width = 24;
                    DockPanel.SetDock(marker, Dock.Left);
                    row.Children.Add(marker);
                    row.Children.Add(T(b.Items[i], 14.5).Also(t => t.LineHeight = 22));
                    sp.Children.Add(row);
                }
                return sp;
            }
            case BlockKind.Table:
            {
                var grid = new Grid { Margin = new Thickness(0, 4, 0, 16) };
                int cols = b.Rows.Max(r => r.Length);
                for (int c = 0; c < cols; c++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(c == 0 ? 2.2 : 1, GridUnitType.Star) });
                for (int r = 0; r < b.Rows.Length; r++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var line = new Border { Background = r == 0 ? Tint : Brushes.Transparent, BorderBrush = r == 0 ? accent : Rule, BorderThickness = new Thickness(0, 0, 0, r == 0 ? 2 : 1) };
                    Grid.SetRow(line, r);
                    Grid.SetColumnSpan(line, cols);
                    grid.Children.Add(line);
                    for (int c = 0; c < b.Rows[r].Length; c++)
                    {
                        bool numeric = c > 0 && LooksNumeric(b.Rows[r][c]);
                        var cell = T(b.Rows[r][c], r == 0 ? 12 : 13.5, r == 0 ? FontWeights.SemiBold : FontWeights.Normal, r == 0 ? accent : null, new Thickness(10, 8, 10, 8));
                        if (numeric || (r == 0 && c > 0 && b.Rows.Skip(1).All(row => c < row.Length && LooksNumeric(row[c])))) cell.TextAlignment = TextAlignment.Right;
                        Grid.SetRow(cell, r);
                        Grid.SetColumn(cell, c);
                        grid.Children.Add(cell);
                    }
                }
                return grid;
            }
            case BlockKind.Chart:
            {
                var sp = new StackPanel { Margin = new Thickness(0, 4, 0, 16) };
                sp.Children.Add(T(b.Text, 13, FontWeights.SemiBold, Muted, new Thickness(0, 0, 0, 10)));
                var values = b.Items.Select(i => i.Split('|')).Select(p => (Label: p[0], Value: double.Parse(p[1], CultureInfo.InvariantCulture))).ToList();
                double max = Math.Max(1, values.Max(v => v.Value));
                var bars = new UniformGrid { Rows = 1, Height = 190 };
                foreach (var (label, value) in values)
                {
                    var col = new DockPanel { Margin = new Thickness(10, 0, 10, 0) };
                    col.Children.Add(T(label, 12, brush: Muted).Also(t => { t.TextAlignment = TextAlignment.Center; t.Margin = new Thickness(0, 6, 0, 0); DockPanel.SetDock(t, Dock.Bottom); }));
                    var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
                    stack.Children.Add(T(value.ToString("0.##", CultureInfo.InvariantCulture), 12.5, FontWeights.SemiBold).Also(t => { t.TextAlignment = TextAlignment.Center; t.Margin = new Thickness(0, 0, 0, 4); }));
                    stack.Children.Add(new Border { Height = Math.Max(4, value / max * 140), CornerRadius = new CornerRadius(4, 4, 0, 0), Background = accent });
                    col.Children.Add(stack);
                    bars.Children.Add(col);
                }
                sp.Children.Add(bars);
                sp.Children.Add(new Border { Height = 1, Background = Rule });
                return sp;
            }
            case BlockKind.Figure:
            {
                var sp = new StackPanel { Margin = new Thickness(0, 4, 0, 14) };
                var color = ((SolidColorBrush)accent).Color;
                var pic = new Grid { Height = b.Height, ClipToBounds = true };
                pic.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Background = new LinearGradientBrush(Color.FromRgb(0xEE, 0xEA, 0xEC), Color.FromArgb(0x55, color.R, color.G, color.B), 50),
                });
                // A simple picture: a sun and two hills.
                var art = new Canvas { ClipToBounds = true };
                art.Children.Add(new Ellipse { Width = 54, Height = 54, Fill = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)) }.Also(e => { Canvas.SetLeft(e, 60); Canvas.SetTop(e, 34); }));
                pic.SizeChanged += (_, _) =>
                {
                    double w = pic.ActualWidth, h = pic.ActualHeight;
                    art.Children.RemoveRange(1, art.Children.Count - 1);
                    art.Children.Add(new Polygon { Points = new PointCollection { new Point(0, h), new Point(w * 0.32, h * 0.42), new Point(w * 0.62, h) }, Fill = new SolidColorBrush(Color.FromArgb(0x70, color.R, color.G, color.B)) });
                    art.Children.Add(new Polygon { Points = new PointCollection { new Point(w * 0.35, h), new Point(w * 0.7, h * 0.3), new Point(w, h * 0.75), new Point(w, h) }, Fill = new SolidColorBrush(Color.FromArgb(0xA0, color.R, color.G, color.B)) });
                };
                pic.Children.Add(new Border { CornerRadius = new CornerRadius(6), Child = art, ClipToBounds = true });
                sp.Children.Add(pic);
                sp.Children.Add(T(b.Text, 12, brush: Muted, margin: new Thickness(0, 8, 0, 0)).Also(t => t.FontStyle = FontStyles.Italic));
                return sp;
            }
            case BlockKind.Stats:
            {
                var grid = new UniformGrid { Rows = 1, Margin = new Thickness(0, 6, 0, 16) };
                foreach (var item in b.Items)
                {
                    var parts = item.Split('|');
                    var cell = new Border { BorderBrush = accent, BorderThickness = new Thickness(3, 0, 0, 0), Padding = new Thickness(12, 2, 8, 2), Margin = new Thickness(0, 0, 12, 0) };
                    var sp = new StackPanel();
                    sp.Children.Add(T(parts[0], 28, FontWeights.Bold, accent));
                    sp.Children.Add(T(parts.Length > 1 ? parts[1] : "", 12.5, brush: Muted));
                    cell.Child = sp;
                    grid.Children.Add(cell);
                }
                return grid;
            }
            case BlockKind.Callout:
                return new Border
                {
                    Background = Tint, BorderBrush = accent, BorderThickness = new Thickness(4, 0, 0, 0), CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(18, 14, 18, 14), Margin = new Thickness(0, 4, 0, 16),
                    Child = T(b.Text, 14.5).Also(t => { t.LineHeight = 22; t.FontStyle = FontStyles.Italic; }),
                };
            case BlockKind.KeyValues:
            {
                var grid = new Grid { Margin = new Thickness(0, 2, 0, 14) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                for (int i = 0; i < b.Items.Length; i++)
                {
                    var parts = b.Items[i].Split('|');
                    bool last = i == b.Items.Length - 1 && parts[0].StartsWith("Total", StringComparison.Ordinal);
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var k = T(parts[0], 13.5, last ? FontWeights.Bold : FontWeights.Normal, last ? null : Muted, new Thickness(0, 4, 12, 4));
                    var v = T(parts.Length > 1 ? parts[1] : "", last ? 16 : 14, FontWeights.SemiBold, last ? accent : null, new Thickness(0, 4, 0, 4));
                    Grid.SetRow(k, i);
                    Grid.SetRow(v, i);
                    Grid.SetColumn(v, 1);
                    grid.Children.Add(k);
                    grid.Children.Add(v);
                }
                return grid;
            }
            case BlockKind.Field:
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                sp.Children.Add(T(b.Text, 12, brush: Muted, margin: new Thickness(0, 0, 0, 4)));
                bool empty = b.Value.Length == 0;
                sp.Children.Add(new Border
                {
                    Height = 34,
                    Background = empty ? FieldEmpty : FieldFilled,
                    BorderBrush = empty ? FieldEmptyBorder : Rule,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(10, 0, 10, 0),
                    Child = T(b.Value, 14.5).Also(t => { t.VerticalAlignment = VerticalAlignment.Center; t.TextWrapping = TextWrapping.NoWrap; t.FontFamily = new FontFamily("Segoe UI"); }),
                });
                return sp;
            }
            case BlockKind.Checkboxes:
            {
                var sp = new StackPanel { Margin = new Thickness(0, 2, 0, 12) };
                foreach (var item in b.Items)
                {
                    bool ticked = item.StartsWith("[x]", StringComparison.OrdinalIgnoreCase);
                    string text = item.Length > 4 ? item[4..] : item;
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
                    var box = new Border
                    {
                        Width = 18, Height = 18, Margin = new Thickness(0, 0, 10, 0), CornerRadius = new CornerRadius(3),
                        BorderBrush = ticked ? accent : FieldEmptyBorder, BorderThickness = new Thickness(1.5),
                        Background = ticked ? accent : FieldEmpty,
                    };
                    if (ticked)
                    {
                        var check = new TextBlock { Text = Ic.Check, FontSize = 12, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                        check.SetResourceReference(TextBlock.FontFamilyProperty, "IconFontFilled");
                        box.Child = check;
                    }
                    row.Children.Add(box);
                    row.Children.Add(T(text, 14.5).Also(t => t.VerticalAlignment = VerticalAlignment.Center));
                    sp.Children.Add(row);
                }
                return sp;
            }
            case BlockKind.Signature:
            {
                var sp = new StackPanel { Margin = new Thickness(0, 6, 0, 10), Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
                var area = new Grid { Height = 54 };
                if (b.Value.Length > 0)
                {
                    area.Children.Add(new TextBlock { Text = b.Value, FontFamily = new FontFamily("Segoe Script, Ink Free, Segoe Print"), FontSize = 26, Foreground = SignInk, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(8, 0, 0, 2) });
                }
                else
                {
                    area.Background = FieldEmpty;
                    var tag = new Border
                    {
                        Background = StickyBg, CornerRadius = new CornerRadius(4), Padding = new Thickness(10, 4, 10, 4),
                        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0),
                        Child = new TextBlock { Text = "Sign here  ›", FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = StickyInk, FontFamily = new FontFamily("Segoe UI") },
                    };
                    area.Children.Add(tag);
                }
                sp.Children.Add(area);
                sp.Children.Add(new Border { Height = 1.5, Background = Ink });
                sp.Children.Add(T(b.Text, 12, brush: Muted, margin: new Thickness(0, 5, 0, 0)));
                return sp;
            }
            case BlockKind.Divider:
                return new Border { Height = 1, Background = Rule, Margin = new Thickness(0, 10, 0, 14) };
            case BlockKind.Spacer:
                return new Border { Height = b.Height };
            case BlockKind.Mono:
            {
                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
                foreach (var line in b.Items)
                    sp.Children.Add(new TextBlock { Text = line.Length == 0 ? " " : line, FontFamily = new FontFamily("Consolas"), FontSize = 16, LineHeight = 24 });
                return sp;
            }
            default:
                return T(b.PlainText, 14.5);
        }
    }

    private static bool LooksNumeric(string s) => s.Length > 0 && (char.IsDigit(s[0]) || s[0] == '$' || s[0] == '−' || s[0] == '-');
}
