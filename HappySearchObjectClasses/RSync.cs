using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Happy_Apps_Core;

namespace DatabaseDumpReader
{
	internal static class RSync
	{
		private const string RsyncExecutableName = @"rsync.exe";
		private const string Switches = @"-rptv --del";

		public static bool Run(string rsyncUrl, string destinationFolderPath, out string errorMessage)
		{
			var filesCopied = new List<string>();
			try
			{
				if (!ValidatePathsAndFiles(destinationFolderPath, out errorMessage, out var originalRsyncFile, out var destinationFolder)) return false;
				if (!CopyRSyncFiles(ref errorMessage, destinationFolder, originalRsyncFile, filesCopied)) return false;
				Debug.Assert(destinationFolder.Parent != null, "destinationFolder.Parent != null");
				var psi = new ProcessStartInfo("cmd.exe")
				{
					Arguments = $"/C \"\"rsync\" {Switches} \"{rsyncUrl}\" \"{destinationFolder.Name}\"\"",
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					WorkingDirectory = destinationFolder.Parent.FullName
				};
				StaticHelpers.Logger.ToFile("Starting Rsync and waiting for completion...");
				var process = Process.Start(psi);
				if (process == null)
				{
					errorMessage = "Failed to start rsync";
					return false;
				}
				if (process.HasExited)
				{
					errorMessage = "rsync process exited immediately.";
					return false;
				}
				process.OutputDataReceived += Process_OutputDataReceived;
				process.ErrorDataReceived += Process_OutputDataReceived;
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();
				errorMessage = "No errors.";
				return true;
			}
			catch (Exception ex)
			{
				errorMessage = ex.ToString();
				return false;
			}
			finally
			{
				DeleteFiles(filesCopied);
			}
		}

		private static bool CopyRSyncFiles(
			ref string errorMessage,
			DirectoryInfo destinationFolder,
			FileInfo originalRsyncFile,
			ICollection<string> filesCopied)
		{
			try
			{
				var rsyncDirectory = destinationFolder.Parent;
				// ReSharper disable once PossibleNullReferenceException
				foreach (var file in originalRsyncFile.Directory.GetFiles())
				{
					// ReSharper disable once PossibleNullReferenceException
					var copyDestination = Path.Combine(rsyncDirectory.FullName, file.Name);
					if (File.Exists(copyDestination)) continue;
					file.CopyTo(copyDestination);
					filesCopied.Add(copyDestination);
				}
				return true;
			}
			catch (Exception ex)
			{
				errorMessage = $"Failed to temporarily copy rsync files to destination path: {ex}";
				return false;
			}
		}

		private static bool ValidatePathsAndFiles(
			string destinationFolderPath,
			out string errorMessage,
			out FileInfo originalRsyncFile,
			out DirectoryInfo destinationFolder)
		{
			var assemblyDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
			// ReSharper disable once PossibleNullReferenceException
			originalRsyncFile = new FileInfo(Path.Combine(assemblyDirectory.FullName, "Rsync", RsyncExecutableName));
			destinationFolder = new DirectoryInfo(Path.GetFullPath(destinationFolderPath));
			if (destinationFolder.Parent == null)
			{
				errorMessage = "Destination folder cannot be a drive (or root path).";
				return false;
			}
			if (!destinationFolder.Exists)
			{
				try
				{
					destinationFolder.Create();
				}
				catch (IOException ex)
				{
					errorMessage = $"Failed to create destination folder '{destinationFolder.FullName}': {ex}";
					return false;
				}
			}
			if (!originalRsyncFile.Exists)
			{
				errorMessage = $"rsync executable not found: {originalRsyncFile}";
				return false;
			}

			errorMessage = "No errors.";
			return true;
		}

		private static void DeleteFiles(IEnumerable<string> filesCopied)
		{
			foreach (var file in filesCopied)
			{
				try
				{
					new FileInfo(file).Delete();
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile($"Failed to delete file '{file}': {ex}");
				}
			}
		}

		private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.Data)) return;
			Console.WriteLine(e.Data);
		}
	}
}
