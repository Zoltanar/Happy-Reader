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
		private static DateTime? _previousLogTime;
		
		public bool LogVerbose { get; set; }

		private static string TimeString
		{
			get
			{
				var dt = DateTime.Now;
				var timePassedString = string.Empty;
				if (_previousLogTime.HasValue)
				{
					var timePassed = dt - _previousLogTime.Value;
					var ts = timePassed.TotalSeconds < 10 ? $"{(int)timePassed.TotalMilliseconds:D0} ms" : $"{(int)timePassed.TotalSeconds:D0} s ";
					timePassedString = $" ({ts,7})";
				}
				_previousLogTime = dt;
				var timeString = $"[{dt,-12:hh:mm:ss:fff}{timePassedString}] ";
				return timeString;
			}
		}
		
		public MultiLogger(string logFolder)
		{
			Directory.CreateDirectory(logFolder);
			_logFolder = logFolder;
			using var process = Process.GetCurrentProcess();
			_creatingProcessId = process.Id;
		}

		public void Verbose(string text)
		{
			if (!LogVerbose) return;
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
				if (counter > 5)
				{
					//throw new IOException("Logfile is locked!");
					Console.WriteLine(messageWithTime);
					return;
				}
				Thread.Sleep(25);
			}
			if (stream == null) throw new ArgumentNullException(nameof(stream), "Log file stream was null.");
			using var writer = new StreamWriter(stream);
			foreach (var message in messages)
			{
				var messageWithTime = TimeString + message;
				Console.WriteLine(messageWithTime);
				writer.WriteLine(messageWithTime);
			}
		}

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