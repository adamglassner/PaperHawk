using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PaperHawk.Models;
using PaperHawk.Views;

namespace PaperHawk.Services;

/// <summary>
/// <c>PaperHawk.exe --screenshots &lt;folder&gt;</c>: captures every page in light and dark mode as PNGs, then exits.
/// Used by CI to publish screenshots.
/// </summary>
public static class Screenshots
{
    public static string? OutputFolder { get; set; }
    public static bool IsRunning => OutputFolder != null;

    private const double Width = 1440, Height = 920, Scale = 1.25;
    private static int _failures;

    public static async Task<int> RunAsync(MainWindow window)
    {
        Directory.CreateDirectory(OutputFolder!);
        window.WindowState = WindowState.Normal;
        window.Left = 0; window.Top = 0;
        window.Width = Width; window.Height = Height;
        await SelfTest.Idle();

        int n = 1;
        foreach (var mode in new[] { AppTheme.Light, AppTheme.Dark })
        {
            App.ApplyTheme(mode);
            foreach (var module in ModuleCatalog.All)
            {
                window.GoTo(module.Key, force: true);
                await Capture(window, $"{n++:00}-{module.Key}-{mode.ToLowerInvariant()}");
            }
            string m = mode.ToLowerInvariant();
            window.OpenComment(SampleDocs.AnnualReport.Pages[2].Annotations[0]);
            await Capture(window, $"{n++:00}-viewer-report-comments-{m}");
            window.OpenDocument(SampleDocs.Lease, 4);
            await Capture(window, $"{n++:00}-viewer-lease-sign-{m}");
            window.OpenDocument(SampleDocs.VolunteerForm, 1, RightPanel.Properties);
            await Capture(window, $"{n++:00}-viewer-form-properties-{m}");
            window.OpenDocument(SampleDocs.QuickStart, 1);
            if (window.CurrentPage is ViewerView v)
            {
                await SelfTest.Idle();
                v.ShowLeftPanel(LeftPanel.Bookmarks);
                v.SetLayout(PageLayout.TwoPage);
            }
            await Capture(window, $"{n++:00}-viewer-guide-two-page-{m}");
            window.OpenDocument(SampleDocs.Receipt);
            await Capture(window, $"{n++:00}-viewer-scan-{m}");
        }
        return _failures;
    }

    private static async Task Capture(Window window, string name)
    {
        try
        {
            await SelfTest.Idle();
            await Task.Delay(150);
            await SelfTest.Idle();
            var root = (FrameworkElement)window.Content;
            var bmp = new RenderTargetBitmap((int)(root.ActualWidth * Scale), (int)(root.ActualHeight * Scale), 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
            bmp.Render(root);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bmp));
            using var file = File.Create(Path.Combine(OutputFolder!, name + ".png"));
            png.Save(file);
        }
        catch (Exception ex)
        {
            _failures++;
            File.WriteAllText(Path.Combine(OutputFolder!, name + ".error.txt"), ex.ToString());
        }
    }
}
