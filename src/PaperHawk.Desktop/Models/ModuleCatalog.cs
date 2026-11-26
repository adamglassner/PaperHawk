namespace PaperHawk.Models;

/// <summary>
/// Every section of PaperHawk. Home, Viewer, Files and All tools have their own preview pages; everything else uses the
/// generic ComingSoonView built from this data. Add a module here and it appears in the sidebar automatically.
/// </summary>
public static class ModuleCatalog
{
    public static readonly ModuleInfo Home = new()
    {
        Key = "home", Title = "Home", Glyph = Ic.Home, IsComingSoon = false,
        Tagline = "Recent documents, things waiting for you and quick tools.",
    };

    public static readonly ModuleInfo Viewer = new()
    {
        Key = "viewer", Title = "Viewer", Glyph = Ic.Pdf, IsComingSoon = false,
        Tagline = "Read the open PDF with thumbnails, bookmarks, zoom and comments.",
    };

    public static readonly ModuleInfo Files = new()
    {
        Key = "files", Title = "Files", Glyph = Ic.Files, IsComingSoon = false,
        Tagline = "Every PDF you've opened, starred, shared or scanned.",
    };

    public static readonly ModuleInfo Tools = new()
    {
        Key = "tools", Title = "All tools", Glyph = Ic.Tools, IsComingSoon = false,
        Tagline = "Create, edit, convert, sign and protect PDFs.",
    };

