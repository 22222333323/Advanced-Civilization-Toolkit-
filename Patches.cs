using System;
using System.Reflection;
using UnityEngine;

namespace WorldBoxMod
{
    public class LocalizationFix
    {
    
        public static void Apply()
        {
            try
            {
                // Try to find the LocalizedTextManager type (not strictly required,
                // but helpful for diagnostics in future).
                var lt = FindTypeByName("LocalizedTextManager");

                // Install a targeted log filter that suppresses only the
                // "LocalizedTextManager: missing text" error messages while
                // forwarding everything else to the original handler.
                var logger = Debug.unityLogger;
                if (logger != null)
                {
                    var originalHandler = logger.logHandler;
                    if (!(originalHandler is FilteredLogHandler))
                    {
                        logger.logHandler = new FilteredLogHandler(originalHandler);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Debug.LogException(ex);
                }
                catch
                {
                }
            }
        }

        private static Type FindTypeByName(string typeName)
        {
            // Try direct lookup first
            var t = Type.GetType(typeName);
            if (t != null) return t;

            // Search loaded assemblies for a matching type name
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    var tt = asm.GetType(typeName);
                    if (tt != null) return tt;

                    // fallback: compare simple name
                    foreach (var candidate in asm.GetTypes())
                    {
                        if (candidate.Name == typeName) return candidate;
                    }
                }
                catch
                {
                    // ignore assemblies we can't inspect
                }
            }
            return null;
        }
    }
}

    internal class FilteredLogHandler : ILogHandler
    {
        private readonly ILogHandler _inner;

        public FilteredLogHandler(ILogHandler inner)
        {
            _inner = inner ?? Debug.unityLogger.logHandler;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            try
            {
                if (logType == LogType.Error || logType == LogType.Exception)
                {
                    string message = (args != null && args.Length > 0) ? string.Format(format, args) : format;
                    if (!string.IsNullOrEmpty(message) && message.Contains("LocalizedTextManager: missing text"))
                    {
                        return; // suppress this specific error message
                    }
                }
            }
            catch
            {
                // If filtering fails, fall through to forwarding
            }
            _inner.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            _inner.LogException(exception, context);
        }
    }