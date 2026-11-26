using System.IO;
using System.Windows;
using System.Windows.Threading;
using PaperHawk.Models;
using PaperHawk.Views;

namespace PaperHawk.Services;

/// <summary>
/// <c>PaperHawk.exe --self-test &lt;file&gt;</c>: opens every page in light and dark mode, every sample document and
/// every page of it, every zoom, layout, rotation and panel, writes PASS/FAIL lines to the file and exits with the
/// number of failures. Used by CI.
/// </summary>
public static class SelfTest
{
    public static string? OutputFile { get; set; }
    public static bool IsRunning => OutputFile != null;

    private static readonly List<string> Lines = [];
    private static int _failures;

    public static void RecordUnhandled(Exception ex) => Fail("unhandled", ex);

    private static void Pass(string what) => Lines.Add($"PASS  {what}");

    private static void Fail(string what, Exception ex)
    {
        _failures++;
        Lines.Add($"FAIL  {what}: {ex.GetType().Name}: {ex.Message}");
    }

    private static async Task Check(string what, Func<Task> action)
    {
        try { await action(); Pass(what); }
        catch (Exception ex) { Fail(what, ex); }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static Task Idle() => Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;

    private static async Task<ViewerView> Viewer(MainWindow window, PdfDocument doc, int? page = null, RightPanel? panel = null)
    {
        window.OpenDocument(doc, page, panel);
        await Idle();
        var view = window.CurrentPage as ViewerView ?? throw new InvalidOperationException("viewer not shown");
        Expect(view.Document == doc, "wrong document");
        return view;
    }

    public static async Task<int> RunAsync(MainWindow window)
    {
        Lines.Add($"PaperHawk {App.VersionText} self-test, {DateTime.Now:yyyy-MM-dd h:mm tt}");
        await Check("sample documents are well formed", () =>
        {
            foreach (var d in SampleDocs.All)
            {
                Expect(d.Pages.Length > 0, $"{d.FileName} has no pages");
                for (int i = 0; i < d.Pages.Length; i++)
                {
                    Expect(d.Pages[i].Number == i + 1, $"{d.FileName} page {i + 1} is numbered {d.Pages[i].Number}");
                    Expect(d.Pages[i].Document == d, $"{d.FileName} page {i + 1} isn't linked to its document");
                    foreach (var a in d.Pages[i].Annotations)
                        Expect(a.Block >= 0 && a.Block < d.Pages[i].Blocks.Length && a.Page == d.Pages[i], $"{d.FileName} page {i + 1}: comment from {a.Author} points at a missing block");
                }
                foreach (var o in d.Outline) Expect(o.Page >= 1 && o.Page <= d.Pages.Length, $"{d.FileName}: bookmark {o.Title} points at page {o.Page}");
                Expect(d.LastPage >= 1 && d.LastPage <= d.Pages.Length, $"{d.FileName}: last page {d.LastPage} is out of range");
            }
            return Task.CompletedTask;
        });

        foreach (var mode in new[] { AppTheme.Light, AppTheme.Dark })
        {
            App.ApplyTheme(mode);
            foreach (var module in ModuleCatalog.All)
            {
                await Check($"{mode}: {module.Title}", async () =>
                {
                    window.GoTo(module.Key, force: true);
                    await Idle();
                    Expect(window.CurrentPage != null, "no page shown");
                });
            }

            foreach (var doc in SampleDocs.All)
            {
                await Check($"{mode}: read {doc.FileName}", async () =>
                {
                    var view = await Viewer(window, doc, 1);
                    Expect(view.ThumbnailCount == doc.Pages.Length, $"{view.ThumbnailCount} thumbnails for {doc.Pages.Length} pages");
                    for (int p = 1; p <= doc.Pages.Length; p++)
                    {
                        view.GoToPage(p);
                        await Idle();
                        Expect(view.CurrentPage == p, $"went to page {p} but the viewer says {view.CurrentPage}");
                    }
                    view.GoToPage(1);
                    for (int p = 2; p <= doc.Pages.Length; p++) { view.NextPage(); Expect(view.CurrentPage == p, $"Next page from {p - 1} landed on {view.CurrentPage}"); }
                    view.NextPage();
                    Expect(view.CurrentPage == doc.Pages.Length, "Next page went past the end");
                    view.GoToPage(99);
                    Expect(view.CurrentPage == doc.Pages.Length, "page 99 wasn't clamped to the last page");
                    view.GoToPage(-3);
                    Expect(view.CurrentPage == 1, "page -3 wasn't clamped to page 1");
                });

                await Check($"{mode}: zoom, layout and rotation on {doc.FileName}", async () =>
                {
                    var view = await Viewer(window, doc);
                    Expect(view.ZoomMode == ZoomMode.FitWidth, "doesn't open at fit width");
                    foreach (var z in ViewerView.ZoomSteps)
                    {
                        view.SetZoom(z);
                        Expect(Math.Abs(view.Zoom - z) < 0.001 && view.ZoomMode == ZoomMode.Custom, $"zoom {z * 100}% not applied");
                    }
                    view.SetZoom(1);
                    view.ZoomIn();
                    Expect(Math.Abs(view.Zoom - 1.25) < 0.001, $"zoom in from 100% gave {view.Zoom * 100:0}%");
                    view.ZoomOut();
                    view.ZoomOut();
                    Expect(Math.Abs(view.Zoom - 0.75) < 0.001, $"zoom out twice from 125% gave {view.Zoom * 100:0}%");
                    view.SetZoomMode(ZoomMode.FitPage);
                    await Idle();
                    double fitPage = view.Zoom;
                    view.SetZoomMode(ZoomMode.FitWidth);
                    await Idle();
                    Expect(view.Zoom >= fitPage - 0.001, "fit width is smaller than fit page");
                    view.SetLayout(PageLayout.TwoPage);
                    await Idle();
                    if (doc.Pages.Length > 2)
                    {
                        view.GoToPage(1);
                        view.NextPage();
                        Expect(view.CurrentPage == 3, $"two-page Next went to {view.CurrentPage}");
                        view.PreviousPage();
                        Expect(view.CurrentPage == 1, $"two-page Previous went to {view.CurrentPage}");
                    }
                    view.SetLayout(PageLayout.Single);
                    for (int i = 0; i < 4; i++) { view.Rotate(90); await Idle(); }
                    Expect(view.Rotation == 0, $"four quarter turns left the view at {view.Rotation}°");
                    view.Rotate(-90);
                    Expect(view.Rotation == 270, $"rotating left gave {view.Rotation}°");
                    view.Rotate(90);
                    view.SetTool(ViewerTool.Hand);
                    view.SetTool(ViewerTool.Select);
                });

                await Check($"{mode}: panels and comments on {doc.FileName}", async () =>
                {
                    var view = await Viewer(window, doc);
                    foreach (var left in Enum.GetValues<LeftPanel>()) { view.ShowLeftPanel(left); await Idle(); Expect(view.LeftPanel == left, $"left panel {left} not shown"); }
                    view.ShowLeftPanel(LeftPanel.Thumbnails);
                    foreach (var right in Enum.GetValues<RightPanel>()) { view.ShowRightPanel(right); await Idle(); Expect(view.RightPanel == right, $"right panel {right} not shown"); }
                    view.ShowRightPanel(RightPanel.Comments);
                    Expect(view.CommentCardCount == doc.CommentCount, $"{view.CommentCardCount} comment cards for {doc.CommentCount} comments");
                    foreach (var a in doc.Annotations)
                    {
                        view.SelectAnnotation(a);
                        await Idle();
                        Expect(view.SelectedAnnotation == a && view.CurrentPage == a.Page.Number, $"selecting {a.Author}'s comment didn't go to page {a.Page.Number}");
                    }
                    view.SelectAnnotation(null);
                    view.ToggleRightPanel(RightPanel.Comments);
                    Expect(view.RightPanel == RightPanel.None, "comments panel didn't close");
                });
            }

            await Check($"{mode}: open a comment from Home", async () =>
            {
                var a = SampleDocs.AnnualReport.Pages[2].Annotations[0];
                window.OpenComment(a);
                await Idle();
                var view = window.CurrentPage as ViewerView ?? throw new InvalidOperationException("viewer not shown");
                Expect(view.SelectedAnnotation == a && view.RightPanel == RightPanel.Comments && view.CurrentPage == 3, "comment not selected on page 3");
            });

            await Check($"{mode}: Files filters", async () =>
            {
                window.GoTo("files", force: true);
                await Idle();
                var view = window.CurrentPage as FilesView ?? throw new InvalidOperationException("Files page not shown");
                foreach (var f in Enum.GetValues<FileFilter>())
                {
                    view.SetFilter(f);
                    int expected = SampleDocs.All.Count(d => FilesView.Matches(d, f));
                    Expect(view.RowCount == expected, $"filter {FilesView.Label(f)}: {view.RowCount} rows, expected {expected}");
                }
                view.SetFilter(FileFilter.All);
                Expect(view.RowCount == SampleDocs.All.Length, "All files doesn't list every document");
            });

            await Check($"{mode}: every tool on All tools", async () =>
            {
                foreach (var tool in ToolsView.Groups.SelectMany(g => g.Tools))
                {
                    window.GoTo("tools", force: true);
                    await Idle();
                    ToolsView.Open(tool);
                    await Idle();
                    if (tool.Module == "comments") Expect(window.CurrentPage is ViewerView { RightPanel: RightPanel.Comments }, "Comment didn't open the comments panel");
                    else if (tool.Module != null) Expect(window.CurrentPage is ComingSoonView cs && cs.Module.Key == tool.Module, $"{tool.Name} didn't open {tool.Module}");
                }
            });

            await Check($"{mode}: toast", async () => { Toaster.Soon("Self-test"); await Idle(); });
        }
        Lines.Add(_failures == 0 ? "ALL PASSED" : $"{_failures} FAILED");
        try { File.WriteAllLines(OutputFile!, Lines); } catch { /* nothing else we can do */ }
        return _failures;
    }
}
