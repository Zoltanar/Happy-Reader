using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Happy_Apps_Core
{
	public class MultiLogger
	{
		private readonly string _logFolder;
		private readonly int _creatingProcessId;

		public MultiLogger(string logFolder)
		{
			Directory.CreateDirectory(logFolder);
			_logFolder = logFolder;
			using var process = Process.GetCurrentProcess();
			_creatingProcessId = process.Id;
		}

		[Conditional("LOGVERBOSE")]
		public void Verbose(string text)
		{
			Debug.WriteLine(TimeString + text);
		}

		[Conditional("DEBUG")]
		public void ToDebug(string text)
		{
			Debug.WriteLine(TimeString + text);
		}

		/// <summary>
		/// Print exception to Console and write it to log file.
		/// </summary>
		/// <param name="exception">Exception to be written to file</param>
		/// <param name="source">Source of error, <see cref="CallerMemberNameAttribute"/> by default</param>
		public void ToFile(Exception exception, [CallerMemberName] string source = null)
		{
			ToFile($"Source: {source}", exception.ToString());
		}

		/// <summary>
		/// Print message to Console and write it to log file.
		/// </summary>
		/// <param name="messages">Messages to be written</param>
		public void ToFile(params string[] messages)
		{
			var file = Path.Combine(_logFolder, $"Happy-Apps-{DateTime.UtcNow:yyyy-MM-dd}-{_creatingProcessId:X6}.log");
			int counter = 0;
			Stream stream;
			while (IsFileLocked(new FileInfo(file), out stream))
			{
				counter++;
				if (counter > 5) throw new IOException("Logfile is locked!");
				Thread.Sleep(25);
			}
			if(stream == null) throw new ArgumentNullException(nameof(stream), "Log file stream was null.");
			using var writer = new StreamWriter(stream);
			foreach (var message in messages)
			{
				var time = TimeString;
				Console.WriteLine(time + message);
				writer.WriteLine(time + message);
			}
		}
		

		private static string TimeString => $"[{DateTime.Now.ToString("hh:mm:ss:fff").PadRight(12)}] ";

		/// <summary>
		/// Check if file is locked
		/// </summary>
		/// <returns>Whether file is locked</returns>
		private static bool IsFileLocked(FileInfo file, out Stream stream)
		{
			stream = null;
			try
			{
				stream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				stream.Seek(0, SeekOrigin.End);
			}
			catch (IOException)
			{
				stream?.Close();
				stream = null;
				return true;
			}
			return false;
		}
	}
}