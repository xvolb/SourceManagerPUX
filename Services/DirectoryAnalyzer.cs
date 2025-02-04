using SourceManagerPUX.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace SourceManagerPUX.Services
{
    public class DirectoryAnalyzer
    {
        private readonly string _previousStateFile;
        private readonly ILogger<DirectoryAnalyzer> _logger;
        public DirectoryAnalyzer(ILogger<DirectoryAnalyzer> logger, string previousStateFile = "previousState.json")
        {
            _previousStateFile = previousStateFile;
            _logger = logger;

        }
        public ComparisonResult AnalyzeDirectory(string directoryPath)
        {
            Dictionary<string, FolderState> allFolderStates = LoadAllFolderStates();
            Dictionary<string, FolderAndeFileMetaData> currentState = GetDirectoryAndFileState(directoryPath);
            Dictionary<string, FolderAndeFileMetaData> previousState = allFolderStates.ContainsKey(directoryPath)
                        ? allFolderStates[directoryPath].Files
                        : new Dictionary<string, FolderAndeFileMetaData>();

            ComparisonResult result = CompareStates(previousState, currentState);

            UpdateOrAddFolderState(directoryPath, currentState);

            return result;
        }

        public Dictionary<string, FolderAndeFileMetaData> GetDirectoryAndFileState(string directoryPath)
        {
            const int FilesMaxCount = 100;
            const long MaxFileSize = 52428800;
            var state = new Dictionary<string, FolderAndeFileMetaData>();

            List<string> files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly).Take(FilesMaxCount).ToList();
            if (files.Count >= FilesMaxCount)
            {
                _logger.LogWarning($"[{DateTime.Now}] Directory '{directoryPath}' contains more than {FilesMaxCount} files. Only the first {FilesMaxCount} will be processed.");
            }

            foreach (string file in files)
            {
                try
                {
                    // Check file size (50 MB = 50 * 1024 * 1024 bytes=52428800)
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length > MaxFileSize)
                    {
                        _logger.LogWarning($"[{DateTime.Now}] Skipped file '{file}' because its size exceeds 50 MB.");
                        continue;
                    }
                    string hash = ComputeFileHash(file);
                    state[file] = new FolderAndeFileMetaData
                    {
                        Name = Path.GetFileName(file),
                        Hash = hash,
                        Version = 1
                    };
                }
                catch (IOException ex)
                {
                    _logger.LogWarning($"[{DateTime.Now}] Skipped file '{Path.GetFileName(file)}': {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning($"[{DateTime.Now}] Access denied to the file '{Path.GetFileName(file)}'. Error: {ex.Message}");
                }
            }

            foreach (var dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string folderHash = ComputeFolderHash(dir);
                    state[dir] = new FolderAndeFileMetaData
                    {
                        Name = Path.GetFileName(dir),
                        Hash = folderHash,
                        Version = 1
                    };
                }
                catch (IOException ex)
                {
                    _logger.LogWarning($"[{DateTime.Now}] Skipped '{dir}': {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning($"[{DateTime.Now}] Access denied to the directory '{dir}'. Error: {ex.Message}");
                }
            }

            return state;
        }
        
        public ComparisonResult CompareStates(Dictionary<string, FolderAndeFileMetaData> previousState, Dictionary<string, FolderAndeFileMetaData> currentState)
        {
            var newFilesAndDirectories = new List<string>();
            var changedFilesAndDirectories = new List<string>();
            var deletedFilesAndDirectories = new List<string>();

            // check new and changed
            foreach (var path in currentState.Keys)
            {
                if (!previousState.ContainsKey(path))
                {
                    newFilesAndDirectories.Add(path);
                }
                else if (previousState[path].Hash != currentState[path].Hash)
                {
                    currentState[path].Version = previousState[path].Version + 1;
                    changedFilesAndDirectories.Add(path);
                }
            }

            // check deleted
            foreach (var path in previousState.Keys)
            {
                if (!currentState.ContainsKey(path))
                {
                    deletedFilesAndDirectories.Add(path);
                }
            }

            return new ComparisonResult
            {
                NewFilesAndDirectories = newFilesAndDirectories,
                ChangedFilesAndDirectories = changedFilesAndDirectories,
                DeletedFilesAndDirectories = deletedFilesAndDirectories
            };
        }
        public Dictionary<string, FolderState> LoadAllFolderStates()
        {
            if (System.IO.File.Exists(_previousStateFile))
            {
                var json = System.IO.File.ReadAllText(_previousStateFile);
                return JsonSerializer.Deserialize<Dictionary<string, FolderState>>(json) ?? new Dictionary<string, FolderState>();
            }
            return new Dictionary<string, FolderState>();
        }
        // Updates the current directory's state in the saved folder states or adds it if it's new.
        public void UpdateOrAddFolderState(string directoryPath, Dictionary<string, FolderAndeFileMetaData> currentState)
        {
            var allFolderStates = LoadAllFolderStates();

            if (allFolderStates.ContainsKey(directoryPath))
            {
                allFolderStates[directoryPath].Files = currentState;
            }
            else
            {
                allFolderStates[directoryPath] = new FolderState
                {
                    DirectoryPath = directoryPath,
                    Files = currentState
                };
            }

            SaveAllFolderStates(allFolderStates);
        }
        public void SaveAllFolderStates(Dictionary<string, FolderState> allFolderStates)
        {
            var json = JsonSerializer.Serialize(allFolderStates);
            System.IO.File.WriteAllText(_previousStateFile, json);
        }
        #region Hashes
        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }
        private string ComputeFolderHash(string directoryPath)
        {
            using (var sha256 = SHA256.Create())
            {
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                                     .OrderBy(f => f)
                                     .ToList();

                var combinedHash = string.Empty;

                foreach (var file in files)
                {
                    var fileHash = ComputeFileHash(file);
                    combinedHash += fileHash;
                }

                var folderHashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedHash));
                return BitConverter.ToString(folderHashBytes).Replace("-", "").ToLower();
            }
        }
        #endregion
    }
}
