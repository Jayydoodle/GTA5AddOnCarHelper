using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA5AddOnCarHelper
{
    public static class LanguageDictionary
    {
        #region Properties

        private static readonly Lazy<Dictionary<string, string>> _instance = new Lazy<Dictionary<string, string>>(() => Create());
        public static Dictionary<string, string> Instance => _instance.Value;
        public static Dictionary<string, List<string>> DuplicateValues { get; private set; }

        #endregion

        #region Public API

        public static void PrintToConsole()
        {
            List<LanguageEntry> entries = new List<LanguageEntry>();

            foreach(string key in Instance.Keys)
            {
                LanguageEntry entry = new LanguageEntry(key);
            }
        }

        #endregion

        #region Private API

        private static Dictionary<string, string> Create()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            List<KeyValuePair<string, string>> duplicates = new List<KeyValuePair<string, string>>();

            DirectoryInfo dir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleMetaFilesPath);
            List<FileInfo> files = dir.GetFiles("*" + Constants.Extentions.Gxt2).ToList();

            files.ForEach(x =>
            {
                GXT2 test = new GXT2(x.OpenRead());

                foreach (KeyValuePair<uint, byte[]> data in test.DataItems)
                {
                    string key = data.Key.ToString("X8");
                    string value = Encoding.UTF8.GetString(data.Value);

                    if (!dict.ContainsKey(key))
                        dict.Add(key, value);
                    else
                        duplicates.Add(new KeyValuePair<string, string>(key, value));
                }
            });

            DuplicateValues = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, string> dupe in duplicates)
            {
                string currentValue = dict[dupe.Key];

                if (!string.Equals(currentValue, dupe.Value))
                {
                    if(!DuplicateValues.ContainsKey(dupe.Key))
                        DuplicateValues.Add(dupe.Key, new List<string>());

                    List<string> dupeList = DuplicateValues[dupe.Key];

                    if (!dupeList.Contains(dupe.Value))
                        dupeList.Add(dupe.Value);
                }
            }

            return dict;
        }

        #endregion

        #region Helper Classes

        private class LanguageEntry
        {
            public string Hash { get; set; }
            public string CurrentValue { get; set; }
            public List<string> OtherValuesFound { get; private set; }

            public LanguageEntry(string hash)
            {
                Hash = hash;
                CurrentValue = LanguageDictionary.Instance[hash];

                List<string> otherValues = new List<string>();
                DuplicateValues.TryGetValue(hash, out otherValues);
                OtherValuesFound = otherValues;
            }
        }

        private class GXT2
        {
            public const int Header = 0x47585432; //"GXT2"

            public Dictionary<uint, byte[]> DataItems;

            public void AddStringItem(string name, string str)
            {
                DataItems.Add(Utilities.CreateHash(name), System.Text.Encoding.UTF8.GetBytes(str));
            }
            public void AddStringItem(uint hash, string str)
            {
                DataItems.Add(hash, System.Text.Encoding.UTF8.GetBytes(str));
            }
            public GXT2()
            {
                DataItems = new Dictionary<uint, byte[]>();
            }

            public GXT2(Stream xIn)
            {
                this.DataItems = new Dictionary<uint, byte[]>();
                BinaryReader reader = new BinaryReader(xIn);
                reader.ReadInt32(); //header
                int itemcount = reader.ReadInt32();
                for (int i = 0; i < itemcount; i++)
                {
                    uint hash = reader.ReadUInt32();
                    int location = reader.ReadInt32();

                    long tempLoc = reader.BaseStream.Position;

                    reader.BaseStream.Position = location;
                    List<byte> thisItemBytes = new List<byte>();
                    while (true)
                    {
                        byte b = reader.ReadByte();
                        if (b == 0)
                            break;
                        thisItemBytes.Add(b);

                    }
                    reader.BaseStream.Position = tempLoc;
                    this.DataItems.Add(hash, thisItemBytes.ToArray());
                }
            }

            public void WriteToStream(Stream xOut)
            {
                BinaryWriter writer = new BinaryWriter(xOut);

                writer.Write(Header);
                writer.Write(DataItems.Count);

                long startTablePos = writer.BaseStream.Position;
                foreach (KeyValuePair<uint, byte[]> datas in this.DataItems)
                {
                    writer.Write(datas.Key);
                    writer.Write(0);
                }

                writer.Write(Header); //end header
                long endHeaderLoc = writer.BaseStream.Position;
                writer.Write(0);

                int indexer = 0;
                foreach (KeyValuePair<uint, byte[]> datas in DataItems)
                {
                    byte[] thisItemData = datas.Value;
                    long _thisItemDataWriteLoc = writer.BaseStream.Position;
                    writer.BaseStream.Position = startTablePos + (indexer * 8) + 4;
                    writer.Write((int)_thisItemDataWriteLoc);
                    writer.BaseStream.Position = _thisItemDataWriteLoc;
                    writer.Write(thisItemData, 0, thisItemData.Length);
                    writer.Write((byte)0);
                    indexer++;
                }

                long _finalEndLoc = writer.BaseStream.Position;
                writer.BaseStream.Position = endHeaderLoc;
                writer.Write((int)_finalEndLoc);
                writer.Flush();
            }
        }

        #endregion
    }
}
