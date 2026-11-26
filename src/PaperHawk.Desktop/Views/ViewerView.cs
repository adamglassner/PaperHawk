using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PaperHawk.Models;
using PaperHawk.Services;

namespace PaperHawk.Views;

public enum ZoomMode { Custom, FitWidth, FitPage }
public enum PageLayout { Single, TwoPage }
public enum LeftPanel { None, Thumbnails, Bookmarks }
public enum RightPanel { None, Comments, Properties }
public enum ViewerTool { Select, Hand }

/// <summary>
/// The PDF viewer, laid out like Acrobat: a toolbar on top, a rail with thumbnails and bookmarks on the left, the pages
/// in the middle with floating page and zoom controls, and comments or document properties on the right.
/// Reading works on the sample documents (paging, zoom, fit, one or two pages, rotation, the hand tool, comments);
/// editing, signing, highlighting and the other tools are coming soon.
/// </summary>
public sealed class ViewerView : UserControl
{
    public static readonly double[] ZoomSteps = [0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0];
    private const double PageGap = 18, PadX = 28, PadTop = 22, PadBottom = 96;

    public PdfDocument Document { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public double Zoom { get; private set; } = 1;
    public ZoomMode ZoomMode { get; private set; } = ZoomMode.FitWidth;
    public PageLayout Layout { get; private set; } = PageLayout.Single;
    public int Rotation { get; private set; }
    public LeftPanel LeftPanel { get; private set; } = LeftPanel.Thumbnails;
    public RightPanel RightPanel { get; private set; } = RightPanel.None;
    public ViewerTool Tool { get; private set; } = ViewerTool.Select;
    public Annotation? SelectedAnnotation { get; private set; }
    public int PageCount => Document.Pages.Length;
    public int ThumbnailCount => _thumbs.Count;
    public int CommentCardCount => _commentCards.Count;

    private readonly ScrollViewer _scroll = new()
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        Focusable = false,
        PanningMode = PanningMode.Both,
    };
    private readonly Border _scrollContent = new() { Padding = new Thickness(PadX, PadTop, PadX, PadBottom) };
    private readonly StackPanel _pagesHost = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly List<RenderedPage> _rendered = [];
    private readonly List<Border> _frames = [];
    private readonly List<Border> _thumbs = [];
    private readonly Dictionary<Annotation, Border> _commentCards = [];

    private readonly ColumnDefinition _leftCol = new() { Width = GridLength.Auto };
    private readonly ColumnDefinition _rightCol = new() { Width = GridLength.Auto };
    private readonly ContentControl _leftHost = new() { Focusable = false };
    private readonly ContentControl _rightHost = new() { Focusable = false };
    private readonly ContentControl _banner = new() { Focusable = false };
    private readonly Border _leftPanel = new() { Width = 200, BorderThickness = new Thickness(0, 0, 1, 0) };
    private readonly Border _rightPanel = new() { Width = 330, BorderThickness = new Thickness(1, 0, 0, 0) };
    private readonly Dictionary<LeftPanel, Button> _railButtons = [];
    private readonly Dictionary<RightPanel, Button> _rightButtons = [];
    private readonly Dictionary<PageLayout, RadioButton> _layoutButtons = [];
    private readonly Dictionary<ViewerTool, RadioButton> _toolButtons = [];
    private readonly Button _docButton = new() { Style = Ui.S("BaseButton"), Padding = new Thickness(8, 6, 10, 6), Margin = new Thickness(0, 0, 14, 0) };
    private readonly TextBox _pageBox = new() { Width = 46, MinHeight = 30, Padding = new Thickness(4, 3, 4, 3), HorizontalContentAlignment = HorizontalAlignment.Center };
    private readonly TextBlock _pageOf = Ui.Text("", muted: true, size: 13);
    private readonly Button _zoomButton = new() { Style = Ui.S("BaseButton"), Padding = new Thickness(8, 4, 8, 4), MinHeight = 30, MinWidth = 78 };
    private readonly TextBlock _status = Ui.Text("", muted: true, size: 12);

    private double _pinnedOffset = double.NaN;
    private Point? _dragStart;
    private Point _dragOffset;

    public ViewerView(PdfDocument document, int? page = null, RightPanel? right = null)
    {
        Document = document;
        Focusable = true;
        FocusVisualStyle = null;
        if (right.HasValue) RightPanel = right.Value;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(BuildToolbar().At(0, 0));
        root.Children.Add(BuildBody().At(0, 1));
        root.Children.Add(BuildStatusBar().At(0, 2));
        Content = root;

        _scrollContent.Child = _pagesHost;
        _scroll.Content = _scrollContent;
        _scroll.SetResourceReference(BackgroundProperty, "Brush.Canvas");
        _scroll.ScrollChanged += (_, e) => { if (e.VerticalChange != 0 || e.ExtentHeightChange != 0) TrackPage(); };
        _scroll.SizeChanged += (_, _) =>
        {
            if (ZoomMode == ZoomMode.Custom || !IsLoaded) return;
            ApplyZoom();
            int keep = CurrentPage;
            Dispatcher.BeginInvoke(new Action(() => GoToPage(keep)), System.Windows.Threading.DispatcherPriority.Loaded);
        };
        _scroll.PreviewMouseWheel += OnWheel;
        _scroll.PreviewMouseLeftButtonDown += OnPanStart;
        _scroll.PreviewMouseMove += OnPanMove;
        _scroll.PreviewMouseLeftButtonUp += OnPanEnd;

        Loaded += (_, _) =>
        {
            ApplyZoom();
            GoToPage(CurrentPage);
            Focus();
        };

        ShowDocument(document, page ?? document.LastPage);
    }

    // ===== Public actions (also used by the self-test) =====