    public static readonly IReadOnlyList<ModuleInfo> Sidebar =
    [
        Home,
        Viewer,
        Files,
        Tools,
        new()
        {
            Key = "edit", Title = "Edit PDF", Glyph = Ic.Edit,
            Tagline = "Change text and pictures right on the page.",
            Description = "Click any paragraph to fix a typo or rewrite it, and the text reflows around it in the same font. Move, resize and replace pictures, add links, and add headers, footers, page numbers and watermarks.",
            PrimaryAction = "Edit a PDF",
            Kpis = ["Edited this week", "Text changes", "Pictures replaced"],
            Columns = ["Document", "Last edit", "Edited by", "Pages"],
            Features =
            [
                "Edit text with reflow and font matching",
                "Add, move, crop and replace pictures",
                "Add and edit links",
                "Headers, footers and page numbers",
                "Watermarks and backgrounds",
                "Find and replace across the document",
                "Spell check",
                "Undo history",
            ],
        },
        new()
        {
            Key = "fillsign", Title = "Fill & Sign", Glyph = Ic.Signature,
            Tagline = "Fill in forms and sign documents without printing them.",
            Description = "Type into form fields, tick boxes and add your signature by typing, drawing or using a picture. Save your signature and initials to reuse, and send documents to others to sign, then follow who has signed.",
            PrimaryAction = "Fill in a form",
            Kpis = ["Waiting for you", "Waiting for others", "Signed this month", "Saved signatures"],
            Columns = ["Document", "Sent to", "Status", "Sent"],
            Features =
            [
                "Fill in form fields and check boxes",
                "Type, draw or upload a signature",
                "Save signatures and initials",
                "Add dates, text and check marks anywhere",
                "Request signatures from others",
                "Track who has signed and send reminders",
                "Turn any PDF into a fillable form",
                "Certificate-based digital signatures",
            ],
        },
        new()
        {
            Key = "organize", Title = "Organize pages", Glyph = Ic.Pages,
            Tagline = "Reorder, rotate, delete, insert and split pages.",
            Description = "See every page as a thumbnail and drag them into the order you want. Rotate sideways pages, delete blank ones, insert pages from another PDF or a scan, and split a document into several files.",
            PrimaryAction = "Organize a PDF",
            Kpis = ["Pages", "Rotated", "Deleted", "Inserted"],
            Columns = ["Page", "Size", "Rotation", "Source"],
            Features =
            [
                "Drag pages into a new order",
                "Rotate pages",
                "Delete and extract pages",
                "Insert pages from files, scans or a blank page",
                "Split by page count, size or bookmarks",
                "Replace pages",
                "Crop pages and change the page size",
            ],
        },
        new()
        {
            Key = "combine", Title = "Combine files", Glyph = Ic.Combine,
            Tagline = "Merge PDFs, Office files and pictures into one PDF.",
            Description = "Add PDFs, WriteHawk and Word documents, spreadsheets, slides and pictures, put them in order, and PaperHawk turns them into a single PDF with bookmarks for each file.",
            PrimaryAction = "Add files",
            Kpis = ["Files", "Pages", "Combined size"],
            Columns = ["File", "Type", "Pages", "Size"],
            Features =
            [
                "Combine PDFs and other files",
                "Drag files into order",
                "Pick a page range from each file",
                "Bookmarks for every file",
                "Make a PDF portfolio",
                "Works with Hawk suite files",
            ],
        },
        new()
        {
            Key = "export", Title = "Export PDF", Glyph = Ic.Export,
            Tagline = "Turn PDFs into Word, Excel, PowerPoint, pictures and more.",
            Description = "Convert a PDF into an editable document, spreadsheet or presentation that keeps the layout, or save its pages as pictures. Scanned PDFs are recognized first so their text can be edited.",
            PrimaryAction = "Export a PDF",
            Kpis = ["Exported this month", "To documents", "To spreadsheets", "To pictures"],
            Columns = ["Document", "Exported to", "Date", "Size"],
            Features =
            [
                "Word (.docx) and WriteHawk",
                "Excel (.xlsx) and GridHawk",
                "PowerPoint (.pptx) and SlideHawk",
                "Pictures (PNG, JPEG, TIFF)",
                "Plain text, RTF and HTML",
                "Export all pictures in a PDF",
                "Text recognition for scans",
            ],
        },
        new()
        {
            Key = "create", Title = "Create PDF", Glyph = Ic.CreatePdf,
            Tagline = "Make a PDF from any file, a scan, a web page or the clipboard.",
            Description = "Drop in a document, a spreadsheet, a picture or a web page and save it as a PDF. Print to the PaperHawk printer from any app, or start from a blank page.",
            PrimaryAction = "Create a PDF",
            Kpis = ["Created this month", "From files", "From scans"],
            Columns = ["Source", "Type", "Created", "Pages"],
            Features =
            [
                "From Office and Hawk suite files",
                "From pictures and screenshots",
                "From a scanner or a phone camera",
                "From a web page",
                "From the clipboard",
                "PaperHawk printer for any app",
                "Blank PDF",
            ],
        },
        new()
        {
            Key = "protect", Title = "Protect", Glyph = Ic.Protect,
            Tagline = "Passwords, permissions and redaction.",
            Description = "Add a password to open a PDF, or limit printing, copying and editing. Black out private details for good with redaction, and remove hidden information like metadata and comments before you share.",
            PrimaryAction = "Protect a PDF",
            Kpis = ["Protected", "Redacted", "Passwords"],
            Columns = ["Document", "Protection", "Date"],
            Features =
            [
                "Open passwords with AES-256 encryption",
                "Permission passwords (printing, copying, editing)",
                "Redact text, pictures and whole pages",
                "Find and redact patterns like phone numbers",
                "Remove hidden information and metadata",
                "Certificate encryption",
            ],
        },
        new()
        {
            Key = "scan", Title = "Scan & OCR", Glyph = Ic.Scan,
            Tagline = "Scan paper and make scanned text searchable.",
            Description = "Scan straight into a PDF and let PaperHawk recognize the text (OCR), so scans can be searched, copied, read aloud and edited. Straighten crooked pages and clean up the background automatically.",
            PrimaryAction = "Scan a document",
            Kpis = ["Scans", "Recognized", "Pages"],
            Columns = ["Scan", "Pages", "Text recognized", "Date"],
            Features =
            [
                "Scan from any Windows scanner",
                "Recognize text in many languages",
                "Straighten and clean up pages",
                "Searchable image or editable text",
                "Recognize a whole folder of scans",
                "Compress scans to a smaller size",
            ],
        },
    ];

    public static readonly ModuleInfo Settings = new()
    {
        Key = "settings", Title = "Settings", Glyph = Ic.Settings,
        Tagline = "Appearance, page display, default app and accessibility.",
        Description = "Choose light or dark mode, the default zoom and page layout, whether documents open where you left off, and make PaperHawk the default app for PDF files in Windows.",
        PrimaryAction = "Save",
        Columns = ["Setting", "Value"],
        Features =
        [
            "Light, dark or match Windows",
            "Default zoom and page layout",
            "Reopen documents where you left off",
            "Make PaperHawk the default PDF app",
            "Your name for comments",
            "Smooth text and pictures",
            "Read aloud voice and speed",
            "Keyboard shortcuts",
        ],
    };

    public static IEnumerable<ModuleInfo> All => Sidebar.Append(Settings);

    public static ModuleInfo? Find(string key) => All.FirstOrDefault(m => m.Key == key);
}
