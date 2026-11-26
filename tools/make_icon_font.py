"""Builds PaperHawk's icon fonts from Microsoft's Fluent UI System Icons (MIT) and writes the matching C# constants.

Each icon gets a private-use code point; the app refers to icons by name through the Ic class (Models/Ic.cs),
e.g. Text="{x:Static m:Ic.Home}" in XAML or Ic.Home in code.

    python tools/make_icon_font.py <path to fluentui-system-icons/fonts>

Output: src/PaperHawk.Desktop/Assets/Fonts/PaperHawkIcons-{Regular,Filled}.ttf and src/PaperHawk.Desktop/Models/Ic.cs
"""
import json, sys
from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.ttLib.tables._c_m_a_p import cmap_format_4

# C# name -> Fluent icon name. Code points are assigned in this order from U+E000, so only ever append.
ICONS = [
    ('Home', 'home'), ('Pdf', 'document_pdf'), ('Files', 'folder_open'), ('Folder', 'folder'), ('Tools', 'grid'),
    ('Apps', 'apps'), ('Comment', 'comment'), ('Comments', 'comment_multiple'), ('Edit', 'document_edit'), ('Pencil', 'edit'),
    ('Signature', 'signature'), ('Pen', 'pen'), ('Pages', 'document_multiple'), ('Combine', 'merge'), ('Export', 'arrow_export_ltr'),
    ('CreatePdf', 'document_add'), ('Protect', 'shield_lock'), ('Redact', 'eraser'), ('Scan', 'scan_text'), ('Compress', 'arrow_minimize'),
    ('Compare', 'split_vertical'), ('Highlight', 'highlight'), ('TextTool', 'text_t'), ('Strike', 'text_strikethrough'), ('Underline', 'text_underline'),
    ('Draw', 'inking_tool'), ('Shapes', 'shapes'), ('Note', 'note'), ('Hand', 'hand_left'), ('Cursor', 'cursor'),
    ('RotateCw', 'arrow_rotate_clockwise'), ('RotateCcw', 'arrow_rotate_counterclockwise'), ('FitPage', 'page_fit'), ('FitWidth', 'arrow_autofit_width'), ('TwoPage', 'book_open'),
    ('SinglePage', 'document'), ('Bookmark', 'bookmark'), ('Attach', 'attach'), ('Print', 'print'), ('Share', 'share'),
    ('Mail', 'mail'), ('Star', 'star'), ('Search', 'search'), ('ChevronUp', 'chevron_up'), ('ChevronDown', 'chevron_down'),
    ('ChevronLeft', 'chevron_left'), ('ChevronRight', 'chevron_right'), ('More', 'more_horizontal'), ('Settings', 'settings'), ('Sun', 'weather_sunny'),
    ('Moon', 'weather_moon'), ('Add', 'add'), ('Check', 'checkmark'), ('CheckCircle', 'checkmark_circle'), ('Dismiss', 'dismiss'),
    ('Info', 'info'), ('Warning', 'warning'), ('Lock', 'lock_closed'), ('Sparkle', 'sparkle'), ('Cloud', 'cloud'),
    ('Eye', 'eye'), ('History', 'history'), ('Clock', 'clock'), ('Person', 'person'), ('People', 'people'),
    ('Filter', 'filter'), ('Sort', 'arrow_sort'), ('Delete', 'delete'), ('Download', 'arrow_download'), ('Sync', 'arrow_sync'),
    ('Help', 'question_circle'), ('Pin', 'pin'), ('Image', 'image'), ('Table', 'table'), ('Link', 'link'),
    ('Layers', 'layer'), ('FullScreen', 'full_screen_maximize'), ('First', 'arrow_previous'), ('Last', 'arrow_next'), ('TextBox', 'textbox'),
    ('Checked', 'checkbox_checked'), ('Unchecked', 'checkbox_unchecked'), ('Calendar', 'calendar_ltr'), ('Certificate', 'certificate'), ('Accessibility', 'accessibility'),
    ('Ruler', 'ruler'), ('Translate', 'translate'), ('ReadAloud', 'read_aloud'), ('PanelLeft', 'panel_left'), ('PanelRight', 'panel_right'),
    ('ZoomIn', 'zoom_in'), ('ZoomOut', 'zoom_out'), ('ZoomFit', 'zoom_fit'), ('Undo', 'arrow_undo'), ('Redo', 'arrow_redo'),
    ('Save', 'save'), ('Document', 'document_text'), ('Page', 'document_one_page'), ('Copy', 'copy'), ('Form', 'form'),
    ('Upload', 'arrow_upload'), ('Swap', 'arrow_swap'), ('DocLock', 'document_lock'), ('Outline', 'text_bullet_list_square'), ('Tag', 'tag'),
    ('DocSearch', 'document_search'), ('Verified', 'shield_checkmark'), ('Properties', 'text_description'), ('Rows', 'layout_row_two'), ('Cut', 'cut'),
    ('Bold', 'text_bold'), ('Italic', 'text_italic'), ('Color', 'color'), ('DocSign', 'document_signature'), ('Stack', 'square_multiple'),
]
CP = {name: 0xE000 + i for i, (name, _) in enumerate(ICONS)}


def build(src_dir, style, family, out_dir):
    names = json.load(open(f'{src_dir}/FluentSystemIcons-{style}.json'))
    font = TTFont(f'{src_dir}/FluentSystemIcons-{style}.ttf')
    cmap = font.getBestCmap()
    chosen = {}
    for name, fluent in ICONS:
        for size in (20, 24, 16, 28):
            key = f'ic_fluent_{fluent}_{size}_{style.lower()}'
            if key in names:
                chosen[CP[name]] = cmap[names[key]]
                break
        else:
            sys.exit(f'missing icon: {fluent} ({style})')
    opts = subset.Options()
    opts.glyph_names = True
    opts.notdef_outline = True
    opts.name_IDs = ['*']
    opts.layout_features = []
    sub = subset.Subsetter(opts)
    sub.populate(glyphs=sorted(set(chosen.values())))
    sub.subset(font)
    table = cmap_format_4(4)
    table.platformID, table.platEncID, table.language = 3, 1, 0
    table.cmap = chosen
    font['cmap'].tables = [table]
    for rec in font['name'].names:
        if rec.nameID in (1, 4, 16):
            rec.string = family
        elif rec.nameID == 6:
            rec.string = family.replace(' ', '')
        elif rec.nameID in (2, 17):
            rec.string = 'Regular'
    out = f'{out_dir}/PaperHawkIcons-{style}.ttf'
    font.save(out)
    print(out, len(chosen), 'icons')


def write_constants(path):
    lines = [
        '// Generated by tools/make_icon_font.py. Do not edit by hand; add icons there and re-run it.',
        'namespace PaperHawk.Models;',
        '',
        '/// <summary>Icon glyphs in the PaperHawk icon fonts (Fluent UI System Icons). XAML: {x:Static m:Ic.Home}.</summary>',
        'public static class Ic',
        '{',
    ]
    for name, fluent in ICONS:
        lines.append(f'    public const string {name} = "\\u{CP[name]:04X}"; // {fluent}')
    lines.append('}')
    open(path, 'w', newline='\n').write('\n'.join(lines) + '\n')
    print(path)


if __name__ == '__main__':
    src = sys.argv[1]
    out = 'src/PaperHawk.Desktop/Assets/Fonts'
    build(src, 'Regular', 'PaperHawk Icons', out)
    build(src, 'Filled', 'PaperHawk Icons Filled', out)
    write_constants('src/PaperHawk.Desktop/Models/Ic.cs')
