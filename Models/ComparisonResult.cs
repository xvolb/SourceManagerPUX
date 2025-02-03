namespace SourceManagerPUX.Models
{
    public class ComparisonResult
    {
        public List<string> NewFilesAndDirectories { get; set; } = new List<string>();
        public List<string> ChangedFilesAndDirectories { get; set; } = new List<string>();
        public List<string> DeletedFilesAndDirectories { get; set; } = new List<string>();
    }
}
