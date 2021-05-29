using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TrainerConsole
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
        static string DataPath {
            get {
                if (_dataPath == null) {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    baseDir = baseDir.Substring(0, baseDir.IndexOf("bin"));
                    var ns = typeof(Program).Namespace;
                    _dataPath = baseDir.Replace(ns, @"CrudeOilStockPrice\Server\Data");
                }
                return _dataPath;
            }
        }
        public static string DataFile(string fileName) => Path.Combine(DataPath, fileName);
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
