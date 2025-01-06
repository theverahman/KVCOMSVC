using System;
using System.IO;

namespace KVCOMSVC
{
    public class ServiceExecutableSource
    {
        private readonly string _executablePath;

        public ServiceExecutableSource(string executablePath)
        {
            _executablePath = executablePath;
        }

        public string GetExecutablePath()
        {
            return _executablePath;
        }

        public string GetExecutableFolder()
        {
            return Path.GetDirectoryName(_executablePath);
        }
    }
}