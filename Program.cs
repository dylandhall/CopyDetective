// See https://aka.ms/new-console-template for more information

using System.Text;
using CopyDetective;
using Dasync.Collections;


var folder = Environment.CurrentDirectory;
var filemask = string.Empty;
var recurse = false;
var colour = true;
var maxWidth = Console.WindowWidth - 4;

if (args.Any(a => a.Trim() is "--help" or "-h"))
{
    Console.WriteLine(" -f, --folder    Folder to read");
    Console.WriteLine(" -m, --filemask  Search pattern");
    Console.WriteLine(" -r, --recurse   Recurse subfolders");
    Console.WriteLine(" -c, --clean     No colour");
    Console.WriteLine(" -w, --width     Maximum width");
    Console.WriteLine(" -h, --help      This message");
    Console.WriteLine();
    return;
}

for (int i = 0; i < args.Length-1; i++)
{
    if (args[i] == "-f" || args[i] == "--folder")
    {
        i++;
        folder = args[i];
    }
    else if (args[i] == "-m" || args[i] == "--filemask")
    {
        i++;
        filemask = args[i];
    }
    else if (args[i] is "-r" or "--recurse")
    {
        recurse = true;
    }
    else if (args[i] is "-c" or "--clean")
    {
        colour = false;
    }
    else if (args[i] is "-w" or "--width")
    {
        i++;
        if (int.TryParse(args[i], out maxWidth)) maxWidth-=4;
    }
}

var allFiles = await folder.GetFiles(filemask, recurse)
    .ParallelSelectEnumAsync(async file =>
        {
            var lines = await File.ReadAllLinesAsync(file);
            return new FileContent(file, lines.Select((line, index) => new Line(line, index)).ToHashSet());
        })
    .ToListAsync();

var fileGroups = allFiles
    .SelectMany(l => l.Lines)
    .Where(l => l.TrimmedText.Length>3)
    .GroupBy(l => l.GetHashCode())
    .Where(g => g.Count()>1)
    .ToList();

fileGroups.SetValidStartingLines();

var groupsWithStartingPoint = fileGroups
    .Where(g => g.Any(l => l.ValidStart))
    .OrderByDescending(g => g.Count())
    .ThenBy(g => g.Min(a => a.Index))
    .ToList();

Console.WriteLine($"Calculated {groupsWithStartingPoint.Count} groups with starting lines");
//var sb = new StringBuilder();
var duplicatedBlocksOrderedByFilename = groupsWithStartingPoint
    .SelectMany(g => g.GetBlocks())
    .OrderBy(v => v.fileNames[0])
    .ThenBy(f => f.lines.Count)
    .ThenBy(f => string.Join(String.Empty, f.lines));

foreach (var (fileNames, lines) in duplicatedBlocksOrderedByFilename)
{
    //lines.Dump(string.Join(", ", fileNames));
    var width = Math.Min(maxWidth, lines.Select(l => l.Length).Max());

    if (colour) Console.ForegroundColor = ConsoleColor.White;
    Console.Write('\u2554');
    Console.Write((width + 2).AsStringOf('\u2550'));
    Console.WriteLine('\u2557');
    foreach (var line in fileNames.Select(Path.GetFileName)!.PrintLines(width, Justify.Centre))
    {
        Console.Write("\u2551 ");
        if (colour) Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write(line);
        if (colour) Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" \u2551");
    }
    if (colour) Console.ForegroundColor = ConsoleColor.White;
    Console.Write("\u255f");
    Console.Write((width + 2).AsStringOf('\u2500'));
    Console.WriteLine("\u2562");
    foreach (var line in lines.PrintLines(width, Justify.Left))
    {
        Console.Write("\u2551 ");
        if (colour) Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(line);
        if (colour) Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" \u2551");
    }
    Console.Write("\u255a");
    Console.Write((width + 2).AsStringOf('\u2550'));
    Console.WriteLine("\u255d");
}
