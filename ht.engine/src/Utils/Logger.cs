using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Engine.Utils
{
    /// <summary>
    /// Utility that can be used for logging messages, at the moment it just redirect to the
    /// console-output but it in future it can be extended to write to a log-file,
    /// add severity etc...
    /// </summary>
    public sealed class Logger
    {
        private class LogStream : IDisposable
        {
            private const int INDENT_SIZE = 4;

            private readonly StringBuilder stringBuilder = new StringBuilder();
            private int indent;

            public void Indent() => indent++;
            public void Outdent() => indent = System.Math.Max(indent - 1, 0);

            public void AppendLine(string text)
            {
                stringBuilder.Append(' ', indent * INDENT_SIZE);
                stringBuilder.AppendLine(text);
            }

            public void AppendList<T>(IList<T> list)
            {
                AppendLine("[");
                Indent();
                for (int i = 0; i < list.Count; i++)
                    AppendLine(list[i].ToString() + (i < (list.Count - 1) ? "," : string.Empty));
                Outdent();
                AppendLine("]");
            }

            public void Dispose() => Console.Write(stringBuilder.ToString());
        }

        public void LogList<T>(string context, string message, IList<T> list)
        {
            using(var stream = new LogStream())
            {
                stream.AppendLine($"[{context}] {message}");
                stream.AppendList(list);
            }
        }

        public void Log(string context, string message) => Log($"[{context}] {message}");
        public void Log(string message) => Console.WriteLine(message);
    }
}