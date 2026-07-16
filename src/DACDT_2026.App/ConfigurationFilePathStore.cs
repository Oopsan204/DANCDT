using System;
using System.IO;

namespace DACDT_2026
{
    public sealed class ConfigurationFilePathStore
    {
        private readonly string defaultConfigurationPath;
        private readonly string selectionStatePath;

        public ConfigurationFilePathStore(string defaultConfigurationPath, string selectionStatePath)
        {
            this.defaultConfigurationPath = defaultConfigurationPath ?? string.Empty;
            this.selectionStatePath = selectionStatePath ?? string.Empty;
        }

        public string GetSelectedPath()
        {
            try
            {
                string selectedPath = File.Exists(selectionStatePath)
                    ? File.ReadAllText(selectionStatePath).Trim()
                    : string.Empty;
                return string.IsNullOrWhiteSpace(selectedPath) ? defaultConfigurationPath : selectedPath;
            }
            catch
            {
                return defaultConfigurationPath;
            }
        }

        public bool TrySaveSelectedPath(string path)
        {
            string selectedPath = path == null ? string.Empty : path.Trim();
            if (string.IsNullOrWhiteSpace(selectedPath) || string.IsNullOrWhiteSpace(selectionStatePath))
                return false;

            try
            {
                string directory = Path.GetDirectoryName(selectionStatePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(selectionStatePath, selectedPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool NeedsSelection(string path)
        {
            return string.IsNullOrWhiteSpace(path) || !File.Exists(path);
        }

        public string GetBrowseDirectory(string path)
        {
            try
            {
                string selectedPath = string.IsNullOrWhiteSpace(path) ? defaultConfigurationPath : path.Trim();
                string selectedDirectory = Path.GetDirectoryName(selectedPath);
                if (!string.IsNullOrWhiteSpace(selectedDirectory) && Directory.Exists(selectedDirectory))
                    return selectedDirectory;

                string defaultDirectory = Path.GetDirectoryName(defaultConfigurationPath);
                if (!string.IsNullOrWhiteSpace(defaultDirectory) && Directory.Exists(defaultDirectory))
                    return defaultDirectory;
            }
            catch
            {
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}