    public void ShowDocument(PdfDocument document, int page = 1)
    {
        Document = document;
        SelectedAnnotation = null;
        MainWindow.Instance?.SetCurrentDocument(document);
        UpdateDocButton();
        BuildPages();
        BuildBanner();
        ShowLeftPanel(LeftPanel);
        ShowRightPanel(RightPanel);
        CurrentPage = Math.Clamp(page, 1, PageCount);
        ApplyZoom();
        GoToPage(CurrentPage);
        UpdateStatus();
    }

    public void GoToPage(int number)
    {
        number = Math.Clamp(number, 1, PageCount);
        CurrentPage = number;
        UpdatePageControls();
        if (!IsLoaded) return;
        _scroll.UpdateLayout();
        var frame = _frames[number - 1];
        double y = frame.TranslatePoint(new Point(0, 0), _scrollContent).Y - PadTop + 4;
        double target = Math.Max(0, Math.Min(y, _scroll.ScrollableHeight));
        _pinnedOffset = target;
        _scroll.ScrollToVerticalOffset(target);
    }

    public void NextPage() => GoToPage(Math.Min(PageCount, CurrentPage + (Layout == PageLayout.TwoPage ? 2 - (CurrentPage + 1) % 2 : 1)));
    public void PreviousPage() => GoToPage(Layout == PageLayout.TwoPage ? ((CurrentPage - 1) / 2 - 1) * 2 + 1 : CurrentPage - 1);

    public void SetZoom(double zoom)
    {
        ZoomMode = ZoomMode.Custom;
        Zoom = Math.Clamp(zoom, ZoomSteps[0], ZoomSteps[^1]);
        ApplyZoom();
        GoToPage(CurrentPage);
    }

    public void SetZoomMode(ZoomMode mode)
    {
        ZoomMode = mode;
        ApplyZoom();
        GoToPage(CurrentPage);
    }

    public void ZoomIn() => SetZoom(ZoomSteps.FirstOrDefault(z => z > Zoom + 0.001, ZoomSteps[^1]));
    public void ZoomOut() => SetZoom(ZoomSteps.LastOrDefault(z => z < Zoom - 0.001, ZoomSteps[0]));

    public void SetLayout(PageLayout layout)
    {
        Layout = layout;
        if (_layoutButtons.TryGetValue(layout, out var rb)) rb.IsChecked = true;
        ArrangePages();
        ApplyZoom();
        GoToPage(CurrentPage);
    }

    public void Rotate(int degrees)
    {
        Rotation = ((Rotation + degrees) % 360 + 360) % 360;
        ApplyZoom();
        GoToPage(CurrentPage);
        Toaster.Show(Rotation == 0 ? "Pages are back the right way up." : $"View rotated {Rotation}°. Rotating the pages in the file is coming soon.");
    }

    public void SetTool(ViewerTool tool)
    {
        Tool = tool;
        if (_toolButtons.TryGetValue(tool, out var rb)) rb.IsChecked = true;
        _scroll.Cursor = tool == ViewerTool.Hand ? Cursors.Hand : null;
    }

    public void ShowLeftPanel(LeftPanel panel)
    {
        LeftPanel = panel;
        foreach (var (p, b) in _railButtons) SetActive(b, p == panel);
        _leftHost.Content = panel switch
        {
            LeftPanel.Thumbnails => BuildThumbnails(),
            LeftPanel.Bookmarks => BuildBookmarks(),
            _ => null,
        };
        _leftPanel.Visibility = panel == LeftPanel.None ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ToggleLeftPanel(LeftPanel panel) => ShowLeftPanel(LeftPanel == panel ? LeftPanel.None : panel);

    public void ShowRightPanel(RightPanel panel)
    {
        RightPanel = panel;
        foreach (var (p, b) in _rightButtons) SetActive(b, p == panel);
        _commentCards.Clear();
        _rightHost.Content = panel switch
        {
            RightPanel.Comments => BuildComments(),
            RightPanel.Properties => BuildProperties(),
            _ => null,
        };
        _rightPanel.Visibility = panel == RightPanel.None ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ToggleRightPanel(RightPanel panel) => ShowRightPanel(RightPanel == panel ? RightPanel.None : panel);

    public void SelectAnnotation(Annotation? annotation)
    {
        SelectedAnnotation = annotation;
        foreach (var rp in _rendered)
            foreach (var (a, mark) in rp.Marks)
                mark.SetResourceReference(Border.BorderBrushProperty, a == annotation ? "Brush.Accent" : "Brush.Transparent");
        foreach (var (a, card) in _commentCards)
            card.SetResourceReference(Border.BorderBrushProperty, a == annotation ? "Brush.Accent" : "Brush.Border");
        if (annotation == null) return;
        if (RightPanel != RightPanel.Comments) ShowRightPanel(RightPanel.Comments);
        if (_commentCards.TryGetValue(annotation, out var c)) c.BringIntoView();
        GoToPage(annotation.Page.Number);
    }

    private static void SetActive(Button b, bool active)
    {
        b.SetResourceReference(BackgroundProperty, active ? "Brush.AccentSoft" : "Brush.Transparent");
        b.SetResourceReference(ForegroundProperty, active ? "Brush.Accent" : "Brush.TextMuted");
    }

    // ===== Pages =====

    private void BuildPages()
    {
        _rendered.Clear();
        _frames.Clear();
        foreach (var page in Document.Pages)
        {
            var r = PageRenderer.Render(page, interactive: true, SelectAnnotation);
            _rendered.Add(r);
            var frame = new Border
            {
                Child = r.Paper,
                Margin = new Thickness(PageGap / 2, 0, PageGap / 2, PageGap),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.22, Direction = 270 },
            };
            System.Windows.Automation.AutomationProperties.SetName(frame, $"Page {page.Number}");
            _frames.Add(frame);
        }
        ArrangePages();
    }

    private void ArrangePages()
    {
        foreach (var f in _frames) (f.Parent as Panel)?.Children.Remove(f);
        _pagesHost.Children.Clear();
        int step = Layout == PageLayout.TwoPage ? 2 : 1;
        for (int i = 0; i < _frames.Count; i += step)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            for (int j = i; j < Math.Min(i + step, _frames.Count); j++) row.Children.Add(_frames[j]);
            _pagesHost.Children.Add(row);
        }
    }

