using System;
using System.Runtime.CompilerServices;

namespace Neitri
{
	public interface ILogEnd
	{
		void Log(LogEntry logEntry);
	}

	public static class LogEndExtensions
	{
		public static void Trace<T>(this ILogEnd log, T value, [CallerMemberName] string caller = null)
		{
			log.Log(new LogEntry(
				LogEntry.LogType.Trace,
				value,
				caller
			));
		}

		public static void Debug<T>(this ILogEnd log, T value, [CallerMemberName] string caller = null)
		{
			log.Log(new LogEntry(
				LogEntry.LogType.Debug,
				value,
				caller
			));
		}

		public static void Info<T>(this ILogEnd log, T value, [CallerMemberName] string caller = null)
		{
			log.Log(new LogEntry(
				LogEntry.LogType.Info,
				value,
				caller
			));
		}

		public static void Warn<T>(this ILogEnd log, T value, [CallerMemberName] string caller = null)
		{
			log.Log(new LogEntry(
				LogEntry.LogType.Warn,
				value,
				caller
			));
		}

		public static void Error<T>(this ILogEnd log, T value, [CallerMemberName] string caller = null)
		{
			log.Log(new LogEntry(
				LogEntry.LogType.Error,
				value,
				caller
			));
		}

		public static void Fatal<T>(this ILogEnd log, T value, [CallerMemberName] string caller = null)
		{
			log.Log(new LogEntry(
				LogEntry.LogType.Fatal,
				value,
				caller
			));
		}

		public static LogScope Scope<T>(this ILogEnd log, T value)
		{
			return new LogScope(log, value.ToString());
		}

		public static LogScope StartScope<T>(this ILogEnd log, T value)
		{
			var scope = Scope<T>(log, value);
			scope.Start();
			return scope;
		}
	}

	public class LogScope : ILogEnd, IDisposable
	{
		ILogEnd parent;
		string scopeName;
		bool started;

		public LogScope(ILogEnd parent, string scopeName)
		{
			this.parent = parent;
			this.scopeName = scopeName;
		}

		public void Log(LogEntry logEntry)
		{
			parent.Log(new LogEntry(
				logEntry.Type,
				scopeName + " - " + logEntry.Message
			));
		}

		public void Start()
		{
			if (started) return;
			this.Trace("start");
			started = true;
		}

		public void Dispose()
		{
			End();
		}

		public void End(string differentEndMessage = null)
		{
			if (!started) return;
			if (differentEndMessage.IsNullOrWhiteSpace()) this.Trace("end");
			else this.Trace("end - " + differentEndMessage);
			started = false;
		}
	}

	public class LogEntry
	{
		public enum LogType
		{
			Debug,
			Warn,
			Error,
			Fatal,
			Trace,
			Info,
		}

		public LogEntry(LogType type, object message, string caller = null)
		{
			this.Type = type;
			this.message = message.ToString();
			if (!caller.IsNullOrWhiteSpace()) message += " [" + caller + "]";
		}

		public string OneLetterType => Type.ToString().Substring(0, 1);
		public LogType Type { get; }
		string message;

		public string Message
		{
			get
			{
				var indent = "";
				for (short i = 0; i < IndentLevel; i++) indent += "\t";
				return indent + message;
			}
		}

		public short IndentLevel { get; set; }
	}
}