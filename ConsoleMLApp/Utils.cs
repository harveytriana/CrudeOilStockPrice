using Microsoft.ML;
using System;
using System.Collections.Generic;
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

        public static void SaveJsonFile<T>(string file, T any, bool writeIdented = false)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIdented,
            };
            var json = JsonSerializer.Serialize(any, options);
            File.WriteAllText(file, json, Encoding.UTF8);
        }

        #region Development Data Path
        static string _p;
        public static string DataPath {
            get {
                if (_p == null) {
                    var s = AppDomain.CurrentDomain.BaseDirectory;
                    _p = s.Substring(0, s.IndexOf("bin")) + "Data\\";
                }
                return _p;
            }
        }
        #endregion


        //? BUG? System.Text.Json.JsonException HResult=0x80131500
        // FAIL
        // var z = Utils.GetListFromJsonFile<List<StockPricePrediction>>(DATA_PATH + "PredictionsPartial.json");
        // OK
        // var z =JsonSerializer.Deserialize<List<StockPricePrediction>>(File.ReadAllText(DATA_PATH + "PredictionsPartial.json"));
        public static List<T> GetListFromJsonFile<T>(string file)
        {
            var s = File.ReadAllText(file);
            var z = JsonSerializer.Deserialize<List<T>>(s);
            return z;
        }


    }
}
