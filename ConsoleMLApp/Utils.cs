using Microsoft.ML;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ConsoleMLApp
{
    public class Utils
    {
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

        public static string CleanNullLines(string rawFile)
        {
            var ext = Path.GetExtension(rawFile);
            var file = rawFile.Replace(ext, $"_CLEAN{ext}");

            using var reader = new StreamReader(rawFile);
            using var writer = new StreamWriter(file);

            string line = null;
            while ((line = reader.ReadLine()) != null) {
                if (line.Contains("null")) {
                    continue;
                }
                writer.WriteLine(line);
            }
            return file;
        }

        public static void SaveJsonFile<T>(string file, T any)
        {
            var json = JsonSerializer.Serialize(any);
            File.WriteAllText(file, json, Encoding.UTF8);
        }
    }
}
