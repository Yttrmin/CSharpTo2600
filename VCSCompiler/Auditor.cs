﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace VCSCompiler
{
    internal class AuditorManager
    {
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        private static readonly Func<long> GetTicks = () => Stopwatch.ElapsedTicks;
        public static AuditorManager Instance { get; } = new AuditorManager();
        private readonly IList<Auditor> Auditors = new List<Auditor>();
        private readonly DateTimeOffset StartTime = DateTimeOffset.Now;

        public Auditor GetAuditor(string name, AuditTag tag)
        {
            var auditor = new Auditor(name, tag, GetTicks);
            Auditors.Add(auditor);
            return auditor;
        }
        
        // TODO - Provide a way to get the HTML string, writing to a file won't be best for an online implementation.
        public void WriteLog(string filePath)
        {
            using (var writer = File.CreateText(filePath))
            {
                writer.WriteLine("<!DOCTYPE HTML><html>");
                writer.WriteLine(@"<style>
pre { margin-top: 0em; margin-bottom: 0em; margin-left: 1em; }
div { margin-left: 1em; }
</style>");
                writer.WriteLine("<body>");
                writer.WriteLine($"<p>Log output from {StartTime.ToString("yyyy/MM/dd HH:mm:ss.fff")} to {DateTimeOffset.Now.ToString("HH:mm:ss.fff")}</p>");
                foreach (var auditor in GetOrderedAuditors())
                {
                    writer.WriteLine(GetAuditorString(auditor));
                }
                writer.WriteLine("</body></html>");
            }

            string GetAuditorString(Auditor auditor)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"<details><summary>[{auditor.Tag}] {WebUtility.HtmlEncode(auditor.Name)}</summary>");
                foreach (var (timestamp, content) in auditor.AllEntries)
                {
                    if (content is string text)
                    {
                        string prefix = string.Empty;
                        if (timestamp is long ticks)
                        {
                            var milliseconds = ((double)ticks / Stopwatch.Frequency) * 1000;
                            prefix = $"{milliseconds.ToString("00000.0000")}ms - ";
                        }
                        stringBuilder.AppendLine($"<pre>{prefix}{WebUtility.HtmlEncode(text)}</pre>");
                    }
                    else if (content is Auditor subAuditor)
                    {
                        stringBuilder.AppendLine($"<div>{GetAuditorString(subAuditor)}</div>");
                    }
                }
                stringBuilder.AppendLine("</details>");
                return stringBuilder.ToString();
            }
        }

        private IEnumerable<Auditor> GetOrderedAuditors()
        {
            return Auditors.Where(a => a.Tag != AuditTag.MethodCompiling && a.Tag != AuditTag.MethodProcessing);
        }
    }

    internal sealed class Auditor
    {
        private readonly IList<(long? Timestamp, object Content)> Entries = new List<(long?, object)>();
        private readonly Func<long> GetTicks;

        public string Name { get; }
        public AuditTag Tag { get; }
        public IEnumerable<(long? Timestamp, object Content)> AllEntries => Entries.ToArray();
        public bool HasEntries => Entries.Any();

        public Auditor(string name, AuditTag tag, Func<long> getTicks)
        {
            Name = name;
            Tag = tag;
            GetTicks = getTicks;
        }

        public void RecordEntry(string text, bool logTimestamp = true)
        {
            Entries.Add((logTimestamp ? GetTicks() : (long?)null, text));
        }

        public void RecordAuditor(Auditor auditor)
        {
            // TODO - May want to move this into WriteLog(), to cover the case of pre-emptively recording an auditor before it has entires so it can be included in a crash.
            if (auditor.HasEntries)
            {
                Entries.Add((GetTicks(), auditor));
            }
        }
    }

    internal enum AuditTag
    {
        None,
        Compiler,
        TypeMap,
        TypeProcessing,
        TypeCompiling,
        MethodProcessing,
        MethodCompiling
    }
}
