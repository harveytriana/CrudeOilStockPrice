// ==================================
// BlazorSpread.net
// ===================================
using Microsoft.ML;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TrainerConsole
{
    public class Utils
    {
        public static string FilterNoiseLines(string rawFile)
        {
            var ext = Path.GetExtension(rawFile);
            var file = rawFile.Replace(ext, $"_CLEAN{ext}");

            using var reader = new StreamReader(rawFile);
            using var writer = new StreamWriter(file);

            string line = null;
            while ((line = reader.ReadLine()) != null) {
                if (line.Contains("null") || line.Contains("NaN")) {// the day nas not data
                    continue;
                }
                if (line[10..].IndexOf('-') > 10) {// filter negative values
                    continue;
                }
                writer.WriteLine(line);
            }
            return file;
        }

        public static void SaveJsonFile<T>(string file, T any, bool writeIdented = false)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIdented,
            };
            var json = JsonSerializer.Serialize(any, options);
            File.WriteAllText(file, json, Encoding.UTF8);
        }

        #region Development Data Path on BlazorServer\Data
        static string _dataPath;
        public static string DataPath(string fileName)
        {
            if (_dataPath == null) {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                baseDir = baseDir.Substring(0, baseDir.IndexOf("bin"));
                var ns = typeof(Program).Namespace;
                _dataPath = baseDir + "Data";
            }
            return Path.Combine(_dataPath, fileName);
        }
        #endregion

        #region Development Data Path on Data
        static string _publishPath;
        public static string PublishPath(string fileName)
        {
            if (_publishPath == null) {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                baseDir = baseDir.Substring(0, baseDir.IndexOf("bin"));
                var ns = typeof(Program).Namespace;
                _publishPath = baseDir.Replace(ns, @"CrudeOilStockPrice\Server\Data");
            }
            return Path.Combine(_publishPath, fileName);
        }
        #endregion

        public static void LogDataView(IDataView dataView, string title = "", int maxRows = 50)
        {
            Console.WriteLine($"\n{title}");

            var preview = dataView.Preview();
            foreach (var col in preview.Schema) {
                if (col.Name == "Features") {
                    continue;
                }
                Console.Write($"{col.Name}\t|");
            }
            var i = 0;
            foreach (var row in preview.RowView) {
                Console.WriteLine();
                foreach (var col in row.Values) {
                    if (col.Key == "Features") {
                        continue;
                    }
                    Console.Write($"{col.Value}\t|");
                }
                if (i++ >= maxRows) {
                    Console.WriteLine("\nContinue...");
                    break;
                }
            }
            Console.WriteLine("\nCount: {0}\n", preview.RowView.Length);
        }
    }
}
