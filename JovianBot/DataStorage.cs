﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace DeltaDev.JovianBot
{
    public class DataStorage<T> where T : notnull
    {
        public string StoragePath { get; init; }
        string StorageDirectory => Path.GetDirectoryName(StoragePath) ?? "";
        string FileName { get; init; }

        const string FileExtension = ".LDS";

        public List<DataChunk<T>> currentStorage = new List<DataChunk<T>>();
        public DataStorage(string path, string name)
        {
            FileName = name;
            StoragePath = Path.Combine(path, FileName + FileExtension);
            Reload();
        }

        public void Reload()
        {
            Directory.CreateDirectory(StorageDirectory);
            bool fileExists = File.Exists(StoragePath);
            File.Open(StoragePath, FileMode.OpenOrCreate).Dispose();
            if (!fileExists)
                WriteText("[");
            currentStorage = GetChunks()?.ToList() ?? new List<DataChunk<T>>();
        }

        public IEnumerable<DataChunk<T>>? GetChunks()
        {
            string content = GetText();
            return JsonConvert.DeserializeObject<IEnumerable<DataChunk<T>>>(content);
        }

        public DataChunk<T>? GetChunkByID(string ID)
        {
            return GetChunks()?.First(x => x.Key == ID);
        }

        public string GetText()
        {
            using (var stream = OpenStreamRead(FileMode.Open))
            {
                return stream.ReadToEnd().TrimEnd(',') + "]";
            }
        }

        public bool WriteData(DataChunk<T> chunk)
        {
            if (currentStorage.Any(x => x.Key == chunk.Key))
            {
                return false;
            }
            currentStorage.Add(chunk);
            WriteText(chunk.ToJSON() + ",");
            return true;
        }

        void WriteText(string text)
        {
            using (var stream = OpenStreamWrite(FileMode.Append))
            {
                stream.Write(text);
            }
        }

        public StreamWriter OpenStreamWrite(FileMode mode)
        {
            return new StreamWriter(File.Open(StoragePath, mode, FileAccess.Write));
        }

        public StreamReader OpenStreamRead(FileMode mode)
        {
            return new StreamReader(File.Open(StoragePath, mode, FileAccess.Read));
        }

        public void Clear()
        {
            File.WriteAllText(StoragePath, "[");
            Reload();
        }
    }

    public class DataChunk<T> : DataChunk where T : notnull
    {
        public new T Value => (T)base.Value;
        public DataChunk(string id, T data) : base(id, data) { }
    }

    public class DataChunk
    {
        public string Key { get; init; }

        public object Value { get; init; }

        public DataChunk(string id, object data)
        {
            Key = id;
            Value = data;
        }

        public virtual string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
