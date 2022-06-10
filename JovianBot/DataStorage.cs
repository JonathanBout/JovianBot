using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Jovian
{
    public class DataStorage
    {
        public string StoragePath { get; init; }
        string Directory => Path.GetDirectoryName(StoragePath) ?? "";
        string FileName { get; init; }

        const string FileExtension = ".LDS";
        public DataStorage(string path, string name)
        {
            FileName = name;
            StoragePath = Path.Combine(path, FileName + FileExtension);
            System.IO.Directory.CreateDirectory(Directory);
            bool fileExists = File.Exists(StoragePath);
            File.Open(StoragePath, FileMode.OpenOrCreate).Dispose();
            if (!fileExists)
                WriteText("[");
        }

        public IEnumerable<DataChunk<T>>? GetChunks<T>() where T : notnull
        {
            return GetChunks()?.Select(x => new DataChunk<T>(x.Id, (T)x.Data)).ToList() ?? new List<DataChunk<T>>();
        }

        public IEnumerable<DataChunk>? GetChunks()
        {
            string content = GetText();
            return JsonConvert.DeserializeObject<IEnumerable<DataChunk>>(content);
        }

        public DataChunk? GetChunkByID(string ID)
        {
            return GetChunks()?.First(x => x.Id == ID);
        }

        public DataChunk<T>? GetChunkByID<T>(string ID) where T : notnull
        {
            return (DataChunk<T>?)GetChunkByID(ID);
        }

        public string GetText()
        {
            using (var stream = OpenStreamRead(FileMode.Open))
            {
                return stream.ReadToEnd().TrimEnd(',') + "]";
            }
        }

        public void WriteData(DataChunk chunk)
        {
            WriteText(chunk.ToJSON() + ",");
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
        }
    }

    public class DataChunk<T> : DataChunk where T : notnull
    {
        public new T Data => (T)base.Data;
        public DataChunk(string id, T data) : base(id, data) { }
    }

    public class DataChunk
    {
        public string Id { get; init; }

        public object Data { get; init; }

        public DataChunk(string id, object data)
        {
            Id = id;
            Data = data;
        }

        public virtual string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
