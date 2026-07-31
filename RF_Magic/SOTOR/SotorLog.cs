using System;
using System.IO;
using System.Linq;
using TaleWorlds.Library;

namespace SOTOR;

public static class SotorLog
{
	public enum Level
	{
		Debug,
		Info,
		Warn,
		Error
	}

	private const int MaxSessionFiles = 30;

	public static Level MinLevel = Level.Info;

	private static readonly object WriteLock = new object();

	private static string _logDirectory;

	private static string _logFilePath;

	private static StreamWriter _writer;

	private static bool _initialized;

	public static string LogDirectory
	{
		get
		{
			EnsureInitialized();
			return _logDirectory;
		}
	}

	public static string LogFilePath
	{
		get
		{
			EnsureInitialized();
			return _logFilePath;
		}
	}

	public static void Debug(string message)
	{
		Write(Level.Debug, message);
	}

	public static void Info(string message)
	{
		Write(Level.Info, message);
	}

	public static void Warn(string message)
	{
		Write(Level.Warn, message);
	}

	public static void Error(string message)
	{
		Write(Level.Error, message);
	}

	public static void Write(Level level, string message)
	{
		if (level < MinLevel)
		{
			return;
		}
		EnsureInitialized();
		string text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
		TaleWorlds.Library.Debug.Print("[SOTOR] " + text);
		try
		{
			lock (WriteLock)
			{
				if (_writer != null)
				{
					_writer.WriteLine(text);
				}
				else
				{
					File.AppendAllText(_logFilePath, text + Environment.NewLine);
				}
			}
		}
		catch
		{
		}
	}

	private static void EnsureInitialized()
	{
		if (!_initialized)
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Mount and Blade II Bannerlord", "Logs", "SOTOR");
			try
			{
				Directory.CreateDirectory(text);
				_logDirectory = text;
				string text2 = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
				_logFilePath = Path.Combine(text, "session_" + text2 + ".log");
				File.WriteAllText(_logFilePath, $"=== SOTOR session {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
				File.WriteAllText(Path.Combine(text, "latest.txt"), _logFilePath + Environment.NewLine);
				_writer = new StreamWriter(_logFilePath, append: true)
				{
					AutoFlush = true
				};
				PruneOldSessionFiles(text);
			}
			catch
			{
				_logDirectory = Path.GetTempPath();
				_logFilePath = Path.Combine(_logDirectory, $"SOTOR_session_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
			}
			_initialized = true;
		}
	}

	private static void PruneOldSessionFiles(string logDir)
	{
		try
		{
			foreach (FileInfo item in (from path in Directory.GetFiles(logDir, "session_*.log")
				select new FileInfo(path) into file
				orderby file.LastWriteTimeUtc descending
				select file).Skip(30).ToList())
			{
				item.Delete();
			}
		}
		catch
		{
		}
	}
}
