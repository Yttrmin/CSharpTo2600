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
        private readonly IList<(Auditor Auditor, AuditTag Tag, string Name)> Auditors = new List<(Auditor Auditor, AuditTag Tag, string Name)>();
        private readonly DateTimeOffset StartTime = DateTimeOffset.Now;

        public Auditor GetTypeMapAuditor()
        {
            if (Auditors.SingleOrDefault(a => a.Tag == AuditTag.TypeMap).Auditor is Auditor auditor)
            {
                return auditor;
            }

            return GetAuditor(nameof(TypeMap), AuditTag.TypeMap);
        }

        public Auditor GetAuditor(string name, AuditTag tag)
        {
            var auditor = new Auditor();
            Auditors.Add((auditor, tag, name));
            return auditor;
        }

        public void WriteLog(string filePath)
        {
            using (var writer = File.CreateText(filePath))
            {
                writer.WriteLine("<!DOCTYPE HTML><html><body>");
                writer.WriteLine($"<p>Log output from {StartTime.ToString("yyyy/MM/dd HH:mm:ss.fff")} to {DateTimeOffset.Now.ToString("HH:mm:ss.fff")}</p>");
                foreach (var (auditor, tag, name) in GetOrderedAuditors())
                {
                    writer.WriteLine($"<details><summary>[{tag}] {name}</summary>");
                    foreach (var (timestamp, text) in auditor.AllEntries)
                    {
                        writer.WriteLine($"<pre>{timestamp.ToString("HH:mm:ss.fff")} - {text}</pre>");
                    }
                    writer.WriteLine("</details>");
                }
                writer.WriteLine("</body></html>");
            }
        }

        private IEnumerable<(Auditor Auditor, AuditTag Tag, string Name)> GetOrderedAuditors()
        {
            return Auditors;
        }
    }

    internal class Auditor
    {
        private readonly IList<(DateTimeOffset Timestamp, string Text)> Entries = new List<(DateTimeOffset, string)>();
        public IEnumerable<(DateTimeOffset Timestamp, string Text)> AllEntries => Entries.OrderBy(e => e.Timestamp);

        public void RecordEntry(string text)
        {
            Entries.Add((DateTimeOffset.Now, text));
        }
    }

    internal enum AuditTag
    {
        None,
        TypeMap,
        TypeProcessing,
        TypeCompiling,
        Compiler
    }
}
