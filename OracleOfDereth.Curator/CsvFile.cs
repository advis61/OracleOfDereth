using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OracleOfDereth.Curator
{
    internal sealed class CsvTable
    {
        public List<string> Headers { get; } = new List<string>();
        public List<Dictionary<string, string>> Rows { get; } = new List<Dictionary<string, string>>();

        public bool HasColumn(string name) => Headers.Any(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

        public string ActualHeader(string name) => Headers.FirstOrDefault(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

        public static CsvTable Read(string path)
        {
            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                var records = Parse(reader).ToList();
                if (records.Count == 0) throw new InvalidDataException($"CSV is empty: {path}");

                var table = new CsvTable();
                foreach (string header in records[0])
                {
                    string clean = (header ?? "").Trim();
                    if (clean.Length == 0) throw new InvalidDataException($"CSV has a blank header: {path}");
                    if (table.Headers.Any(h => string.Equals(h, clean, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException($"CSV has duplicate header '{clean}': {path}");
                    table.Headers.Add(clean);
                }

                for (int recordIndex = 1; recordIndex < records.Count; recordIndex++)
                {
                    List<string> record = records[recordIndex];
                    if (record.All(string.IsNullOrWhiteSpace)) continue;
                    if (record.Count != table.Headers.Count)
                        throw new InvalidDataException(
                            $"CSV record {recordIndex + 1} has {record.Count} fields; expected {table.Headers.Count}: {path}");
                    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < table.Headers.Count; i++)
                        row[table.Headers[i]] = i < record.Count ? record[i].Trim() : "";
                    table.Rows.Add(row);
                }
                return table;
            }
        }

        public void Write(string path)
        {
            ValidateShape();
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(directory);
            string temp = Path.Combine(directory, $"quests-{Guid.NewGuid():N}.tmp");
            string backup = path + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff");

            try
            {
                using (var writer = new StreamWriter(temp, false, new UTF8Encoding(false)))
                {
                    writer.WriteLine(string.Join(",", Headers.Select(Escape)));
                    foreach (var row in Rows)
                        writer.WriteLine(string.Join(",", Headers.Select(h => Escape(Get(row, h)))));
                }

                if (File.Exists(path))
                {
                    File.Copy(path, backup, false);
                    try
                    {
                        File.Delete(path);
                        File.Move(temp, path);
                    }
                    catch
                    {
                        if (!File.Exists(path) && File.Exists(backup)) File.Copy(backup, path);
                        throw;
                    }
                }
                else File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        public static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out string value) ? value ?? "" : "";

        private void ValidateShape()
        {
            if (Headers.Count == 0) throw new InvalidDataException("CSV has no headers.");
            if (Headers.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("CSV has a blank header.");
            if (Headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != Headers.Count)
                throw new InvalidDataException("CSV has duplicate headers.");

            for (int i = 0; i < Rows.Count; i++)
            {
                if (Rows[i] == null) throw new InvalidDataException($"CSV row {i + 2} is missing.");
                foreach (string header in Headers)
                    if (!Rows[i].ContainsKey(header))
                        throw new InvalidDataException($"CSV row {i + 2} is missing field '{header}'.");
            }
        }

        private static string Escape(string value)
        {
            value = value ?? "";
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        private static IEnumerable<List<string>> Parse(TextReader reader)
        {
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            while (true)
            {
                int next = reader.Read();
                if (next < 0)
                {
                    if (quoted) throw new InvalidDataException("CSV ends inside a quoted field.");
                    if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); yield return row; }
                    yield break;
                }

                char c = (char)next;
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (reader.Peek() == '"') { reader.Read(); field.Append('"'); }
                        else quoted = false;
                    }
                    else field.Append(c);
                }
                else if (c == '"' && field.Length == 0) quoted = true;
                else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && reader.Peek() == '\n') reader.Read();
                    row.Add(field.ToString()); field.Clear();
                    yield return row; row = new List<string>();
                }
                else field.Append(c);
            }
        }
    }
}
