using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace VCSCompiler
{
    internal class AuditorManager
    {
        public static AuditorManager Instance { get; } = new AuditorManager();
        private readonly IList<Auditor> Auditors = new List<Auditor>();
        private readonly DateTimeOffset StartTime = DateTimeOffset.Now;

        public Auditor GetTypeMapAuditor()
        {
            if (Auditors.SingleOrDefault(a => a.Tag == AuditTag.TypeMap) is Auditor auditor)
            {
                return auditor;
            }

            return GetAuditor(nameof(TypeMap), AuditTag.TypeMap);
        }

        public Auditor GetAuditor(string name, AuditTag tag)
        {
            var auditor = new Auditor(name, tag);
            Auditors.Add(auditor);
            return auditor;
        }

        public void WriteLog(string filePath)
        {
            using (var writer = File.CreateText(filePath))
            {
                writer.WriteLine("<!DOCTYPE HTML><html><body>");
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
                stringBuilder.AppendLine($"<details><summary>[{auditor.Tag}] {auditor.Name}</summary>");
                foreach (var (timestamp, content) in auditor.AllEntries)
                {
                    if (content is string text)
                    {
                        stringBuilder.AppendLine($"<pre style=\"margin-top: 0em; margin-bottom: 0em; margin-left: 1em;\">{timestamp.ToString("HH:mm:ss.fff")} - {text}</pre>");
                    }
                    else if (content is Auditor subAuditor)
                    {
                        stringBuilder.AppendLine($"<div style=\"margin-left: 1em;\">{GetAuditorString(subAuditor)}</div>");
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
        private readonly IList<(DateTimeOffset Timestamp, object Content)> Entries = new List<(DateTimeOffset, object)>();

        public string Name { get; }
        public AuditTag Tag { get; }
        public IEnumerable<(DateTimeOffset Timestamp, object Content)> AllEntries => Entries.OrderBy(e => e.Timestamp);
        public bool HasEntries => Entries.Any();

        public Auditor(string name, AuditTag tag)
        {
            Name = name;
            Tag = tag;
        }

        public void RecordEntry(string text)
        {
            // TODO - Should probably sanitize input before an online version is made.
            Entries.Add((DateTimeOffset.Now, text));
        }

        public void RecordAuditor(Auditor auditor)
        {
            if (auditor.HasEntries)
            {
                Entries.Add((DateTimeOffset.Now, auditor));
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
