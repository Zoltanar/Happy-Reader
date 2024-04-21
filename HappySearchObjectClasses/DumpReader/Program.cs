using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DatabaseDumpReader;

namespace Happy_Apps_Core.DumpReader;

public static class Program
{
    private static readonly string DumpFolder = Path.Combine(StaticHelpers.AppDataFolder, "Database Dumps");
    private const string LatestDbDumpUrl = "https://dl.vndb.org/dump/vndb-db-latest.tar.zst";
    private const string LatestVoteDumpUrl = "https://dl.vndb.org/dump/vndb-votes-latest.gz";
    private const string RsyncUrlBase = @"rsync://dl.vndb.org/vndb-img/";
    private const int UpToDateDays = 1;
    public static Action<string[]> PrintLogLine = text => StaticHelpers.Logger.ToFile(text);
    private static Stopwatch RunWatch;
    private static Stopwatch DownloadWatch;


    public static async Task<UpdateResult> Execute(Action<string[]> loggingAction)
    {
        PrintLogLine = loggingAction;
        UpdateResult result = new UpdateResult();
        Stopwatch syncWatch = null;
        var databaseLogging = StaticHelpers.Logger.LogDatabase;

        try
        {
            StaticHelpers.Logger.LogDatabase = false;
            var dumpFolder = DumpFolder;
            await Task.Run(()=> Run(dumpFolder, StaticHelpers.CSettings.UserID, result));
            if (result.Type != UpdateType.Error)
            {
                syncWatch = Stopwatch.StartNew();
                await Task.Run(()=> SyncImages(StaticHelpers.CSettings.SyncImages));
                syncWatch.Stop();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            PrintLogLine([ex.ToString()]);
            Console.ResetColor();
            result.Type = UpdateType.Error;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            StaticHelpers.Logger.LogDatabase = databaseLogging;
            if (DownloadWatch != null && !DownloadWatch.IsRunning) PrintLogLine([$"Time for Dump Download: {DownloadWatch.Elapsed.TotalMinutes:00}:{DownloadWatch.Elapsed.Seconds:00}"]);
            if (RunWatch != null && !RunWatch.IsRunning) PrintLogLine([$"Time for DB Update: {RunWatch.Elapsed.TotalMinutes:00}:{RunWatch.Elapsed.Seconds:00}"]);
            if (syncWatch != null && !syncWatch.IsRunning) PrintLogLine([$"Time for Image Sync: {syncWatch.Elapsed.TotalMinutes:00}:{syncWatch.Elapsed.Seconds:00}"]);
        }
        return result;

    }

    private static void SyncImages(ImageSyncMode syncMode)
    {
        if (syncMode == ImageSyncMode.None) return;
        if (syncMode == ImageSyncMode.All)
        {
            SyncImagesForFolder(string.Empty);
            return;
        }
        if (syncMode.HasFlag(ImageSyncMode.Characters))
        {
            SyncImagesForFolder("ch/");
        }
        if (syncMode.HasFlag(ImageSyncMode.Covers))
        {
            SyncImagesForFolder("cv/");
        }
        if (syncMode.HasFlag(ImageSyncMode.Screenshots))
        {
            SyncImagesForFolder("sf/");
        }
        if (syncMode.HasFlag(ImageSyncMode.Thumbnails))
        {
            SyncImagesForFolder("st/");
        }
    }

    private static void SyncImagesForFolder(string folder)
    {
        var url = $"{RsyncUrlBase}{folder}";
        var path = $"{StaticHelpers.CSettings.ImageFolderPath}{folder}";
        PrintLogLine([$"RSync: Syncing images from {url} to {path}..."]);
        var success = RSync.Run(url, path, out var error);
        if (!success) PrintLogLine([$"Failed to sync images: {error}"]);
    }

    private static void RemovePastBackups(string dumpFolder, DumpFileInfo dumpFileInfo)
    {
        if (!StaticHelpers.CSettings.ClearOldDumpsAndBackups) return;
        try
        {
            RemovePastDatabaseFiles();
            RemovePastDataDumps(dumpFolder, dumpFileInfo);
        }
        catch (Exception ex)
        {
            //don't change exit code if it fails during backup removal
            Console.ForegroundColor = ConsoleColor.Red;
            StaticHelpers.Logger.ToFile(ex);
            Console.ResetColor();
        }
    }

    private static void RemovePastDataDumps(string dumpFolder, DumpFileInfo dumpFileInfo)
    {
        var infos = new DirectoryInfo(dumpFolder).GetFileSystemInfos("vndb-*", SearchOption.TopDirectoryOnly).ToArray();
        foreach (var info in infos)
        {
            DeleteFileOrFolder(dumpFileInfo, info);
        }
    }

    private static void RemovePastDatabaseFiles()
    {
        if (string.IsNullOrWhiteSpace(StaticHelpers.DatabaseFile)) throw new InvalidOperationException("Database file path was empty.");
        //remove all but one DB backups
        var fileNoExt = Path.GetFileNameWithoutExtension(StaticHelpers.DatabaseFile);
        var fileExt = Path.GetExtension(StaticHelpers.DatabaseFile);
        var databaseDirectory = Directory.GetParent(StaticHelpers.DatabaseFile);
        if (databaseDirectory == null) throw new InvalidOperationException("Database directory was null.");
        var files = databaseDirectory.GetFiles($"{fileNoExt}*", SearchOption.TopDirectoryOnly)
            //only files of same extension and exclude the database file.
            .Where(f => f.Extension == fileExt && f.FullName != StaticHelpers.DatabaseFile)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            //skip the most recent backup
            .Skip(1).ToArray();
        foreach (var file in files)
        {
            DeleteFileOrFolder(null, file);
        }
    }

    public static async Task Run(string dumpFolder, int userId, UpdateResult result)
    {
        DumpReader.GetDbStats(StaticHelpers.DatabaseFile, out var previousDumpUpdate, out var previousVnIds, out var previousCharacterIds);
        var dumpFileInfo = await GetLatestDump(previousDumpUpdate, dumpFolder);
        var oldDateString = previousDumpUpdate.HasValue
            ? $"Current update was from: {previousDumpUpdate.Value.ToShortDateString()}."
            : "There was no previous dump file.";
        if (dumpFileInfo.UpToDate || !dumpFileInfo.NewFileDate.HasValue)
        {
            var reloadUpdate = MessageBox.Show($"{oldDateString}{Environment.NewLine}Already up to date.{Environment.NewLine}" +
                                               $"Do you wish to reload latest database dump?", $"{StaticHelpers.ClientName} - VNDB Update", MessageBoxButton.YesNo);
            if (reloadUpdate != MessageBoxResult.Yes)
            {
                result.ErrorMessage = "Not reloading.";
                result.Type = UpdateType.NoUpdate;
                return;
            }
        }
        Debug.Assert(dumpFileInfo.NewFileDate != null, nameof(dumpFileInfo.NewFileDate) + " != null");
        PrintLogLine([oldDateString, $"Getting update to: {dumpFileInfo.NewFileDate.Value.ToShortDateString()}."]);
        RunWatch = Stopwatch.StartNew();
        var processor = new DumpReader(dumpFileInfo.LatestDumpFolder, StaticHelpers.DatabaseFile, userId, out var inProgressFile);
        processor.Run(dumpFileInfo.NewFileDate.Value, previousVnIds, previousCharacterIds);
        result.Type = dumpFileInfo.UpToDate ? UpdateType.ReloadLatest : UpdateType.Update;
        RunWatch.Stop();
        if (result.Type is UpdateType.ReloadLatest or UpdateType.Update)
        {
            var userAnswer = MessageBox.Show("VNDB data updated, select Yes to replace database file with updated.",
                $"{StaticHelpers.ClientName} - VNDB Update", MessageBoxButton.YesNo);
            if (userAnswer != MessageBoxResult.Yes)
            {
                result.ErrorMessage = "VNDB update was rejected.";
                result.Type = UpdateType.NoUpdate;
                return;
            }
            File.Delete(StaticHelpers.DatabaseFile);
            File.Move(inProgressFile, StaticHelpers.DatabaseFile);
        }
        RemovePastBackups(dumpFolder, dumpFileInfo);
    }

    private static async Task<DumpFileInfo> GetLatestDump(DateTime? previousUpdate, string dumpsFolder)
    {
        var upToDate = !(previousUpdate == null || (DateTime.UtcNow - previousUpdate.Value).TotalDays > UpToDateDays);
        var di = Directory.CreateDirectory(dumpsFolder);
        DownloadWatch = Stopwatch.StartNew();
        var dumps = await DownloadLatestDumpFiles();
        DownloadWatch.Stop();
        var newFileDate = dumps.DatabaseDump.LastWriteTimeUtc;
        var targetFolder = await ExtractTarToFolder(dumps.DatabaseDump, di);
        await ExtractGzToFolder(dumps.VoteDump, new DirectoryInfo(targetFolder));
        var result = new DumpFileInfo
        {
            LatestDumpFolder = targetFolder,
            LatestDumpFile = dumps.DatabaseDump,
            LatestVoteDumpFile = dumps.VoteDump,
            NewFileDate = newFileDate,
            UpToDate = upToDate
        };
        return result;
    }

    private static void DeleteFileOrFolder(DumpFileInfo dumpFileInfo, FileSystemInfo info)
    {
        if (dumpFileInfo?.Contains(info) ?? false) return;
        try
        {
            if (info is DirectoryInfo dInfo) dInfo.Delete(true);
            else info.Delete();
        }
        catch (Exception ex)
        {
            StaticHelpers.Logger.ToFile(ex);
        }
    }

    private static async Task ExtractGzToFolder(FileInfo file, DirectoryInfo folder)
    {
        var destFileName = Path.Combine(folder.FullName, Path.GetFileNameWithoutExtension(file.Name));
        if (File.Exists(destFileName)) return;
        using var fileStream = file.OpenRead();
        using var zip = new GZipStream(fileStream, CompressionMode.Decompress);
        using var newFileStream = new FileStream(destFileName, FileMode.CreateNew);
        await zip.CopyToAsync(newFileStream);
    }

    private static async Task<(FileInfo DatabaseDump,FileInfo VoteDump)> DownloadLatestDumpFiles()
    {
        var dbDumpPath = await StaticHelpers.DownloadFile(LatestDbDumpUrl, DumpFolder, null, PrintLogLine)
                         ?? throw new InvalidOperationException("Failed to download database dump.");
        var voteDumpPath = await StaticHelpers.DownloadFile(LatestVoteDumpUrl, DumpFolder, null, PrintLogLine) 
                           ?? throw new InvalidOperationException("Failed to download vote dump.");
        return (new FileInfo(dbDumpPath), new FileInfo(voteDumpPath));
    }

    private static async Task<string> ExtractTarToFolder(FileInfo file, DirectoryInfo folder)
    {
        var fileName = Path.Combine(folder.FullName, Path.GetFileNameWithoutExtension(file.Name));
        var folderName = Path.Combine(folder.FullName, Path.GetFileNameWithoutExtension(fileName));
        Stream tarStream = null;
        try
        {
            if (!File.Exists(fileName))
            {
                using var extractStream = new Zstandard.Net.ZstandardStream(file.OpenRead(), CompressionMode.Decompress);
                tarStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                await extractStream.CopyToAsync(tarStream);
                tarStream.Seek(0, SeekOrigin.Begin);
            }
            else tarStream = File.Open(fileName, FileMode.Open, FileAccess.Read);
            await ExtractTar(tarStream, folderName);
        }
        finally
        {
            tarStream?.Dispose();
        }
        return folderName;
    }

    /// <summary>
    /// Extracts a <c>tar</c> archive to the specified directory.
    /// </summary>
    /// <param name="stream">The <i>.tar</i> to extract.</param>
    /// <param name="outputDir">Output directory to write the files.</param>
    private static async Task ExtractTar(Stream stream, string outputDir)
    {
        if (Directory.Exists(outputDir)) return;
        var buffer = new byte[100];
        while (true)
        {
            await stream.ReadAsync(buffer, 0, 100);
            var name = Encoding.ASCII.GetString(buffer).Trim('\0');
            if (string.IsNullOrWhiteSpace(name)) break;
            stream.Seek(24L, SeekOrigin.Current);
            await stream.ReadAsync(buffer, 0, 12);
            var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);
            stream.Seek(376L, SeekOrigin.Current);
            var output = Path.Combine(outputDir, name);
            var outputDirectory = Path.GetDirectoryName(output) ?? throw new ArgumentNullException(nameof(Path.GetDirectoryName));
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
            if (size != 0 && !name.Equals("./", StringComparison.InvariantCulture))
            {
                using var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write);
                int readSoFar = 0;
                do
                {
                    long chunkSize = Math.Min(4096, size - readSoFar);
                    var buf = new byte[chunkSize];
                    readSoFar += await stream.ReadAsync(buf, 0, buf.Length);
                    str.Write(buf, 0, buf.Length);
                } while (readSoFar < size);
            }
            var pos = stream.Position;
            var offset = 512 - (pos % 512);
            if (offset == 512) offset = 0;
            stream.Seek(offset, SeekOrigin.Current);
        }
    }

    private class DumpFileInfo
    {
        public string LatestDumpFolder { get; set; }
        public FileInfo LatestDumpFile { get; set; }
        public FileInfo LatestVoteDumpFile { get; set; }
        public DateTime? NewFileDate { get; set; }
        public bool UpToDate { get; set; }

        public bool Contains(FileSystemInfo info)
        {
            return (info is DirectoryInfo && info.FullName == LatestDumpFolder) ||
                   (info is FileInfo && info.FullName == LatestDumpFile.FullName) ||
                   (info is FileInfo && info.FullName == LatestVoteDumpFile.FullName);
        }
    }

    public class UpdateResult
    {
        public UpdateType Type { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success => Type != UpdateType.Unknown && Type != UpdateType.Error && Type != UpdateType.NoUpdate;

        public UpdateResult()
        {
            Type = UpdateType.Unknown;
        }
    }

    public enum UpdateType
    {
        Unknown = -2,
        Error = -1,
        Update = 0,
        ReloadLatest = 1,
        NoUpdate = 2,
    }
}