    private Size RotatedPageSize(PdfPage p) => Rotation % 180 == 0 ? new Size(p.Width, p.Height) : new Size(p.Height, p.Width);

    private void ApplyZoom()
    {
        if (ZoomMode != ZoomMode.Custom)
        {
            double vw = _scroll.ViewportWidth > 0 ? _scroll.ViewportWidth : Math.Max(400, _scroll.ActualWidth);
            double vh = _scroll.ViewportHeight > 0 ? _scroll.ViewportHeight : Math.Max(300, _scroll.ActualHeight);
            if (vw <= 0 || double.IsNaN(vw)) vw = 900;
            if (vh <= 0 || double.IsNaN(vh)) vh = 700;
            int across = Layout == PageLayout.TwoPage && PageCount > 1 ? 2 : 1;
            var size = RotatedPageSize(Document.Pages[0]);
            double widest = Document.Pages.Max(p => RotatedPageSize(p).Width);
            double fitW = (vw - PadX * 2 - PageGap * across - 22) / (widest * across);
            double fitH = (vh - PadTop - PageGap - 12) / size.Height;
            Zoom = Math.Clamp(ZoomMode == ZoomMode.FitPage ? Math.Min(fitW, fitH) : fitW, 0.2, 4);
        }
        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(Zoom, Zoom));
        transform.Children.Add(new RotateTransform(Rotation));
        transform.Freeze();
        foreach (var r in _rendered) r.Paper.LayoutTransform = transform;
        foreach (var t in _thumbs) if (t.Child is Viewbox { Child: FrameworkElement thumbPaper }) thumbPaper.LayoutTransform = new RotateTransform(Rotation);
        UpdateZoomButton();
    }

    private void TrackPage()
    {
        if (!double.IsNaN(_pinnedOffset))
        {
            if (Math.Abs(_scroll.VerticalOffset - _pinnedOffset) < 1.5) return;
            _pinnedOffset = double.NaN;
        }
        double line = _scroll.VerticalOffset + Math.Min(_scroll.ViewportHeight * 0.35, 260);
        int page = 1;
        for (int i = 0; i < _frames.Count; i++)
        {
            if (!_frames[i].IsLoaded) continue;
            double top = _frames[i].TranslatePoint(new Point(0, 0), _scrollContent).Y;
            if (top <= line) page = i + 1;
        }
        if (_scroll.VerticalOffset >= _scroll.ScrollableHeight - 1 && _scroll.ScrollableHeight > 0) page = Math.Max(page, LastVisiblePage());
        if (page != CurrentPage)
        {
            CurrentPage = page;
            UpdatePageControls();
        }
    }

    private int LastVisiblePage()
    {
        int last = 1;
        double bottom = _scroll.VerticalOffset + _scroll.ViewportHeight;
        for (int i = 0; i < _frames.Count; i++)
            if (_frames[i].TranslatePoint(new Point(0, 0), _scrollContent).Y < bottom - 80) last = i + 1;
        return Layout == PageLayout.TwoPage ? Math.Max(1, last - (last + 1) % 2) : last;
    }

    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        e.Handled = true;
        if (e.Delta > 0) ZoomIn(); else ZoomOut();
    }

    private void OnPanStart(object sender, MouseButtonEventArgs e)
    {
        if (Tool != ViewerTool.Hand) return;
        _dragStart = e.GetPosition(_scroll);
        _dragOffset = new Point(_scroll.HorizontalOffset, _scroll.VerticalOffset);
        _scroll.CaptureMouse();
        e.Handled = true;
    }

    private void OnPanMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not Point start || e.LeftButton != MouseButtonState.Pressed) return;
        var p = e.GetPosition(_scroll);
        _scroll.ScrollToHorizontalOffset(_dragOffset.X - (p.X - start.X));
        _scroll.ScrollToVerticalOffset(_dragOffset.Y - (p.Y - start.Y));
    }

    private void OnPanEnd(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart == null) return;
        _dragStart = null;
        _scroll.ReleaseMouseCapture();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox) { base.OnPreviewKeyDown(e); return; }
        var mods = Keyboard.Modifiers;
        bool handled = true;
        if (mods == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.OemPlus or Key.Add: ZoomIn(); break;
                case Key.OemMinus or Key.Subtract: ZoomOut(); break;
                case Key.D0 or Key.NumPad0: SetZoomMode(ZoomMode.FitPage); break;
                case Key.D1 or Key.NumPad1: SetZoom(1); break;
                case Key.D2 or Key.NumPad2: SetZoomMode(ZoomMode.FitWidth); break;
                default: handled = false; break;
            }
        }
        else if (mods == ModifierKeys.None)
        {
            switch (e.Key)
            {
                case Key.PageDown or Key.Right: NextPage(); break;
                case Key.PageUp or Key.Left: PreviousPage(); break;
                case Key.Home: GoToPage(1); break;
                case Key.End: GoToPage(PageCount); break;
                case Key.H: SetTool(Tool == ViewerTool.Hand ? ViewerTool.Select : ViewerTool.Hand); break;
                case Key.Escape: SelectAnnotation(null); break;
                default: handled = false; break;
            }
        }
        else handled = false;
        e.Handled = handled;
        base.OnPreviewKeyDown(e);
    }

    // ===== Toolbar =====

    private FrameworkElement BuildToolbar()
    {
        var bar = new Border { BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(16, 10, 16, 10) };
        bar.SetResourceReference(Border.BackgroundProperty, "Brush.Bg");
        bar.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var dock = new DockPanel();

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(Ui.SearchBox("Find in document…", 190).Also(s => s.Margin = new Thickness(0, 0, 8, 0)));
        right.Children.Add(Ui.ToolButton("Sharing", Ic.Share, "Share"));
        right.Children.Add(Ui.ToolButton("Printing", Ic.Print, "Print (Ctrl+P)"));
        right.Children.Add(Ui.ToolButton("Saving a copy", Ic.Download, "Save a copy"));
        right.Children.Add(Ui.Separator().Also(s => { s.Height = 22; s.Margin = new Thickness(8, 4, 8, 4); }));
        right.Children.Add(PanelButton(RightPanel.Comments, Ic.Comments, "Comments"));
        right.Children.Add(PanelButton(RightPanel.Properties, Ic.Info, "Document properties"));
        DockPanel.SetDock(right, Dock.Right);
        dock.Children.Add(right);

        _docButton.Click += (_, _) => ShowDocumentMenu();
        System.Windows.Automation.AutomationProperties.SetName(_docButton, "Switch document");
        DockPanel.SetDock(_docButton, Dock.Left);
        dock.Children.Add(_docButton);

        var groups = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };

        var tools = new StackPanel { Orientation = Orientation.Horizontal };
        tools.Children.Add(ToolRadio(ViewerTool.Select, Ic.Cursor, "Select (V)"));
        tools.Children.Add(ToolRadio(ViewerTool.Hand, Ic.Hand, "Hand tool: drag to move around (H)"));
        groups.Children.Add(Group(tools));

        var markup = new StackPanel { Orientation = Orientation.Horizontal };
        markup.Children.Add(Ui.ToolButton("Adding comments", Ic.Comment, "Add a comment"));
        markup.Children.Add(Ui.ToolButton("Highlighting text", Ic.Highlight, "Highlight text"));
        markup.Children.Add(Ui.ToolButton("Underlining text", Ic.Underline, "Underline text"));
        markup.Children.Add(Ui.ToolButton("Striking out text", Ic.Strike, "Strike through text"));
        markup.Children.Add(Ui.ToolButton("Drawing", Ic.Draw, "Draw"));
        markup.Children.Add(Ui.ToolButton("Text boxes", Ic.TextTool, "Add a text box"));
        markup.Children.Add(Ui.ToolButton("Shapes", Ic.Shapes, "Shapes"));
        groups.Children.Add(Group(markup));

        var work = new StackPanel { Orientation = Orientation.Horizontal };
        work.Children.Add(Ui.SoonButton("Fill & Sign", Ic.Signature, "Fill & Sign").Also(b => b.Margin = new Thickness(0, 0, 6, 0)));
        work.Children.Add(Ui.SoonButton("Editing PDFs", Ic.Edit, "Edit", "PrimaryButton"));
        groups.Children.Add(work);
        dock.Children.Add(groups);

        bar.Child = dock;
        return bar;
    }

    private static FrameworkElement Group(UIElement content)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(content);
        sp.Children.Add(Ui.Separator().Also(s => { s.Margin = new Thickness(8, 4, 8, 4); s.Height = 22; }));
        return sp;
    }

    private RadioButton ToolRadio(ViewerTool tool, string glyph, string tip)
    {
        var rb = new RadioButton
        {
            Style = Ui.S("ChipRadio"), GroupName = "ViewerTool" + GetHashCode(), IsChecked = tool == Tool, ToolTip = tip,
            Content = Ui.Icon(glyph, 15), Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(0),
        };
        rb.Checked += (_, _) => { if (Tool != tool) SetTool(tool); };
        System.Windows.Automation.AutomationProperties.SetName(rb, tip);
        _toolButtons[tool] = rb;
        return rb;
    }

    private Button PanelButton(RightPanel panel, string glyph, string tip)
    {
        var b = new Button { Style = Ui.S("BaseButton"), Padding = new Thickness(7), MinHeight = 32, MinWidth = 32, ToolTip = tip, Content = Ui.Icon(glyph, 16), Margin = new Thickness(2, 0, 0, 0) };
        b.Click += (_, _) => ToggleRightPanel(panel);
        System.Windows.Automation.AutomationProperties.SetName(b, tip);
        _rightButtons[panel] = b;
        return b;
    }

    private void UpdateDocButton()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(Ui.Icon(Ic.Pdf, 18, null, filled: true).Also(i => { i.Foreground = Ui.Hex(Document.Color); i.Margin = new Thickness(0, 0, 9, 0); }));
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, MaxWidth = 300 };
        texts.Children.Add(Ui.Text(Document.Title, size: 15, weight: FontWeights.Bold).Also(t => t.SetResourceReference(TextBlock.FontFamilyProperty, "DisplayFont")));
        texts.Children.Add(Ui.Text(Document.FileName, muted: true, size: 11.5));
        row.Children.Add(texts);
        row.Children.Add(Ui.Icon(Ic.ChevronDown, 12, "Brush.TextMuted").Also(i => i.Margin = new Thickness(10, 2, 0, 0)));
        _docButton.Content = row;
        _docButton.ToolTip = $"{Document.Kind} · {Document.PageCountText} · {Document.SizeText}";
    }

    private void ShowDocumentMenu()
    {
        var menu = new ContextMenu { PlacementTarget = _docButton, Placement = PlacementMode.Bottom };
        foreach (var d in SampleDocs.Recent)
        {
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(Ui.Dot(d.Color, 10).Also(x => x.Margin = new Thickness(0, 0, 10, 0)));
            header.Children.Add(new TextBlock { Text = d.Title, FontWeight = d == Document ? FontWeights.SemiBold : FontWeights.Normal });
            var item = new MenuItem { Header = header };
            var target = d;
            item.Click += (_, _) => ShowDocument(target, target.LastPage);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var open = new MenuItem { Header = "Open a PDF file…" };
        open.Click += (_, _) => Toaster.Soon("Opening your own PDF files");
        menu.Items.Add(open);
        menu.IsOpen = true;
    }

    // ===== Body =====

    private FrameworkElement BuildBody()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(_leftCol);
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(_rightCol);

        grid.Children.Add(BuildRail().At(0));

        _leftPanel.Child = _leftHost;
        _leftPanel.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
        _leftPanel.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        grid.Children.Add(_leftPanel.At(1));

        var center = new Grid();
        center.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        center.RowDefinitions.Add(new RowDefinition());
        center.Children.Add(_banner.At(0, 0));
        center.Children.Add(_scroll.At(0, 1));
        center.Children.Add(BuildPageControls().At(0, 1));
        grid.Children.Add(center.At(2));

        _rightPanel.Child = _rightHost;
        _rightPanel.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
        _rightPanel.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        grid.Children.Add(_rightPanel.At(3));
        return grid;
    }

    private FrameworkElement BuildRail()
    {
        var rail = new Border { Width = 52, BorderThickness = new Thickness(0, 0, 1, 0), Padding = new Thickness(6, 10, 6, 10) };
        rail.SetResourceReference(Border.BackgroundProperty, "Brush.Bg");
        rail.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var sp = new StackPanel();
        sp.Children.Add(RailButton(LeftPanel.Thumbnails, Ic.Pages, "Page thumbnails"));
        sp.Children.Add(RailButton(LeftPanel.Bookmarks, Ic.Bookmark, "Bookmarks"));
        sp.Children.Add(RailSoon("Attachments", Ic.Attach));
        sp.Children.Add(RailSoon("Layers", Ic.Layers));
        sp.Children.Add(RailSoon("The signatures panel", Ic.DocSign, "Signatures"));
        sp.Children.Add(RailSoon("Accessibility tags", Ic.Accessibility, "Accessibility tags"));
        rail.Child = sp;
        return rail;
    }

    private Button RailButton(LeftPanel panel, string glyph, string tip)
    {
        var b = new Button { Style = Ui.S("BaseButton"), Padding = new Thickness(8), MinHeight = 38, Margin = new Thickness(0, 0, 0, 4), ToolTip = tip, Content = Ui.Icon(glyph, 18) };
        b.Click += (_, _) => ToggleLeftPanel(panel);
        System.Windows.Automation.AutomationProperties.SetName(b, tip);
        _railButtons[panel] = b;
        SetActive(b, false);
        return b;
    }

    private static Button RailSoon(string feature, string glyph, string? tip = null)
    {
        var b = new Button { Style = Ui.S("BaseButton"), Padding = new Thickness(8), MinHeight = 38, Margin = new Thickness(0, 0, 0, 4), ToolTip = (tip ?? feature) + " (coming soon)", Content = Ui.Icon(glyph, 18), Tag = feature };
        b.SetResourceReference(ForegroundProperty, "Brush.TextMuted");
        b.Click += (s, _) => Toaster.Soon(s);
        System.Windows.Automation.AutomationProperties.SetName(b, tip ?? feature);
        return b;
    }

    private void BuildBanner()
    {
        string? glyph = null, text = null, action = null, feature = null;
        if (Document.IsScan)
        {
            glyph = Ic.Scan; feature = "Text recognition (OCR)"; action = "Recognize text";
            text = "This PDF is a scanned picture, so its text can't be searched or copied yet.";
        }
        else if (Document.NeedsSignature)
        {
            glyph = Ic.Signature; feature = "Signing"; action = "Sign now";
            text = "Your signature is requested. Signature fields are marked \"Sign here\".";
        }
        else if (Document.IsForm)
        {
            glyph = Ic.Form; feature = "Filling in forms"; action = "Fill in form";
            text = "This document has form fields you can fill in. They're shaded blue.";
        }
        if (text == null) { _banner.Content = null; return; }
        var border = new Border { Padding = new Thickness(18, 9, 14, 9), BorderThickness = new Thickness(0, 0, 0, 1) };
        border.SetResourceReference(Border.BackgroundProperty, "Brush.AccentSoft");
        border.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var dock = new DockPanel();
        dock.Children.Add(Ui.SoonButton(feature!, null, action, "PrimaryButton").Also(b => { b.Padding = new Thickness(14, 5, 14, 5); b.MinHeight = 30; DockPanel.SetDock(b, Dock.Right); }));
        dock.Children.Add(Ui.Icon(glyph!, 16, "Brush.Accent").Also(i => { i.Margin = new Thickness(0, 0, 10, 0); DockPanel.SetDock(i, Dock.Left); }));
        dock.Children.Add(Ui.Text(text, size: 13));
        border.Child = dock;
        _banner.Content = border;
    }

    private FrameworkElement BuildPageControls()
    {
        var pill = new Border
        {
            CornerRadius = new CornerRadius(14), Padding = new Thickness(8, 5, 8, 5), BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 22),
            Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.2, Direction = 270 },
        };
        pill.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
        pill.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var row = new StackPanel { Orientation = Orientation.Horizontal };

        row.Children.Add(Small(Ic.First, "First page (Home)", () => GoToPage(1)));
        row.Children.Add(Small(Ic.ChevronLeft, "Previous page (Page Up)", PreviousPage));
        _pageBox.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            if (int.TryParse(_pageBox.Text.Trim(), out int n)) GoToPage(n); else UpdatePageControls();
            e.Handled = true;
            Focus();
        };
        _pageBox.LostKeyboardFocus += (_, _) => UpdatePageControls();
        System.Windows.Automation.AutomationProperties.SetName(_pageBox, "Page number");
        row.Children.Add(_pageBox.Also(b => b.Margin = new Thickness(4, 0, 6, 0)));
        row.Children.Add(_pageOf.Also(t => t.Margin = new Thickness(0, 0, 4, 0)));
        row.Children.Add(Small(Ic.ChevronRight, "Next page (Page Down)", NextPage));
        row.Children.Add(Small(Ic.Last, "Last page (End)", () => GoToPage(PageCount)));
        row.Children.Add(Ui.Separator().Also(s => { s.Height = 20; s.Margin = new Thickness(6, 4, 6, 4); }));

        row.Children.Add(Small(Ic.ZoomOut, "Zoom out (Ctrl+−)", ZoomOut));
        _zoomButton.Click += (_, _) => ShowZoomMenu();
        System.Windows.Automation.AutomationProperties.SetName(_zoomButton, "Zoom");
        row.Children.Add(_zoomButton);
        row.Children.Add(Small(Ic.ZoomIn, "Zoom in (Ctrl+=)", ZoomIn));
        row.Children.Add(Ui.Separator().Also(s => { s.Height = 20; s.Margin = new Thickness(6, 4, 6, 4); }));

        foreach (var (layout, glyph, tip) in new[] { (PageLayout.Single, Ic.SinglePage, "One page at a time, scrolling"), (PageLayout.TwoPage, Ic.TwoPage, "Two pages side by side") })
        {
            var rb = new RadioButton
            {
                Style = Ui.S("ChipRadio"), GroupName = "ViewerLayout" + GetHashCode(), IsChecked = layout == Layout, ToolTip = tip,
                Content = Ui.Icon(glyph, 14), Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(0),
            };
            var l = layout;
            rb.Checked += (_, _) => { if (Layout != l) SetLayout(l); };
            System.Windows.Automation.AutomationProperties.SetName(rb, tip);
            _layoutButtons[layout] = rb;
            row.Children.Add(rb);
        }
        row.Children.Add(Ui.Separator().Also(s => { s.Height = 20; s.Margin = new Thickness(4, 4, 6, 4); }));
        row.Children.Add(Small(Ic.RotateCcw, "Rotate view left", () => Rotate(-90)));
        row.Children.Add(Small(Ic.RotateCw, "Rotate view right", () => Rotate(90)));

        pill.Child = row;
        return pill;
    }

    private static Button Small(string glyph, string tip, Action click)
    {
        var b = new Button { Style = Ui.S("BaseButton"), Padding = new Thickness(6), MinHeight = 30, MinWidth = 30, ToolTip = tip, Content = Ui.Icon(glyph, 14) };
        b.Click += (_, _) => click();
        System.Windows.Automation.AutomationProperties.SetName(b, tip);
        return b;
    }

    private void ShowZoomMenu()
    {
        var menu = new ContextMenu { PlacementTarget = _zoomButton, Placement = PlacementMode.Top };
        void Add(string header, Action action, bool current)
        {
            var item = new MenuItem { Header = new TextBlock { Text = header, FontWeight = current ? FontWeights.SemiBold : FontWeights.Normal } };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }
        Add("Fit width   (Ctrl+2)", () => SetZoomMode(ZoomMode.FitWidth), ZoomMode == ZoomMode.FitWidth);
        Add("Fit page   (Ctrl+0)", () => SetZoomMode(ZoomMode.FitPage), ZoomMode == ZoomMode.FitPage);
        menu.Items.Add(new Separator());
        foreach (var z in ZoomSteps)
        {
            double zoom = z;
            Add(z == 1 ? "100%  Actual size   (Ctrl+1)" : $"{z * 100:0}%", () => SetZoom(zoom), ZoomMode == ZoomMode.Custom && Math.Abs(Zoom - z) < 0.001);
        }
        menu.IsOpen = true;
    }

    private void UpdatePageControls()
    {
        _pageBox.Text = CurrentPage.ToString();
        _pageOf.Text = $"of {PageCount}";
        for (int i = 0; i < _thumbs.Count; i++)
            _thumbs[i].SetResourceReference(Border.BorderBrushProperty, i + 1 == CurrentPage ? "Brush.Accent" : "Brush.Border");
        if (_thumbs.Count >= CurrentPage && IsLoaded) _thumbs[CurrentPage - 1].BringIntoView();
        UpdateStatus();
    }

    private void UpdateZoomButton()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(Ui.Text($"{Zoom * 100:0}%", size: 13, weight: FontWeights.SemiBold));
        row.Children.Add(Ui.Icon(Ic.ChevronDown, 10, "Brush.TextMuted").Also(i => i.Margin = new Thickness(6, 1, 0, 0)));
        _zoomButton.Content = row;
        _zoomButton.ToolTip = ZoomMode switch { ZoomMode.FitWidth => "Zoom: fit width", ZoomMode.FitPage => "Zoom: fit page", _ => "Zoom" };
    }

    // ===== Left panels =====

    private FrameworkElement PanelShell(string title, string? subtitle, UIElement body, UIElement? headerRight = null)
    {
        var dock = new DockPanel();
        var head = new DockPanel { Margin = new Thickness(16, 14, 10, 10) };
        if (headerRight != null) { DockPanel.SetDock(headerRight, Dock.Right); head.Children.Add(headerRight); }
        var titles = new StackPanel();
        titles.Children.Add(Ui.Text(title, size: 14, weight: FontWeights.SemiBold));
        if (subtitle != null) titles.Children.Add(Ui.Text(subtitle, muted: true, size: 11.5));
        head.Children.Add(titles);
        DockPanel.SetDock(head, Dock.Top);
        dock.Children.Add(head);
        dock.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false, Content = body });
        return dock;
    }

    private FrameworkElement BuildThumbnails()
    {
        _thumbs.Clear();
        var list = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        foreach (var page in Document.Pages)
        {
            var paper = PageRenderer.Render(page, interactive: false).Paper;
            paper.LayoutTransform = new RotateTransform(Rotation);
            var frame = new Border
            {
                BorderThickness = new Thickness(2.5), CornerRadius = new CornerRadius(4), Padding = new Thickness(0),
                Child = new Viewbox { Width = 116, Stretch = Stretch.Uniform, Child = paper },
            };
            frame.SetResourceReference(Border.BorderBrushProperty, page.Number == CurrentPage ? "Brush.Accent" : "Brush.Border");
            _thumbs.Add(frame);
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(frame);
            var label = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
            label.Children.Add(Ui.Text(page.Number.ToString(), muted: true, size: 12));
            if (page.Annotations.Length > 0)
                label.Children.Add(Ui.Icon(Ic.Comment, 11, "Brush.Accent", filled: true).Also(i => { i.Margin = new Thickness(6, 1, 0, 0); i.ToolTip = $"{page.Annotations.Length} comment{(page.Annotations.Length == 1 ? "" : "s")}"; }));
            sp.Children.Add(label);
            var b = new Button { Style = Ui.S("BaseButton"), Content = sp, Padding = new Thickness(8, 8, 8, 6), Margin = new Thickness(8, 0, 8, 2), ToolTip = $"Page {page.Number}" };
            int n = page.Number;
            b.Click += (_, _) => GoToPage(n);
            System.Windows.Automation.AutomationProperties.SetName(b, $"Page {page.Number}");
            list.Children.Add(b);
        }
        return PanelShell("Pages", Document.PageCountText, list, Ui.ToolButton("Organizing pages", Ic.More, "Page options"));
    }

    private FrameworkElement BuildBookmarks()
    {
        var list = new StackPanel { Margin = new Thickness(6, 0, 6, 16) };
        if (Document.Outline.Length == 0)
        {
            list.Children.Add(Ui.Wrap("This document has no bookmarks.", muted: true, size: 12.5).Also(t => t.Margin = new Thickness(10, 4, 10, 4)));
        }
        foreach (var item in Document.Outline)
        {
            var row = new DockPanel();
            row.Children.Add(Ui.Text(item.Page.ToString(), muted: true, size: 11.5).Also(t => { t.Margin = new Thickness(8, 0, 0, 0); DockPanel.SetDock(t, Dock.Right); }));
            row.Children.Add(Ui.Icon(item.Level == 0 ? Ic.Bookmark : Ic.ChevronRight, item.Level == 0 ? 13 : 10, "Brush.Accent").Also(i => { i.Margin = new Thickness(0, 0, 8, 0); DockPanel.SetDock(i, Dock.Left); }));
            row.Children.Add(Ui.Text(item.Title, size: 13, weight: item.Level == 0 ? FontWeights.SemiBold : FontWeights.Normal));
            var b = new Button
            {
                Style = Ui.S("BaseButton"), Content = row, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(10 + item.Level * 16, 7, 10, 7), MinHeight = 32, ToolTip = $"Go to page {item.Page}",
            };
            int page = item.Page;
            b.Click += (_, _) => GoToPage(page);
            System.Windows.Automation.AutomationProperties.SetName(b, item.Title);
            list.Children.Add(b);
        }
        return PanelShell("Bookmarks", Document.Outline.Length == 0 ? null : $"{Document.Outline.Length} bookmarks", list, Ui.ToolButton("Adding bookmarks", Ic.Add, "New bookmark"));
    }

    // ===== Right panels =====

    private FrameworkElement BuildComments()
    {
        var list = new StackPanel { Margin = new Thickness(14, 0, 14, 16) };
        var all = Document.Annotations.ToList();
        if (all.Count == 0)
        {
            var empty = new StackPanel { Margin = new Thickness(0, 30, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            empty.Children.Add(Ui.Icon(Ic.Comments, 26, "Brush.Accent").Also(i => i.HorizontalAlignment = HorizontalAlignment.Center));
            empty.Children.Add(Ui.Text("No comments yet", size: 14, weight: FontWeights.SemiBold).Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 8, 0, 2); }));
            empty.Children.Add(Ui.Wrap("Highlights and sticky notes on this document will show up here.", muted: true, size: 12.5).Also(t => t.TextAlignment = TextAlignment.Center));
            list.Children.Add(empty);
        }
        foreach (var group in all.GroupBy(a => a.Page.Number))
        {
            list.Children.Add(new TextBlock { Text = $"PAGE {group.Key}", Style = Ui.S("SectionLabel"), Margin = new Thickness(2, 10, 0, 8) });
            foreach (var a in group) list.Children.Add(CommentCard(a));
        }
        var add = Ui.SoonButton("Adding comments", Ic.Add, "Add", "PrimaryButton").Also(b => { b.Padding = new Thickness(12, 5, 12, 5); b.MinHeight = 30; });
        return PanelShell("Comments", all.Count == 1 ? "1 comment" : $"{all.Count} comments", list, add);
    }

    private Border CommentCard(Annotation a)
    {
        var sp = new StackPanel();
        var head = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var kind = Ui.Icon(a.Kind == AnnotationKind.Highlight ? Ic.Highlight : Ic.Note, 14, "Brush.Amber", filled: true);
        kind.ToolTip = a.Kind == AnnotationKind.Highlight ? "Highlight" : "Sticky note";
        DockPanel.SetDock(kind, Dock.Right);
        head.Children.Add(kind);
        head.Children.Add(Avatar(a.Author).Also(x => { x.Margin = new Thickness(0, 0, 10, 0); DockPanel.SetDock(x, Dock.Left); }));
        var who = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        who.Children.Add(Ui.Text(a.Author, size: 13, weight: FontWeights.SemiBold));
        who.Children.Add(Ui.Text(When.Ago(a.Date), muted: true, size: 11.5));
        head.Children.Add(who);
        sp.Children.Add(head);

        var block = a.Page.Blocks[a.Block];
        string quote = block.PlainText;
        if (quote.Length > 110) quote = quote[..107].TrimEnd() + "…";
        if (a.Kind == AnnotationKind.Highlight && quote.Length > 0)
        {
            var q = new Border { BorderThickness = new Thickness(3, 0, 0, 0), Padding = new Thickness(9, 2, 0, 2), Margin = new Thickness(0, 0, 0, 8) };
            q.SetResourceReference(Border.BorderBrushProperty, "Brush.Amber");
            q.Child = Ui.Wrap(quote, muted: true, size: 12.5, lineHeight: 18).Also(t => t.FontStyle = FontStyles.Italic);
            sp.Children.Add(q);
        }
        if (a.Text.Length > 0) sp.Children.Add(Ui.Wrap(a.Text, size: 13, lineHeight: 19));
        else sp.Children.Add(Ui.Text("Highlighted, no comment", muted: true, size: 12.5));
        foreach (var reply in a.Replies)
        {
            int colon = reply.IndexOf(':');
            var r = new StackPanel { Margin = new Thickness(14, 10, 0, 0) };
            r.Children.Add(Ui.Text(colon > 0 ? reply[..colon] : "Reply", size: 12, weight: FontWeights.SemiBold));
            r.Children.Add(Ui.Wrap(colon > 0 ? reply[(colon + 1)..].Trim() : reply, size: 12.5, lineHeight: 18));
            sp.Children.Add(r);
        }
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(-8, 6, 0, -6) };
        actions.Children.Add(new Button { Style = Ui.S("LinkButton"), Content = "Reply", Tag = "Replying to comments" }.Also(b => b.Click += (s, _) => Toaster.Soon(s)));
        actions.Children.Add(new Button { Style = Ui.S("LinkButton"), Content = "Resolve", Tag = "Resolving comments" }.Also(b => b.Click += (s, _) => Toaster.Soon(s)));
        sp.Children.Add(actions);

        var card = new Border { Style = Ui.S("Card"), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 10), Child = sp, Cursor = Cursors.Hand };
        card.BorderThickness = new Thickness(1.5);
        card.SetResourceReference(Border.BorderBrushProperty, a == SelectedAnnotation ? "Brush.Accent" : "Brush.Border");
        card.MouseLeftButtonUp += (_, _) => SelectAnnotation(a);
        System.Windows.Automation.AutomationProperties.SetName(card, $"Comment from {a.Author} on page {a.Page.Number}");
        _commentCards[a] = card;
        return card;
    }

    private static readonly string[] AvatarColors = ["#C42F3D", "#2563EB", "#2F7D4A", "#D97706", "#7C3AED", "#0F766E"];

    internal static Border Avatar(string name, double size = 30)
    {
        var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));
        int hash = 0;
        foreach (char c in name) hash = hash * 31 + c;
        return new Border
        {
            Width = size, Height = size, CornerRadius = new CornerRadius(size / 2),
            Background = Ui.Hex(AvatarColors[Math.Abs(hash % AvatarColors.Length)]),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = initials, Foreground = Brushes.White, FontSize = size * 0.4, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };
    }

    private FrameworkElement BuildProperties()
    {
        var sp = new StackPanel { Margin = new Thickness(16, 0, 16, 16) };
        var d = Document;
        void Section(string title) => sp.Children.Add(new TextBlock { Text = title, Style = Ui.S("SectionLabel"), Margin = new Thickness(0, 12, 0, 6) });
        void Pair(string key, string value)
        {
            var row = new StackPanel { Margin = new Thickness(0, 0, 0, 9) };
            row.Children.Add(Ui.Text(key, muted: true, size: 11.5));
            row.Children.Add(Ui.Wrap(value, size: 13));
            sp.Children.Add(row);
        }
        Section("DESCRIPTION");
        Pair("File name", d.FileName);
        Pair("Title", d.Title);
        Pair("Author", d.Author);
        if (d.Subject.Length > 0) Pair("Subject", d.Subject);
        Pair("Type", d.Kind);
        Section("FILE");
        Pair("Location", $@"This PC\{d.Folder}");
        Pair("Size", d.SizeText);
        Pair("Pages", d.PageCountText);
        Pair("Page size", d.PageSizeText);
        Pair("Created", When.Full(d.Created));
        Pair("Modified", When.Full(d.Modified));
        Section("ADVANCED");
        Pair("PDF version", d.PdfVersion);
        Pair("Made with", d.Producer);
        Pair("Security", d.Security);
        Pair("Comments", d.CommentCount == 0 ? "None" : d.CommentCount.ToString());
        Pair("Fonts", d.IsScan ? "None (picture only)" : $"{d.Font}, embedded");
        sp.Children.Add(Ui.SoonButton("Editing document properties", Ic.Pencil, "Edit properties").Also(b => { b.HorizontalAlignment = HorizontalAlignment.Left; b.Margin = new Thickness(0, 6, 0, 0); }));
        return PanelShell("Document properties", null, sp);
    }

    // ===== Status bar =====

    private FrameworkElement BuildStatusBar()
    {
        var border = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(22, 7, 22, 7) };
        border.SetResourceReference(Border.BackgroundProperty, "Brush.SurfaceAlt");
        border.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        var dock = new DockPanel();
        var right = new StackPanel { Orientation = Orientation.Horizontal };
        right.Children.Add(Ui.Icon(Ic.Lock, 12, "Brush.TextMuted").Also(i => i.Margin = new Thickness(0, 0, 6, 0)));
        right.Children.Add(Ui.Text("Preview: sample document, changes aren't saved", muted: true, size: 12));
        DockPanel.SetDock(right, Dock.Right);
        dock.Children.Add(right);
        dock.Children.Add(_status);
        border.Child = dock;
        return border;
    }

    private void UpdateStatus()
    {
        var d = Document;
        string text = $"Page {CurrentPage} of {PageCount}  ·  {d.PageSizeText}  ·  {d.SizeText}";
        if (d.CommentCount > 0) text += $"  ·  {d.CommentCount} comment{(d.CommentCount == 1 ? "" : "s")}";
        if (Rotation != 0) text += $"  ·  rotated {Rotation}°";
        _status.Text = text;
    }
}
