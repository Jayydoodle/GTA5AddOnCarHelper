using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CustomSpectreConsole
{
    public class XMLFile
    {
        #region Properties

        public string SourceFileName { get; set; }
        public XDocument Document { get; private set; }

        #endregion

        #region Constructor

        private XMLFile() { }

        #endregion

        #region Public API

        public XElement TryGetNode(string node, Func<XElement, bool> predicate = null)
        {
            return predicate != null ? Document?.Descendants(node).Where(predicate).FirstOrDefault()
                                     : Document?.Descendants(node).FirstOrDefault();
        }

        public List<XElement> TryGetNodes(string node, Func<XElement, bool> predicate = null)
        {
            return predicate != null ? Document?.Descendants(node).Where(predicate).ToList()
                                     : Document?.Descendants(node).ToList();
        }

        public void Save(string path)
        {
            Document.Save(path);
        }

        #endregion

        #region Static API

        public static XMLFile Load(string fileName)
        {
            XDocument doc = null;

            try 
            {
                string xmlText = File.ReadAllText(fileName);
                xmlText = xmlText.Replace("&", "&amp;");

                TextReader reader = new StringReader(xmlText);

                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings { 
                    CheckCharacters = false, 
                    IgnoreComments = true,
                    ConformanceLevel = ConformanceLevel.Fragment
                };

                using (XmlReader xmlReader = XmlReader.Create(reader, xmlReaderSettings))
                {
                    // Load our XmlDocument
                    xmlReader.MoveToContent();
                    doc = XDocument.Load(xmlReader);
                }
            }
            catch (Exception e) 
            { 
                return null; 
            }

            XMLFile file = new XMLFile();
            file.Document = doc;
            file.SourceFileName = fileName;

            return file;
        }

        #endregion

    }
}
