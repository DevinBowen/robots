namespace agents_tools.controllers;

public sealed class InstallProgress
{
    public long BytesWritten { get; set; }
    public long TotalBytes { get; set; }

    public int Percentage
    {
        get
        {
            if (TotalBytes <= 0) return 0;
            return (int)((BytesWritten * 100L) / TotalBytes);
        }
    }
}
