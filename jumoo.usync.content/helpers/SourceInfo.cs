using System;

using System.Collections ; 
using System.Collections.Generic;

using System.IO ; 
using System.Xml;
using System.Xml.Linq ;

namespace jumoo.usync.content.helpers
{
    /// <summary>
    ///  persists some basic info to and from the disk, it is used to track renames and moves.
    ///  
    ///  We do this because we've made the choice to have readable content folders, if we just used
    ///  the content ID as the folder name, renames wouldn't matter (but moves still would)
    /// </summary>
    public class SourceInfo 
    {
        static Dictionary<Guid, Tuple<string, int>> source = new Dictionary<Guid, Tuple<string, int>>();
        static string sourceFile;

        static SourceInfo()
        {
            sourceFile = Path.Combine(FileHelper.uSyncTemp, "_uSyncSource.xml");
            Load(); 
        }

        public static bool IsNew()
        {
            //
            // easier to read than return !File.Exisits(sourceFile) 
            //
            if (File.Exists(sourceFile))
                return false;

            return true;
        }

        public static void Add(Guid guid, string name, int parent)
        {
            if ( source.ContainsKey(guid) )
                source.Remove(guid) ; 

            source.Add(guid, new Tuple<string,int>(name, parent));
        }

        public static void Save()
        {
            if (File.Exists(sourceFile))
                File.Delete(sourceFile);

            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
            doc.AppendChild(dec);

            XmlElement data = doc.CreateElement("usync.data");
            XmlElement content = doc.CreateElement("content");

            foreach (KeyValuePair<Guid, Tuple<string, int>> info in source)
            {
                XmlElement p = doc.CreateElement("info");
                p.SetAttribute("guid", info.Key.ToString());
                p.SetAttribute("name", info.Value.Item1);
                p.SetAttribute("parent", info.Value.Item2.ToString());

                content.AppendChild(p);
            }

            data.AppendChild(content);
            doc.AppendChild(data);
            doc.Save(sourceFile);
        }

        public static void Load()
        {
            // blank it
            source = new Dictionary<Guid, Tuple<string, int>>();

            if (File.Exists(sourceFile))
            {
                XElement xmlInfo = XElement.Load(sourceFile);

                var sourcePairs = xmlInfo.Descendants("info");
                foreach (var pair in sourcePairs)
                {
                    source.Add( 
                        Guid.Parse(pair.Attribute("guid").Value),
                        new Tuple<string,int>(
                            pair.Attribute("name").Value,
                            int.Parse(pair.Attribute("parent").Value)
                            )
                        );
                }
            }


        }


        public static string GetName(Guid guid)
        {
            if (source.ContainsKey(guid))
                return source[guid].Item1;

            return null;
        }

        public static int? GetParent(Guid guid)
        {
            if (source.ContainsKey(guid))
                return source[guid].Item2;

            return null;
        }

    }

}
