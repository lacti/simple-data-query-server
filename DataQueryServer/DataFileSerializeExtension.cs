using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace DataQueryServer
{
    public interface IData
    {
    }

    public static class DataFileSerializeExtension
    {
        private static readonly ConcurrentDictionary<string, MethodGenerator.CsvParser> CsvParsers = new ConcurrentDictionary<string, MethodGenerator.CsvParser>();
        private static readonly ConcurrentDictionary<string, MethodGenerator.CsvWriter> CsvWriters = new ConcurrentDictionary<string, MethodGenerator.CsvWriter>();
        private static readonly ConcurrentDictionary<string, MethodGenerator.BinParser> BinParsers = new ConcurrentDictionary<string, MethodGenerator.BinParser>();
        private static readonly ConcurrentDictionary<string, MethodGenerator.BinWriter> BinWriters = new ConcurrentDictionary<string, MethodGenerator.BinWriter>();

        public static void PrepareCsvBinParserWriter(this Type type)
        {
            CsvParsers.TryAdd(type.FullName, MethodGenerator.GenerateCsvParser(type));
            CsvWriters.TryAdd(type.FullName, MethodGenerator.GenerateCsvWriter(type));
            BinParsers.TryAdd(type.FullName, MethodGenerator.GenerateBinParser(type));
            BinWriters.TryAdd(type.FullName, MethodGenerator.GenerateBinWriter(type));
        }

        public static List<T> LoadFromCsv<T>(this List<T> data, string filePath, bool includeHeader = true) where T : IData
        {
            return LoadFromCsv(data, filePath, includeHeader, Encoding.UTF8);
        }

        public static List<T> LoadFromCsv<T>(this List<T> data, string filePath, bool includeHeader, Encoding encoding) where T : IData
        {
            var parser = CsvParsers[typeof(T).FullName];
            using (var textReader = new StreamReader(filePath, encoding))
            using (var csvReader = new CsvReader(textReader, new CsvConfiguration { HasHeaderRecord = includeHeader}))
            {
                while (csvReader.Read())
                {
                    var record = csvReader.CurrentRecord;
                    data.Add((T)parser(record));
                }
            }
            return data;
        }

        public static void SaveToCsv<T>(this List<T> data, string filePath) where T : IData
        {
            SaveToCsv(data, filePath, Encoding.UTF8);
        }

        public static void SaveToCsv<T>(this List<T> data, string filePath, Encoding encoding) where T : IData
        {
            var writer = CsvWriters[typeof (T).FullName];
            using (var stream = new StreamWriter(filePath, false, encoding))
            {
                foreach (var tuple in data)
                {
                    stream.WriteLine(writer(tuple));
                }
            }
        }

        public static List<T> LoadFromBin<T>(this List<T> data, string filePath) where T : IData
        {
            var parser = BinParsers[typeof(T).FullName];
            var bytes = File.ReadAllBytes(filePath);
            var offset = 0;
            while (offset < bytes.Length)
            {
                var tuple = (T)parser(bytes, ref offset);
                data.Add(tuple);
            }
            return data;
        }

        public static void SaveToBin<T>(this List<T> data, string filePath) where T : IData
        {
            var writer = BinWriters[typeof (T).FullName];
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write, 1048576))
            using (var binaryWriter = new BinaryWriter(fileStream))
            {
                foreach (var tuple in data)
                {
                    writer(binaryWriter, tuple);
                }
            }

        }
    }
}
