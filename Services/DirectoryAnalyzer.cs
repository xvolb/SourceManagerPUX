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

            var currentState = GetDirectoryAndFileState(directoryPath);
            var previousState = LoadPreviousState();

            var result = CompareStates(previousState, currentState);

            SaveCurrentState(currentState);

            return result;
        }

        public Dictionary<string, FolderAndeFileMetaData> GetDirectoryAndFileState(string directoryPath)
        {
            var state = new Dictionary<string, FolderAndeFileMetaData>();

            foreach (string file in Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                try
                {
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
                string folderHash = ComputeFolderHash(dir);
                state[dir] = new FolderAndeFileMetaData
                {
                    Name = Path.GetFileName(dir),
                    Hash = folderHash,
                    Version = 1
                };
            }

            return state;
        }
        public void SaveCurrentState(Dictionary<string, FolderAndeFileMetaData> state)
        {
            var json = JsonSerializer.Serialize(state);
            System.IO.File.WriteAllText(_previousStateFile, json);
        }

        public Dictionary<string, FolderAndeFileMetaData> LoadPreviousState()
        {
            if (System.IO.File.Exists(_previousStateFile))
            {
                var json = System.IO.File.ReadAllText(_previousStateFile);
                return JsonSerializer.Deserialize<Dictionary<string, FolderAndeFileMetaData>>(json) ?? new Dictionary<string, FolderAndeFileMetaData>();
            }
            return new Dictionary<string, FolderAndeFileMetaData>();
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
    }
}
