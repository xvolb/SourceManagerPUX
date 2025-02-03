namespace SourceManagerPUX.Models
{
    public class FolderState
    {
        public string DirectoryPath { get; set; } = string.Empty;
        public Dictionary<string, FolderAndeFileMetaData> Files { get; set; } = new Dictionary<string, FolderAndeFileMetaData>();
    }
}

