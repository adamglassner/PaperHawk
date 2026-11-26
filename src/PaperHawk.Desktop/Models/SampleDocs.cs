using static PaperHawk.Models.Block;

namespace PaperHawk.Models;

/// <summary>
/// The sample PDFs the preview opens. They're described as blocks (headings, paragraphs, tables, form fields…) and drawn
/// by the viewer, so the shell works without a PDF engine. Opening real PDF files comes with the PDF engine (coming soon).
/// Every person and company in them is made up.
/// </summary>
public static class SampleDocs
{
    private static DateTime Ago(double days, int hour = 10, int minute = 0) => DateTime.Today.AddDays(-days).AddHours(hour).AddMinutes(minute);

    public static readonly PdfDocument AnnualReport = new PdfDocument
    {
        FileName = "Bytehawk-Year-in-Review-2026.pdf",
        Title = "Bytehawk Studio: 2026 Year in Review",
        Kind = "Report",
        Author = "Bytehawk Studio",
        Subject = "What we shipped this year and what comes next",
        Color = "#C42F3D",
        SizeKb = 2460,
        Created = Ago(12, 9, 14),
        Modified = Ago(1, 16, 42),
        LastOpened = DateTime.Today.AddHours(8).AddMinutes(51),
        LastPage = 3,
        Starred = true,
        Shared = true,
        Outline =
        [
            new("Welcome", 1), new("The year in numbers", 2), new("Apps we shipped", 3), new("The Hawk suite", 3, 1),
            new("Everyday apps", 4, 1), new("What people asked for", 5), new("Looking ahead", 6),
        ],
        Pages =
        [
            new PdfPage
            {
                Number = 1,
                Blocks =
                [
                    Space(150),
                    Title("2026 Year in Review"),
                    Subtitle("Bytehawk Studio  ·  Free software for home and work"),
                    Space(30),
                    Figure("Cover: the Bytehawk Studio Launcher with this year's apps", 300),
                    Space(26),
                    P("This was the year Bytehawk Studio grew from a handful of utilities into a family of apps: an office suite in the making, tools for the home, and a launcher that installs and updates all of them from one place. Thank you to everyone who downloaded, tested and sent ideas."),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Note, Author = "Jordan Lee", Block = 4, Date = Ago(2, 14, 5), Text = "Can we swap this for the new dark-mode screenshot of the launcher?", Replies = ["Sam Rivera: Yes, I'll export it tomorrow."] },
                ],
            },
            new PdfPage
            {
                Number = 2,
                Blocks =
                [
                    H("The year in numbers"),
                    P("Every app is free, and most of them now update themselves through the Bytehawk Studio Launcher. Here is how the year added up."),
                    Stats("19|apps in the launcher", "6|Hawk suite previews", "1.8|launcher version", "0|ads or trackers"),
                    Space(8),
                    Chart("Apps added to the launcher, by quarter", "Q1|2", "Q2|4", "Q3|5", "Q4|8"),
                    H2("Releases by type"),
                    Table(["Type", "Apps", "Examples"],
                          ["Office and creative suite", "6", "NoteHawk, PlanHawk, RingHawk, PaperHawk"],
                          ["Everyday apps", "9", "Taskflare, Sumthing, WillowWallet, Shutterbox"],
                          ["Servers", "2", "Guardrail Server, WillowWallet Server"],
                          ["Browser extensions", "1", "AlbumGrab"]),
                    P("Most of the growth came in the second half of the year, after the launcher learned to install apps from their portable zips and keep them up to date."),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Highlight, Author = "Sam Rivera", Block = 4, Date = Ago(3, 11, 20), Text = "Q4 isn't over yet, so let's say \"so far\" in the caption." },
                    new Annotation { Kind = AnnotationKind.Highlight, Author = "Jordan Lee", Block = 7, Date = Ago(1, 9, 45), Text = "" },
                ],
            },
            new PdfPage
            {
                Number = 3,
                Blocks =
                [
                    H("Apps we shipped"),
                    P("Every Bytehawk app shares the Hawkline look: a dark sidebar on the left, cards on the right, and light and dark modes that follow Windows."),
                    H2("The Hawk suite"),
                    P("The Hawk suite is our answer to the big office and creative packages. Each app starts as a shell you can click through, then grows feature by feature."),
                    Table(["App", "Does the job of", "Status"],
                          ["NoteHawk", "OneNote, Evernote", "Preview"],
                          ["PlanHawk", "Microsoft Project", "Preview"],
                          ["RingHawk", "Skype", "Preview"],
                          ["PaperHawk", "Adobe Acrobat", "Preview"],
                          ["WriteHawk", "Microsoft Word", "Coming soon"],
                          ["GridHawk", "Microsoft Excel", "Coming soon"],
                          ["SlideHawk", "Microsoft PowerPoint", "Coming soon"]),
                    Callout("Shells first: each app ships as a clickable preview so the design can be tried and tuned before the heavy features land."),
                    P("PaperHawk joins the suite as the PDF reader and editor. The preview opens sample documents with thumbnails, bookmarks, zoom, page layouts and comments; editing, signing and the other tools follow."),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Highlight, Author = "Jordan Lee", Block = 5, Date = Ago(1, 10, 2), Text = "Love this line. Let's use it on the website too." },
                    new Annotation { Kind = AnnotationKind.Note, Author = "Sam Rivera", Block = 4, Date = Ago(0, 7, 58), Text = "Add DataHawk once it has a name for its first preview." },
                ],
            },
            new PdfPage
            {
                Number = 4,
                Blocks =
                [
                    H2("Everyday apps"),
                    P("Alongside the suite, the everyday apps cover the small jobs: a calculator, a to-do list with an AI assistant connector, kanban boards, budgeting with phone sync, photo organizing, family backups and a media player for your own movies and shows."),
                    Bullets("Taskflare: to-do lists that work with AI assistants",
                            "Boardwalk: kanban boards, one per project",
                            "WillowWallet: budgeting on the desktop and the phone",
                            "Shutterbox: photo organizing with albums and people",
                            "Sparecopy: backups to a NAS, a server or a USB drive",
                            "Castaway: your own movies and shows, streamed from your own server"),
                    Figure("Taskflare, Boardwalk and WillowWallet side by side", 240),
                    P("Each of them installs in one click from the launcher, and none of them needs an account to get started."),
                ],
            },
            new PdfPage
            {
                Number = 5,
                Blocks =
                [
                    H("What people asked for"),
                    P("Feedback this year came through GitHub issues, email and conversations with family and friends who use the apps every day. The most requested features, in order:"),
                    Numbered("Open and edit Microsoft Office files in the Hawk suite",
                             "Sync between the desktop apps and phones",
                             "macOS and Linux versions of more apps",
                             "Fill in and sign PDF forms without printing them",
                             "One sign-in for every Bytehawk server"),
                    Chart("Share of requests, by topic", "Office files|34", "Phone sync|22", "Mac and Linux|18", "PDF forms|15", "Single sign-in|11"),
                    P("Filling in and signing PDFs comes up again and again, which is why Fill & Sign is near the top of PaperHawk's list."),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Highlight, Author = "Sam Rivera", Block = 4, Date = Ago(0, 8, 30), Text = "Worth calling out that this one is already in progress." },
                ],
            },
            new PdfPage
            {
                Number = 6,
                Blocks =
                [
                    H("Looking ahead"),
                    P("Next year is about depth: turning the Hawk suite previews into apps people can use for real work, bringing more apps to macOS and Linux, and connecting the apps to each other."),
                    Bullets("WriteHawk, GridHawk and SlideHawk previews",
                            "PaperHawk: open your own PDFs, comment, fill in and sign",
                            "PlanHawk: editing tasks and opening .mpp files",
                            "Phone apps for WillowWallet and Taskflare"),
                    Callout("Everything stays free. No ads, no trackers, and the source is on GitHub."),
                    Space(40),
                    Divider(),
                    P("Bytehawk Studio  ·  github.com/adamglassner"),
                ],
            },
        ],
    }.Link();

    public static readonly PdfDocument Lease = new PdfDocument
    {
        FileName = "Lease-Agreement-412-Maple-Lane.pdf",
        Title = "Residential Lease Agreement",
        Kind = "Agreement",
        Author = "Harbor Point Property Co.",
        Subject = "12-month lease, 412 Maple Lane, Unit 2",
        Color = "#1F4E79",
        Font = "Cambria",
        SizeKb = 384,
        Created = Ago(5, 15, 10),
        Modified = Ago(5, 15, 12),
        LastOpened = Ago(1, 19, 24),
        LastPage = 2,
        Security = "Signatures requested: 2 of 3 still to sign",
        Outline = [new("Parties and property", 1), new("Term and rent", 1), new("Deposit", 2), new("Rules of the home", 2), new("Signatures", 4)],
        Pages =
        [
            new PdfPage
            {
                Number = 1,
                Blocks =
                [
                    Title("Residential Lease Agreement"),
                    Subtitle("412 Maple Lane, Unit 2  ·  Prepared by Harbor Point Property Co."),
                    Divider(),
                    H2("1. Parties and property"),
                    P("This agreement is between Harbor Point Property Co. (\"Landlord\") and Casey Morgan and Riley Morgan (\"Tenants\"). The Landlord rents to the Tenants the home at 412 Maple Lane, Unit 2, including one parking space and the storage closet in the basement."),
                    H2("2. Term"),
                    P("The lease starts on the first day of next month and runs for twelve months. After that it continues month to month until either side gives 60 days' written notice."),
                    H2("3. Rent"),
                    KeyValues("Monthly rent|$1,450.00", "Due|1st of each month", "Grace period|5 days", "Late fee|$50.00", "Paid to|Harbor Point Property Co."),
                    P("Rent may be paid by bank transfer, check or through the Landlord's online portal. A receipt is provided for every payment."),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Highlight, Author = "Casey Morgan", Block = 6, Date = Ago(1, 19, 40), Text = "Does month to month start automatically, or do we have to ask?" },
                ],
            },
            new PdfPage
            {
                Number = 2,
                Blocks =
                [
                    H2("4. Security deposit"),
                    P("The Tenants pay a security deposit of $1,450.00 before moving in. The Landlord keeps it in a separate account and returns it within 30 days after the Tenants move out, minus the cost of any damage beyond normal wear and tear, with a written list of deductions."),
                    H2("5. Utilities"),
                    Table(["Utility", "Paid by"], ["Water and sewer", "Landlord"], ["Trash and recycling", "Landlord"], ["Electricity", "Tenants"], ["Gas heat", "Tenants"], ["Internet", "Tenants"]),
                    H2("6. Rules of the home"),
                    Bullets("No smoking anywhere inside the home or on the porch.",
                            "Pets are allowed with written approval; one cat is approved.",
                            "Quiet hours are 10:00 PM to 7:00 AM.",
                            "Tell the Landlord about any leak or broken appliance within 48 hours."),
                    H2("7. Repairs"),
                    P("The Landlord handles repairs to the building, plumbing, heating and the appliances that come with the home. The Tenants keep the home clean and replace light bulbs and smoke-alarm batteries."),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Note, Author = "Riley Morgan", Block = 5, Date = Ago(1, 20, 2), Text = "Remember to send the photo of Juniper for the pet approval." },
                ],
            },
            new PdfPage
            {
                Number = 3,
                Blocks =
                [
                    H2("8. Entry"),
                    P("The Landlord may enter the home to make repairs or show it to future tenants after giving at least 24 hours' notice, except in an emergency."),
                    H2("9. Ending the lease early"),
                    P("If the Tenants need to leave before the end of the term, they give 60 days' written notice and pay rent until a new tenant moves in or the notice period ends, whichever comes first."),
                    H2("10. Moving out"),
                    Checks("[x] Return all keys and the garage remote", "[x] Leave a forwarding address", "[ ] Book a walk-through with the Landlord", "[ ] Clean the oven and refrigerator"),
                    H2("11. Whole agreement"),
                    P("This document is the whole agreement between the Landlord and the Tenants. Any change must be in writing and signed by everyone below."),
                ],
            },
            new PdfPage
            {
                Number = 4,
                Blocks =
                [
                    H2("12. Signatures"),
                    P("By signing below, everyone agrees to the terms of this lease."),
                    Space(18),
                    Sign("Landlord: Harbor Point Property Co.", "Dana Whitfield"),
                    Field("Date", DateTime.Today.AddDays(-5).ToString("MMMM d, yyyy")),
                    Space(18),
                    Sign("Tenant: Casey Morgan"),
                    Field("Date"),
                    Space(18),
                    Sign("Tenant: Riley Morgan"),
                    Field("Date"),
                ],
            },
        ],
    }.Link();

    public static readonly PdfDocument VolunteerForm = new PdfDocument
    {
        FileName = "Community-Garden-Volunteer-Form.pdf",
        Title = "Riverside Community Garden: Volunteer Sign-up",
        Kind = "Form",
        Author = "Riverside Community Garden",
        Color = "#2F7D4A",
        SizeKb = 212,
        Created = Ago(20, 13, 0),
        Modified = Ago(3, 18, 20),
        LastOpened = Ago(3, 18, 25),
        Outline = [new("About you", 1), new("When you can help", 1), new("Jobs you'd like", 2), new("Signature", 2)],
        Pages =
        [
            new PdfPage
            {
                Number = 1,
                Blocks =
                [
                    Title("Volunteer Sign-up"),
                    Subtitle("Riverside Community Garden  ·  Spring season"),
                    Callout("Fill in this form and bring it to the garden shed, or email it back. Fields with a blue background can be filled in right here once Fill & Sign arrives in PaperHawk."),
                    H2("About you"),
                    Field("Full name", "Alex Rivera"),
                    Field("Email", "alex.rivera@example.com"),
                    Field("Phone"),
                    Field("Street address"),
                    H2("When you can help"),
                    Checks("[x] Saturday mornings", "[ ] Saturday afternoons", "[x] Sunday mornings", "[ ] Weekday evenings"),
                    Field("Hours per month (about)", "6"),
                ],
            },
            new PdfPage
            {
                Number = 2,
                Blocks =
                [
                    H2("Jobs you'd like"),
                    Checks("[x] Watering and weeding", "[ ] Building raised beds", "[x] Running the seed swap", "[ ] Teaching a kids' workshop", "[ ] Social media and photos"),
                    H2("Anything we should know?"),
                    Field("Allergies, skills, or other notes"),
                    H2("Safety"),
                    P("Volunteers wear closed-toe shoes, use gloves with tools, and sign in at the shed. Children under 14 come with an adult."),
                    Space(16),
                    Sign("Volunteer"),
                    Field("Date"),
                ],
            },
        ],
    }.Link();

    public static readonly PdfDocument QuickStart = new PdfDocument
    {
        FileName = "Guardrail-EHS-Quick-Start.pdf",
        Title = "Guardrail EHS Quick Start Guide",
        Kind = "Guide",
        Author = "Bytehawk Studio",
        Subject = "Set up Guardrail EHS in 20 minutes",
        Color = "#D97706",
        SizeKb = 1180,
        Created = Ago(40, 10, 0),
        Modified = Ago(9, 14, 30),
        LastOpened = Ago(6, 12, 10),
        LastPage = 2,
        Outline = [new("Welcome", 1), new("Install", 2), new("Add your sites", 3), new("Report an incident", 4), new("Next steps", 5)],
        Pages =
        [
            new PdfPage
            {
                Number = 1,
                Blocks =
                [
                    Space(120),
                    Title("Guardrail EHS"),
                    Subtitle("Quick Start Guide  ·  Free, open-source safety management"),
                    Space(24),
                    Figure("Guardrail EHS dashboard with incidents, inspections and training", 330),
                    Space(16),
                    P("Guardrail EHS keeps incidents, inspections, training records and fleet safety in one place. This guide walks through installing it, adding your sites and reporting your first incident."),
                ],
            },
            new PdfPage
            {
                Number = 2,
                Blocks =
                [
                    H("1. Install"),
                    Numbered("Open the Bytehawk Studio Launcher.",
                             "Find Guardrail EHS under Bytehawk Apps and click Install.",
                             "To share data with a team, install Guardrail Server from the Bytehawk Servers page.",
                             "Start Guardrail EHS and sign in with the account the server gave you, or work on your own."),
                    Callout("Working alone? Skip the server. Guardrail EHS keeps everything on your computer until you connect one."),
                    Figure("The Guardrail EHS card in the Bytehawk Studio Launcher", 220),
                    H2("System requirements"),
                    Table(["", "Minimum", "Recommended"], ["Windows", "10 (64-bit)", "11"], ["Memory", "4 GB", "8 GB"], ["Disk space", "300 MB", "1 GB"]),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Highlight, Author = "Morgan Ellis", Block = 1, Date = Ago(6, 12, 22), Text = "Step 3 needs a screenshot of the Servers page." },
                ],
            },
            new PdfPage
            {
                Number = 3,
                Blocks =
                [
                    H("2. Add your sites"),
                    P("Sites are the places your people work: an office, a warehouse, a job site or a vehicle yard. Everything in Guardrail EHS belongs to a site, so add them first."),
                    Numbered("Go to Settings, then Sites.", "Click Add site and enter its name and address.", "Choose who manages it and who gets alerts.", "Repeat for each location."),
                    Figure("Adding a site", 240),
                    H2("Tips"),
                    Bullets("Use the names people already say out loud, like \"North warehouse\".", "Vehicles can be grouped under a yard site.", "Archive a site instead of deleting it to keep its history."),
                ],
            },
            new PdfPage
            {
                Number = 4,
                Blocks =
                [
                    H("3. Report an incident"),
                    P("Anyone can report an incident in under a minute. Supervisors then add the investigation, causes and corrective actions."),
                    Numbered("Click Report incident on the dashboard.", "Pick the site, the date and time, and what happened.", "Add photos and the people involved.", "Submit. The site manager gets an alert right away."),
                    Stats("1 min|to report", "24 h|to review", "7 days|to close out"),
                    Callout("Near misses count too. Reporting them is how serious incidents are prevented."),
                ],
            },
            new PdfPage
            {
                Number = 5,
                Blocks =
                [
                    H("Next steps"),
                    Bullets("Schedule your first inspection checklist.", "Import training records from a spreadsheet.", "Set up fleet safety for drivers and vehicles.", "Invite your team to Guardrail Server."),
                    Space(20),
                    Divider(),
                    P("Questions? Open an issue on GitHub at adamglassner/guardrailehs."),
                ],
            },
        ],
    }.Link();

    public static readonly PdfDocument Invoice = new PdfDocument
    {
        FileName = "Invoice-INV-1042.pdf",
        Title = "Invoice INV-1042",
        Kind = "Invoice",
        Author = "Lakeside Print & Sign",
        Color = "#0F766E",
        SizeKb = 96,
        Created = Ago(8, 11, 5),
        Modified = Ago(8, 11, 5),
        LastOpened = Ago(4, 9, 15),
        Starred = true,
        Pages =
        [
            new PdfPage
            {
                Number = 1,
                Blocks =
                [
                    Title("Invoice"),
                    Subtitle("Lakeside Print & Sign  ·  88 Harbor Road  ·  billing@lakeside-print.example"),
                    Space(8),
                    KeyValues("Invoice number|INV-1042", "Date|" + DateTime.Today.AddDays(-8).ToString("MMMM d, yyyy"), "Due|" + DateTime.Today.AddDays(22).ToString("MMMM d, yyyy"), "Bill to|Riverside Community Garden"),
                    Space(10),
                    Table(["Item", "Qty", "Price", "Amount"],
                          ["Garden sign, 24 × 36 in, outdoor vinyl", "2", "$64.00", "$128.00"],
                          ["Volunteer sign-up flyers, color", "200", "$0.35", "$70.00"],
                          ["Seed swap posters, 11 × 17 in", "12", "$4.50", "$54.00"],
                          ["Design time", "1.5 h", "$40.00", "$60.00"]),
                    KeyValues("Subtotal|$312.00", "Community discount (10%)|−$31.20", "Sales tax|$16.85", "Total due|$297.65"),
                    Space(10),
                    Callout("Thank you! Pay by check to Lakeside Print & Sign or by bank transfer using the invoice number as the reference."),
                ],
                Annotations =
                [
                    new Annotation { Kind = AnnotationKind.Note, Author = "Alex Rivera", Block = 6, Date = Ago(4, 9, 20), Text = "Paid by check on the 18th. Keep for the garden's records." },
                ],
            },
        ],
    }.Link();

    public static readonly PdfDocument Receipt = new PdfDocument
    {
        FileName = "Scan-Hardware-Receipt.pdf",
        Title = "Scanned receipt",
        Kind = "Scan",
        Author = "Scanner",
        Folder = "Scans",
        Color = "#6B7280",
        Font = "Consolas",
        SizeKb = 1620,
        Created = Ago(2, 17, 48),
        Modified = Ago(2, 17, 48),
        LastOpened = Ago(2, 17, 50),
        IsScan = true,
        Producer = "Scanner (image only, no text layer)",
        Pages =
        [
            new PdfPage
            {
                Number = 1,
                Blocks =
                [
                    Space(60),
                    Mono("        MAPLE STREET HARDWARE",
                         "          1200 Maple Street",
                         "",
                         "  Raised bed corner brackets  4   $31.96",
                         "  Cedar board 2x8x8          6   $89.94",
                         "  Deck screws 1 lb           1    $9.49",
                         "  Garden gloves, large       2   $15.98",
                         "  Hose splitter              1    $7.99",
                         "  ------------------------------------",
                         "  SUBTOTAL                       $155.36",
                         "  TAX                              $9.71",
                         "  TOTAL                          $165.07",
                         "",
                         "  CARD ****  APPROVED",
                         "        THANK YOU - KEEP YOUR RECEIPT"),
                ],
            },
        ],
    }.Link();

    public static readonly PdfDocument[] All = [AnnualReport, Lease, VolunteerForm, QuickStart, Invoice, Receipt];

    public static IEnumerable<PdfDocument> Recent => All.OrderByDescending(d => d.LastOpened);
}
