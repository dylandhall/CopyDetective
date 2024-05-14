using CopyDetective;

public static class Ext
{
    public static IEnumerable<string> PrintLines(this IEnumerable<string> lines, int maxWidth, Justify justify)
    {
        foreach (var line in lines)
        {
            if (line.Length <= maxWidth)
            {
                yield return JustifyString(line);
                continue;
            }

            for (int i = 0; i < line.Length; i+=maxWidth)
            {
                if (i>=line.Length) continue;
                var len = Math.Min(maxWidth, line.Length - i);
                yield return JustifyString(line.Substring(i, len));
            }
        }

        string JustifyString(string value)
        {
            switch (justify)
            {
                case Justify.Left:
                    value = value.TrimEnd();
                    return value + (maxWidth - value.Length).AsStringOf();
                case Justify.Centre:
                    value = value.Trim();
                    return ((int)Math.Floor((maxWidth - value.Length) / 2f)).AsStringOf() + value +
                           ((int)Math.Ceiling((maxWidth - value.Length) / 2f)).AsStringOf();
                case Justify.Right:
                    value = value.TrimStart();
                    return (maxWidth - value.Length).AsStringOf() + value;
                default:
                    return string.Empty;
            }
        }
    }

    public static IEnumerable<(string[] fileNames, List<string> lines)> GetBlocks(this IEnumerable<Line> matchingLines,
        List<Line>? lineCollection = null)
    {
        matchingLines = matchingLines.ToArray();
        var addedText = lineCollection == null;
        lineCollection ??= [matchingLines.First()];

        do
        {
            Line[] newLines = matchingLines.Select(l => l.ParentFile![l.Index + 1]).Where(v => v != null).ToArray()!;

            if (newLines.Length == 0)
            {
                if (addedText && lineCollection.Count > 10)
                    yield return (matchingLines.Select(l => $"{l.ParentFile!.Filename}:{l.Index - lineCollection.Count + 1}").ToArray(), lineCollection.Select(l => l.Text).ToList());
                yield break;
            }

            if (newLines.Length != matchingLines.Count() || newLines.Select(l => l.GetHashCode()).Distinct().Count() > 1)
            {
                var groups = newLines.GroupBy(l => l.GetHashCode()).Where(v => v.Count() > 1).ToList();
                if (groups.Count == 0)
                {
                    if (addedText && lineCollection.Count > 10) yield return (matchingLines.Select(l => $"{l.ParentFile!.Filename}:{l.Index - lineCollection.Count + 1}").ToArray(), lineCollection.Select(l => l.Text).ToList());
                    yield break;
                }
                //
                // if (groups.Count > 1 && addedText && lineCollection.Count > 10)
                //     yield return (matchingLines.Select(l => $"{l.ParentFile!.Filename}:{l.Index - lineCollection.Count + 1}").ToArray(), lineCollection.Select(l => l.Text).ToList());

                int returns = 0;
                foreach (var group in groups)
                {
                    // if we're no longer tracking a unique sequence, we'll have covered these intersections elsewhere
                    if (group.All(l => !(l.ParentFile![l.Index - lineCollection.Count]?.ValidStart ?? false)))
                        continue;

                    foreach (var block in group.GetBlocks(lineCollection.Append(group.MinBy(l => l.ParentFile!.Filename + l.Index)!).ToList()))
                    {
                        yield return block;
                        returns++;
                    }
                }

                if (returns > 1)
                {
                    yield return (matchingLines.Select(l => $"{l.ParentFile!.Filename}:{l.Index - lineCollection.Count + 1}").ToArray(), lineCollection.Select(l => l.Text).ToList());
                }
                yield break;
            }

            var text = newLines.MinBy(l => l.ParentFile!.Filename)!;
            lineCollection.Add(text);
            addedText = addedText || !string.IsNullOrWhiteSpace(text.TrimmedText);
            matchingLines = newLines;
        } while (true);
    }

    public static void SetValidStartingLines(this List<Line[]> list)
    {
        // foreach (var line in list
        //              .Select(grouping => (grouping,
        //                  hash: string.Join(string.Empty, grouping.Select(l => l.ParentFile!.Filename).OrderBy(l => l))
        //                      .GetHashCode()))
        //              .GroupBy(g => g.hash)
        //              .SelectMany(groupsWithUniqueFileset =>
        //                  groupsWithUniqueFileset.SelectMany(linesInFile => linesInFile.grouping)
        //                      .GroupBy(l => l.ParentFile!.Filename)
        //                      .SelectMany(linesInFile => linesInFile
        //                          .ExceptBy(linesInFile.Select(v => v.Index + 1), li => li.Index)
        //                          .GroupBy(l =>
        //                              l.GetHashCode()) // if a file has several identical lines, only start from one of them
        //                          .Select(l => l.MinBy(i => i.Index)))))
        //     line!.ValidStart = true;
        // foreach (var line in list
        //              .Select(grouping => (grouping,
        //                  hash: string.Join(string.Empty, grouping.Select(l => l.ParentFile!.Filename).OrderBy(l => l))
        //                      .GetHashCode()))
        //              .GroupBy(g => g.hash)
        //              .SelectMany(groupsWithUniqueFileset =>
        //                  groupsWithUniqueFileset.SelectMany(linesInFile => linesInFile.grouping)
        //                      .GroupBy(l => l.ParentFile!.Filename)
        //                      .SelectMany(linesInFile => linesInFile.Where(l => l.TrimmedText.Length>3)
        //                          .ExceptBy(linesInFile.Select(v => v.Index + 1), li => li.Index))))
        //     line!.ValidStart = true;
        foreach (var line in list.SelectMany(uniqueLines => uniqueLines)
                             .GroupBy(l => l.ParentFile!.Filename)
                             .SelectMany(linesInFile => linesInFile
                                 .Where(l => l.TrimmedText.Length>3)
                                 .ExceptBy(linesInFile.Select(v => v.Index + 1), li => li.Index)))
           line.ValidStart = true;
        //
        // Console.WriteLine(string.Join(", ", list.SelectMany(l => l).Where(l => l.ParentFile!.Filename.EndsWith("StudentLearningPathway.cshtml")).Select(l => l.Index)));
        // Console.WriteLine(string.Join(", ", list.SelectMany(l => l).Where(l => l.ParentFile!.Filename.EndsWith("StudentLearningPathway.cshtml") && l.ValidStart).Select(l => l.Index)));
    }

    public static IEnumerable<string> GetFiles(this string folder, string filemask, bool recurse)
    {
        var e = string.IsNullOrEmpty(filemask)
            ? Directory.EnumerateFiles(folder)
            : Directory.EnumerateFiles(folder, filemask);
        foreach (var f in e)
            yield return f;

        if (!recurse) yield break;
        foreach (var directory in Directory.GetDirectories(folder))
        {
            foreach (var f in GetFiles(directory, filemask, recurse))
                yield return f;
        }
    }

    public static string AsStringOf(this int len, char c = ' ') => new (Enumerable.Repeat(c, len).ToArray());

    public static bool Eq(this string arg, string two) => arg.Equals(two, StringComparison.OrdinalIgnoreCase);
}