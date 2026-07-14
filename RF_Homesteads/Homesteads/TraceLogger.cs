using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MCM.Abstractions.Base.Global;

namespace Homesteads;

internal static class TraceLogger
{
	private const int MaxLogLines = 4000;

	private const int TrimCheckInterval = 200;

	private static readonly object Sync = new object();

	private static readonly HashSet<string> OnceKeys = new HashSet<string>();

	private static int sequence;

	private static bool sessionStarted;

	private static int writesSinceTrim;

	private static string LogDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Mount and Blade II Bannerlord", "logs");

	public static string LogFilePath => Path.Combine(LogDirectoryPath, "HomesteadsReloaded.trace.log");

	public static void StartSession(string reason)
	{
		lock (Sync)
		{
			try
			{
				Directory.CreateDirectory(LogDirectoryPath);
				if (!sessionStarted)
				{
					File.AppendAllText(LogFilePath, Environment.NewLine + "===== New Homesteads Reloaded Session =====" + Environment.NewLine);
					TrimLogFileToLimit();
					sessionStarted = true;
				}
			}
			catch
			{
				return;
			}
		}
		Write("Session", reason);
	}

	public static void Write(string source, string message)
	{
		MCMSettings? instance = GlobalSettings<MCMSettings>.Instance;
		if (instance == null || !instance.EnableDebugLogging)
		{
			return;
		}
		lock (Sync)
		{
			try
			{
				Directory.CreateDirectory(LogDirectoryPath);
				int num = ++sequence;
				string contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{num:D5}] [T{Thread.CurrentThread.ManagedThreadId}] {source}: {message}{Environment.NewLine}";
				File.AppendAllText(LogFilePath, contents);
				if (++writesSinceTrim >= 200)
				{
					writesSinceTrim = 0;
					TrimLogFileToLimit();
				}
			}
			catch
			{
			}
		}
	}

	public static void WriteOnce(string key, string source, string message)
	{
		lock (Sync)
		{
			if (!OnceKeys.Add(key))
			{
				return;
			}
		}
		Write(source, message);
	}

	private static void TrimLogFileToLimit()
	{
		if (File.Exists(LogFilePath))
		{
			string[] array = File.ReadAllLines(LogFilePath);
			if (array.Length > 4000)
			{
				string[] array2 = new string[4000];
				Array.Copy(array, array.Length - 4000, array2, 0, 4000);
				File.WriteAllLines(LogFilePath, array2);
			}
		}
	}
}
