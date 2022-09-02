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
        #region Public API

        public static Dictionary<string, LanguageEntry> GetEntries()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            bool mergeFiles = false;

            if (LanguageGenerator.Instance.WorkingDirectory != null)
            {
                FileInfo sourceFile = LanguageGenerator.Instance.WorkingDirectory.GetFiles(LanguageGenerator.OutputFileName).FirstOrDefault();

                if(sourceFile != null)
                {
                    IEnumerable<string> items = File.ReadLines(sourceFile.FullName);

                    foreach(string item in items)
                    {
                        string[] split = item.Split("=", StringSplitOptions.RemoveEmptyEntries);

                        if (split.Length != 2)
                            continue;

                        values.Add(split[0].Trim(), split[1].Trim());
                    }
                }

                if (values.Any()) 
                {
                    mergeFiles = Utilities.GetConfirmation(String.Format("A previously processed language dictionary was found in the directory [green]{0}[/].  " +
                        "Would you like to check for any new entries to be merged with this file?", LanguageGenerator.Instance.WorkingDirectory.FullName));
                }
            }

            List<FileInfo> files = new List<FileInfo>();
            List<KeyValuePair<string, string>> duplicates = new List<KeyValuePair<string, string>>();

            if (!values.Any() || mergeFiles)
            {
                string prompt = "Enter the directory that contains the [green]" + Constants.Extentions.Gxt2 + "[/] files: ";
                DirectoryInfo dir = Settings.GetDirectory(Settings.Node.LanguageFilesPath, prompt);

                files = dir.GetFiles("*" + Constants.Extentions.Gxt2).ToList();
            }

            files.ForEach(x =>
            {
                GXT2 test = new GXT2(x.OpenRead());

                foreach (KeyValuePair<uint, byte[]> data in test.DataItems)
                {
                    string key = data.Key.ToString("X8");
                    string value = Encoding.UTF8.GetString(data.Value);

                    if (!values.ContainsKey(key))
                        values.Add(key, value);
                    else
                        duplicates.Add(new KeyValuePair<string, string>(key, value));
                }
            });

            Dictionary<string, List<string>> duplicateValues = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, string> dupe in duplicates)
            {
                string currentValue = values[dupe.Key];

                if (!string.Equals(currentValue, dupe.Value))
                {
                    if(!duplicateValues.ContainsKey(dupe.Key))
                        duplicateValues.Add(dupe.Key, new List<string>());

                    List<string> dupeList = duplicateValues[dupe.Key];

                    if (!dupeList.Contains(dupe.Value))
                        dupeList.Add(dupe.Value);
                }
            }

            Dictionary<string, LanguageEntry> entries = new Dictionary<string, LanguageEntry>();

            foreach (KeyValuePair<string, string> item in values)
            {
                LanguageEntry entry = new LanguageEntry(item, duplicateValues);

                if (!entries.ContainsKey(entry.Hash))
                    entries.Add(entry.Hash, entry);
            }

            return entries;
        }

        #endregion

        #region Helper Classes

        public class LanguageEntry
        {
            [TableColumn]
            public string Hash { get; set; }
            [TableColumn]
            public string CurrentValue { get; set; }
            [TableColumn]
            public string OtherValuesFound { get; set; }
            public List<string> OtherValues { get; private set; }

            public LanguageEntry(KeyValuePair<string, string> item, Dictionary<string, List<string>> duplicateValues)
            {
                Hash = item.Key.StartsWith("0x") ? item.Key : string.Format("0x{0}", item.Key);
                CurrentValue = item.Value;

                List<string> otherValues = new List<string>();

                duplicateValues.TryGetValue(item.Key, out otherValues);
                OtherValues = otherValues;

                if (OtherValues != null && OtherValues.Any())
                    OtherValuesFound = String.Join(",", OtherValues);
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
