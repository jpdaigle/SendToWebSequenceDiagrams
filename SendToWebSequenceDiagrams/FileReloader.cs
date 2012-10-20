using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SendToWebSequenceDiagrams
{
    class FileReloader
    {
        FileSystemWatcher _fsWatcher;
        Action<string> _handleContents;

        public FileReloader(string path, Action<string> handleNewContents)
        {
            _handleContents = handleNewContents;
            FileInfo f = new FileInfo(path);
            if (!f.Exists) throw new Exception("Path does not exist: " + path);
            _fsWatcher = new FileSystemWatcher(f.DirectoryName, f.Name);
            _fsWatcher.Changed += new FileSystemEventHandler(FileChanged);
            _fsWatcher.EnableRaisingEvents = true;
            Console.WriteLine("dbg> created reloader for {0}", path);

            // Initial fire
            FileChanged(_fsWatcher, new FileSystemEventArgs(WatcherChangeTypes.Changed, f.DirectoryName, f.Name));
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // need to open in shared mode
                using (var fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var textReader = new StreamReader(fileStream))
                {
                    var allText = textReader.ReadToEnd();
                    Console.WriteLine("dbg> file {0} reloaded {1} length", e.FullPath, allText.Length);
                    _handleContents(allText);
                }
            }
            catch (IOException ioe)
            {
                Console.WriteLine(ioe);
            }
        }


    }
}
