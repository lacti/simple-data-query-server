using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DataQueryServer
{
    public interface IData
    {
    }

    public enum DataFileType
    {
        Csv, Binary
    }

    public class DataFile<T>
    {
        private readonly List<T> _data = new List<T>();
 
        public T Load(string filePath, DataFileType fileType)
        {
            switch (fileType)
            {
                case DataFileType.Csv:
                    return LoadFromCsv(filePath);
                case DataFileType.Binary:
                    return LoadFromBinary(filePath);
            }
            throw new NotImplementedException();
        }

        private T LoadFromCsv(string filePath)
        {
            return default(T);
        }

        private T LoadFromBinary(string filePath)
        {
            return default(T);
        }

        public void Save(string filePath, DataFileType fileType)
        {
            switch (fileType)
            {
                case DataFileType.Csv:
                    SaveToCsv(filePath);
                    break;
                case DataFileType.Binary:
                    SaveToBinary(filePath);
                    break;
            }
            throw new NotImplementedException();
        }

        private void SaveToCsv(string filePath)
        {
            
        }

        private void SaveToBinary(string filePath)
        {
            
        }
    }
}
