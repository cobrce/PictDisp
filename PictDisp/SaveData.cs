using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PictDisp
{
    public class SaveData
    {
        internal Dictionary<string, string> EntriesDict = new Dictionary<string, string>();

        public bool Dark;
        public List<Entry> Entries = new List<Entry>();

        // use when read
        internal void ReadEntries()
        {
            EntriesDict =  Entries.ToDictionary(x => x.Path, x => x.LastFile);
        }

        // use when write
        internal void SetEntries()
        {
            Entries.Clear();
            Entries.AddRange((from k in EntriesDict select new Entry() { Path = k.Key, LastFile = k.Value }).ToList());
        }
    }
    public class Entry
    {
        public string Path;
        public string LastFile;
    }
}
