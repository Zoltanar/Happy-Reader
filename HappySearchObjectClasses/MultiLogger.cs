using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Happy_Apps_Core
{
	public class MultiLogger
	{
		private readonly string _logFile;

		public MultiLogger(string logFile)
		{
			_logFile = logFile;
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
		/// Print exception to Debug and write it to log file.
		/// </summary>
		/// <param name="exception">Exception to be written to file</param>
		/// <param name="source">Source of error, CallerMemberName by default</param>
		public void ToFile(Exception exception,[CallerMemberName] string source = null)
		{
			Debug.WriteLine($"Source: {source}");
			Debug.WriteLine(exception.Message);
			Debug.WriteLine(exception.StackTrace);
			int counter = 0;
			while (IsFileLocked(new FileInfo(_logFile)))
			{
				counter++;
				if (counter > 5) throw new IOException("Logfile is locked!");
				Thread.Sleep(25);
			}
			using (var writer = new StreamWriter(_logFile, true))
			{
				writer.WriteLine($"Source: {source}");
				writer.WriteLine(exception.Message);
				writer.WriteLine(exception.StackTrace);
			}
		}

		/// <summary>
		/// Print message to Debug and write it to log file.
		/// </summary>
		/// <param name="message">Message to be written</param>
		/// <param name="logDebug">Print to debug, true by default</param>
		public void ToFile(string message, bool logDebug = true)
		{
			if (logDebug) Debug.Print(TimeString + message);
			int counter = 0;
			while (IsFileLocked(new FileInfo(_logFile)))
			{
				counter++;
				if (counter > 5) throw new IOException("Logfile is locked!");
				Thread.Sleep(25);
			}
			using (var writer = new StreamWriter(_logFile, true)) writer.WriteLine(message);
		}

		private static string TimeString => $"[ {DateTime.Now.ToString("hh:mm:ss:fff").PadRight(13)}] ";

		/// <summary>
		/// Check if file is locked,
		/// </summary>
		/// <param name="file">File to be checked</param>
		/// <returns>Whether file is locked</returns>
		private static bool IsFileLocked(FileInfo file)
		{
			FileStream stream = null;

			try
			{
				stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				return true;
			}
			finally
			{
				stream?.Close();
			}
			return false;
		}
	}
}