using System;
using System.Collections.Generic;

using System.IO; 
using System.Xml;
using System.Xml.Linq;

namespace jumoo.usync.content.helpers
{
    /// <summary>
    ///  this and sort pairs are very similar you could 
    ///  probibly just template them out and make it neater
    /// </summary>
    public class ImportPairs
    {
        public static Dictionary<Guid, Guid> pairs = new Dictionary<Guid, Guid>();
        public static string pairFile;

        static ImportPairs()
        {
            pairFile = Path.Combine(FileHelper.uSyncRoot, "_import.xml");
        }


        public static void SavePair(Guid id, Guid key)
        {
            if (pairs.ContainsKey(id))
            {
                pairs.Remove(id); 
            }
            pairs.Add(id, key);
        }

        public static void LoadFromDisk()
        {
            // blank it. 
            pairs = new Dictionary<Guid, Guid>(); 


            if (File.Exists(pairFile))
            {
                XElement source = XElement.Load(pairFile);

                var sourcePairs = source.Descendants("pair");
                foreach (var pair in sourcePairs)
                {
                    pairs.Add(
                        Guid.Parse(pair.Attribute("id").Value),
                        Guid.Parse(pair.Attribute("guid").Value));
                }
            }
        }

        public static void SaveToDisk()
        {
            if (File.Exists(pairFile))
                File.Delete(pairFile);

            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
            doc.AppendChild(dec);

            XmlElement data = doc.CreateElement("usync.data");
            XmlElement content = doc.CreateElement("content");

            foreach (KeyValuePair<Guid, Guid> pair in pairs)
            {
                XmlElement p = doc.CreateElement("pair");
                p.SetAttribute("id", pair.Key.ToString());
                p.SetAttribute("guid", pair.Value.ToString());

                content.AppendChild(p);
            }

            data.AppendChild(content);
            doc.AppendChild(data);
            doc.Save(pairFile);
        }
    }
}

