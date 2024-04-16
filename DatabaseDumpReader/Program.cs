using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Happy_Apps_Core;

namespace DatabaseDumpReader
{
	public static class Program
	{
		private static readonly string DumpFolder = Path.Combine(StaticHelpers.AppDataFolder, "Database Dumps");
		private const string LatestDbDumpUrl = "https://dl.vndb.org/dump/vndb-db-latest.tar.zst";
		private const string LatestVoteDumpUrl = "https://dl.vndb.org/dump/vndb-votes-latest.gz";
		private const string RsyncUrlBase = @"rsync://dl.vndb.org/vndb-img/";
		private const int UpToDateDays = 1;

		/// <summary>
		/// The exit code represents:
		///  0 - Success (Updated)
		///  1 - Success (Not Updated)
		/// -1 - Error
		/// </summary>
		/// <param name="args">Must pass 1 argument, Path to folder with DB Dump, or no arguments, to use default path.</param>
		private static int Main(string[] args)
		{
			ExitCode result;
            Stopwatch downloadWatch = null;
			Stopwatch runWatch = null;
			Stopwatch syncWatch = null;
            try
			{
				string settingsPath = args.Length < 1 ? StaticHelpers.AllSettingsJson : args[0];
				if (!File.Exists(settingsPath)) throw new FileNotFoundException("Settings File not found.", settingsPath);
				var dumpFolder = DumpFolder;
				StaticHelpers.CSettings = SettingsJsonFile.Load<SettingsViewModel>(settingsPath).CoreSettings;
                StaticHelpers.Logger.LogDatabase = false;
				result = Run(dumpFolder, StaticHelpers.CSettings.UserID, out downloadWatch, out runWatch);
				if (result != ExitCode.Error)
				{
					syncWatch = Stopwatch.StartNew();
					SyncImages(StaticHelpers.CSettings.SyncImages);
					syncWatch.Stop();
				}
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				StaticHelpers.Logger.ToFile(ex);
				Console.ResetColor();
				result = ExitCode.Error;
			}
			finally
			{
                if (downloadWatch != null) StaticHelpers.Logger.ToFile($"Time for Dump Download: {downloadWatch.Elapsed.TotalMinutes:00}:{downloadWatch.Elapsed.Seconds:00}");
				if (runWatch != null) StaticHelpers.Logger.ToFile($"Time for DB Update: {runWatch.Elapsed.TotalMinutes:00}:{runWatch.Elapsed.Seconds:00}");
				if (syncWatch != null) StaticHelpers.Logger.ToFile($"Time for Image Sync: {syncWatch.Elapsed.TotalMinutes:00}:{syncWatch.Elapsed.Seconds:00}");
			}
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
			return (int)result;

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
			var success = RSync.Run(url, path, out var error);
			if (!success) StaticHelpers.Logger.ToFile($"Failed to sync images: {error}");
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

		private static ExitCode Run(string dumpFolder, int userId, out Stopwatch downloadWatch, out Stopwatch runWatch)
        {
            runWatch = null;
			DumpReader.GetDbStats(StaticHelpers.DatabaseFile,out var previousDumpUpdate, out var previousVnIds, out var previousCharacterIds);
			var dumpFileInfo = GetLatestDump(previousDumpUpdate, dumpFolder, out downloadWatch);
			var oldDateString = previousDumpUpdate.HasValue
				? $"Current update was from: {previousDumpUpdate.Value.ToShortDateString()}."
				: "There was no previous dump file.";
			if (dumpFileInfo.UpToDate || !dumpFileInfo.NewFileDate.HasValue)
			{
				StaticHelpers.Logger.ToFile(oldDateString, "Already up to date.");
				Console.WriteLine("Do you wish to reload latest dump? (y/n)");
				var input = Console.ReadLine();
				if (input?.ToLowerInvariant() != "y")
				{
					Console.WriteLine("Not reloading.");
					return ExitCode.NoUpdate;
				}
			}
			Debug.Assert(dumpFileInfo.NewFileDate != null, nameof(dumpFileInfo.NewFileDate) + " != null");
			StaticHelpers.Logger.ToFile(oldDateString, $"Getting update to: {dumpFileInfo.NewFileDate.Value.ToShortDateString()}.");
			runWatch = Stopwatch.StartNew();
			var processor = new DumpReader(dumpFileInfo.LatestDumpFolder, StaticHelpers.DatabaseFile, userId, out var inProgressFile);
			processor.Run(dumpFileInfo.NewFileDate.Value, previousVnIds, previousCharacterIds);
			var result = dumpFileInfo.UpToDate ? ExitCode.ReloadLatest : ExitCode.Update;
            if (result is ExitCode.ReloadLatest or ExitCode.Update)
            {
				Console.WriteLine("Update successful, ensure Happy Reader is closed and enter 'y' to replace database file with updated or 'n' to abandon (y/n):");
                var line = Console.ReadLine()!;
                while (true)
                {
                    if (line.Equals("n", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Update abandoned.");
                        break;
                    }
                    if (line.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
						File.Delete(StaticHelpers.DatabaseFile);
                        File.Move(inProgressFile, StaticHelpers.DatabaseFile);
                        break;
                    }
                    Console.WriteLine("Save update? (y/n)");
                    line = Console.ReadLine()!;
                }
            }
			runWatch.Stop();
			RemovePastBackups(dumpFolder, dumpFileInfo);
			 return result;
		}

		private static DumpFileInfo GetLatestDump(DateTime? previousUpdate, string dumpsFolder, out Stopwatch downloadWatch)
		{
			var upToDate = !(previousUpdate == null || (DateTime.UtcNow - previousUpdate.Value).TotalDays > UpToDateDays);
			var di = Directory.CreateDirectory(dumpsFolder);
            downloadWatch = Stopwatch.StartNew();
			DownloadLatestDumpFiles(out var latestDump, out var latestVoteDump, out var anyDownload);
			downloadWatch.Stop();
            if (!anyDownload) downloadWatch = null;
			var newFileDate = latestDump.LastWriteTimeUtc;
			var targetFolder = ExtractTarToFolder(latestDump, di);
			ExtractGzToFolder(latestVoteDump, new DirectoryInfo(targetFolder));
			var result = new DumpFileInfo
			{
				LatestDumpFolder = targetFolder,
				LatestDumpFile = latestDump,
				LatestVoteDumpFile = latestVoteDump,
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

		private static void ExtractGzToFolder(FileInfo file, DirectoryInfo folder)
		{
			var destFileName = Path.Combine(folder.FullName, Path.GetFileNameWithoutExtension(file.Name));
			if (File.Exists(destFileName)) return;
			using var fileStream = file.OpenRead();
			using var zip = new GZipStream(fileStream, CompressionMode.Decompress);
			using var newFileStream = new FileStream(destFileName, FileMode.CreateNew);
			zip.CopyTo(newFileStream);
		}

		private static void DownloadLatestDumpFiles(out FileInfo databaseDump, out FileInfo voteDump, out bool anyDownload)
        {
            anyDownload = false;
			var dbDownloaded = StaticHelpers.DownloadFile(LatestDbDumpUrl, DumpFolder, null, out var dbDumpPath, ref anyDownload);
			if (!dbDownloaded) throw new InvalidOperationException("Failed to download database dump.");
			var voteDownloaded = StaticHelpers.DownloadFile(LatestVoteDumpUrl, DumpFolder, null, out var voteDumpPath, ref anyDownload);
			if (!voteDownloaded) throw new InvalidOperationException("Failed to download vote dump.");
			databaseDump = new FileInfo(dbDumpPath);
			voteDump = new FileInfo(voteDumpPath);
		}

		private static string ExtractTarToFolder(FileInfo file, DirectoryInfo folder)
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
					extractStream.CopyTo(tarStream);
					tarStream.Seek(0, SeekOrigin.Begin);
					extractStream.Dispose();
				}
				else tarStream = File.Open(fileName, FileMode.Open, FileAccess.Read);
				ExtractTar(tarStream, folderName);
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
		private static void ExtractTar(Stream stream, string outputDir)
		{
			if (Directory.Exists(outputDir)) return;
			var buffer = new byte[100];
			while (true)
			{
				stream.Read(buffer, 0, 100);
				var name = Encoding.ASCII.GetString(buffer).Trim('\0');
				if (string.IsNullOrWhiteSpace(name)) break;
				stream.Seek(24L, SeekOrigin.Current);
				stream.Read(buffer, 0, 12);
				var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);
				stream.Seek(376L, SeekOrigin.Current);
				var output = Path.Combine(outputDir, name);
				var outputDirectory = Path.GetDirectoryName(output) ?? throw new ArgumentNullException(nameof(Path.GetDirectoryName));
				if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
				if (size != 0 && !name.Equals("./", StringComparison.InvariantCulture))
				{
					using var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write);
					var buf = new byte[size];
					stream.Read(buf, 0, buf.Length);
					str.Write(buf, 0, buf.Length);
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

		private enum ExitCode
		{
			Error = -1,
			Update = 0,
			ReloadLatest = 1,
			NoUpdate = 2,
		}
	}
}
