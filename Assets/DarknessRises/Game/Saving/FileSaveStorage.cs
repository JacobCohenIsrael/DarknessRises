using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// The save as a file on the device. A thin adapter: it only reads and writes text. Writing is atomic: the text
    /// goes to a temporary file first, which then takes the save file's place in one step, so a crash or a full disk
    /// mid-write leaves the old save whole. The save it replaced is kept beside it as a backup, and read when the save
    /// file itself is missing.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        /// <summary>The save's file name under the app's persistent data path.</summary>
        public const string FileName = "save.json";

        private const string TemporarySuffix = ".new";
        private const string BackupSuffix = ".bak";

        // No byte-order mark: the file is plain JSON.
        private static readonly Encoding TextEncoding = new UTF8Encoding(false);

        private readonly string path;
        private readonly string temporaryPath;
        private readonly string backupPath;

        /// <param name="filePath">The save file's full path. Its folder is created when it's first written.</param>
        public FileSaveStorage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("The save needs a file path.", nameof(filePath));
            }

            path = filePath;
            temporaryPath = filePath + TemporarySuffix;
            backupPath = filePath + BackupSuffix;
        }

        /// <summary>The save file in the app's persistent data path: where a player's progress normally lives.</summary>
        public static FileSaveStorage InPersistentData()
        {
            return new FileSaveStorage(Path.Combine(Application.persistentDataPath, FileName));
        }

        public string FilePath => path;

        public bool TryRead(out string text)
        {
            return TryReadFile(path, out text) || TryReadFile(backupPath, out text);
        }

        public bool TryWrite(string text)
        {
            try
            {
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(temporaryPath, text, TextEncoding);
                if (File.Exists(path))
                {
                    Replace();
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Save] Couldn't write the save to {path}: {exception.Message}");
                return false;
            }
        }

        public void Delete()
        {
            try
            {
                File.Delete(path);
                File.Delete(backupPath);
                File.Delete(temporaryPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Save] Couldn't delete the save at {path}: {exception.Message}");
            }
        }

        // The new file takes the save's place in one step where the platform can (the old save becomes the backup).
        // Where it can't, the old save is copied to the backup first, so a whole save is on disk at every moment.
        private void Replace()
        {
            try
            {
                File.Replace(temporaryPath, path, backupPath);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(path, backupPath, true);
                File.Delete(path);
                File.Move(temporaryPath, path);
            }
        }

        private static bool TryReadFile(string filePath, out string text)
        {
            text = null;
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                text = File.ReadAllText(filePath, TextEncoding);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Save] Couldn't read the save at {filePath}: {exception.Message}");
                return false;
            }
        }
    }
}
