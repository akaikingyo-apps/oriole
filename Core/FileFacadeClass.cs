using Oriole;
using System;
using System.IO;

namespace Oriole.Library.Core
{
    public class FileFacadeClass : IFacadeClass
    {
        #region interface

        public string[] GetStaticFields()
        {
            return new string[0];
        }

        public string[] GetInstanceFields()
        {
            return new string[0];
        }

        public string[] GetStaticMethodSignatures()
        {
            return new string[] { "readText(1):1", "writeText(2):1", "appendText(2):1", "exists(1):1" };
        }

        public string[] GetInstanceMethodSignatures()
        {
            return new string[] { "openRead(1):1", "openWrite(1):1", "close(0):1", "readln(0):1", "writeln(1):1" };
        }

        public object GetField(string fieldName)
        {
            return null;
        }

        public void SetField(string fieldName, object value)
        {
        }

        public object CallMethod(string methodSignature, object[] arguments)
        {
            switch (methodSignature)
            {
                case "readText(1):1": return File.ReadAllText((string)arguments[0]);
                case "writeText(2):1": File.WriteAllText((string)arguments[0], (string)arguments[1]); return 0;
                case "appendText(2):1": File.AppendAllText((string)arguments[0], (string)arguments[1]); return 0;
                case "exists(1):1": return File.Exists((string)arguments[0]);
                case "delete(1):1": File.Delete((string)arguments[0]); return 0;
                case "copy(2):1": File.Copy((string)arguments[0], (string)arguments[1]); return 0;
                case "move(2):1": File.Move((string)arguments[0], (string)arguments[1]); return 0;
                case "openRead(1):1": this.OpenRead((string)arguments[0]); return 0;
                case "openWrite(1):1": this.OpenWrite((string)arguments[0]); return 0;
                case "close(0):1": this.Close(); return 0;
                case "readln(0):1": return this.ReadLine();
                case "writeln(1):1": this.WriteLine((string)arguments[0]); return 0;
                default: throw new Exception(string.Format("Undefined method: {0}", methodSignature));
            }
        }

        #endregion

        #region implementations

        private Stream _stream;
        private StreamWriter _writer;
        private StreamReader _reader;

        private void OpenRead(string file)
        {
            this._stream = File.Open(file, FileMode.Open);
            this._reader = new StreamReader(this._stream);
        }

        private void OpenWrite(string file)
        {
            this._stream = File.Open(file, FileMode.OpenOrCreate);
            this._writer = new StreamWriter(this._stream);
        }

        private void WriteLine(string data)
        {
            this._writer.WriteLine(data);
        }

        private string ReadLine()
        {
            return this._reader.ReadLine();
        }

        private void Close()
        {
            if (this._reader != null)
            {
                this._reader.Close();
            }
            if (this._writer != null)
            {
                this._writer.Close();
            }
        }

        #endregion
    }
}
