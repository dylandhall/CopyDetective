namespace CopyDetective;


public record FileContent
{
    public FileContent(string filename, HashSet<Line> lines)
    {
        Filename = filename;
        HashCode = Filename.GetHashCode();
        Lines = lines;
        foreach (var line in Lines)
            line.ParentFile = this;
    }

    public string Filename { get; }
    public HashSet<Line> Lines { get; }

    public readonly int HashCode;
    public override int GetHashCode() => HashCode;

    public override string ToString() => Filename + " (" + Lines.Count + " lines)";

    public virtual bool Equals(FileContent? other) => other?.Filename == Filename;

    public Line? this[int index] => Lines.Count > index && index >= 0 ? Lines.ElementAt(index) : null;
}
public record Line
{
    public Line(string text,int index)
    {
        Text = text;
        TrimmedText = Text.Trim();
        Index = index;
        _hashCode = TrimmedText.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public FileContent? ParentFile { get; set; }
    public readonly string Text;
    public readonly string TrimmedText;

    public readonly int Index;

    public bool ValidStart = false;

    private readonly int _hashCode; 
    public override int GetHashCode() => _hashCode;
    
    public Line[]? MatchingLines { get; set; }

    public bool IsProcessed = false;
}