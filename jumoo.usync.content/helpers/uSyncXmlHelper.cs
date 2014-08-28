using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core ;
using Umbraco.Core.Models ;
using Umbraco.Core.Services;
using Umbraco.Core.Logging ; 

using System.Xml;
using System.Xml.Linq;

using System.Text.RegularExpressions;

using jumoo.usync.content.helpers;

namespace jumoo.usync.content.helpers
{
    /// <summary>
    ///  some of the genric stuff. we can do for both media and content
    /// </summary>
    public class uSyncXmlHelper
    {
        /// <summary>
        ///  export the basics of content items, the stuff shared by both
        ///  Content Items and Media Items
        /// </summary>
        /// <param name="type">the doctype or mediatype name</param>
        /// <param name="item">the item it's self</param>
        /// <param name="mapProps">are we mapping the ids?</param>
        /// <returns></returns>
        public static XElement ExportContentBase(string type, IContentBase item, bool mapProps = true)
        {
            XElement xml = new XElement(type);

            Guid _guid = ImportPairs.GetSourceGuid(item.Key);
            xml.Add(new XAttribute("guid", _guid));

            xml.Add(new XAttribute("id", item.Id));
            xml.Add(new XAttribute("nodeName", item.Name));
            xml.Add(new XAttribute("isDoc", ""));
            xml.Add(new XAttribute("updated", item.UpdateDate));

            LogHelper.Debug<uSyncXmlHelper>(">> Starting property loop");
            foreach (var property in item.Properties.Where(p => p != null))
            {
                LogHelper.Debug<uSyncXmlHelper>("Property: {0}", () => property.Alias);
                XElement propXml = null;

                try
                {
                    propXml = property.ToXml();
                }
                // if it can't be serialized
                catch
                {
                    propXml = new XElement(property.Alias, string.Empty);
                }

                string xmlVal = "";
                if (mapProps)
                {
                    xmlVal = ReplaceIdsWithGuid(GetInnerXML(propXml));
                }
                else
                {
                    xmlVal = GetInnerXML(propXml);
                }


                XElement p = XElement.Parse(string.Format("<{0}>{1}</{0}>", propXml.Name.ToString(), xmlVal), LoadOptions.PreserveWhitespace);
                LogHelper.Debug<uSyncXmlHelper>("Parse {0}", () => p.ToString());

                xml.Add(p);
            }
            LogHelper.Debug<uSyncXmlHelper>("<< finished property loop");

            return xml;
        }

  

        #region Helpers 

        /// <summary>
        ///  takes a string of XML, looks for things that might be Ids and 
        ///  attempts to find the corisponding GUID so we can bring them
        ///  accross to our other install
        /// </summary>
        /// <param name="propValue">the xml of the object</param>
        /// <returns>xml string with all the ids replaced with GUIDs</returns>
        private static string ReplaceIdsWithGuid(string propValue)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            // look for things that might be Ids
            foreach (Match m in Regex.Matches(propValue, @"\d{1,9}"))
            {
                int id ;
                
                if (int.TryParse(m.Value, out id))
                {
                    Guid? localGuid = GetGuidFromId(id);
                    if (localGuid != null)
                    {
                        if (!replacements.ContainsKey(m.Value))
                        {
                            Guid sourceGuid = ImportPairs.GetSourceGuid(localGuid.Value);
                            replacements.Add(m.Value, sourceGuid.ToString().ToUpper());
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, string> pair in replacements)
            {
                LogHelper.Debug(typeof(uSyncXmlHelper), String.Format("Updating Id's {0} > {1}", pair.Key, pair.Value));
                propValue = propValue.Replace(pair.Key, pair.Value);
            }

            LogHelper.Debug(typeof(uSyncXmlHelper), String.Format("Updated [{0}]", propValue));
            return propValue;
        }


        /// <summary>
        ///  takes an ID
        ///  and goes and gets the GUID out of umbraco
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static Guid? GetGuidFromId(int id)
        {
            LogHelper.Debug<uSyncXmlHelper>("Getting guid from id {0}", () => id);
            var cs = ApplicationContext.Current.Services.ContentService;
            // ContentService cs = new ContentService();

            IContent contentItem = cs.GetById(id);
            if (contentItem != null)
            {
                return contentItem.Key;
            }
            else
            {
                // try media ?
                IMediaService ms = ApplicationContext.Current.Services.MediaService;
                IMedia item = ms.GetById(id);
                if (item != null)
                {
                    return item.Key;
                }
                else
                {
                    return null;
                }
            }

        }

        /// <summary>
        ///  gets the innetXML from an XElement. 
        /// </summary>
        /// <param name="parent">XElement to get xml for</param>
        /// <returns>string inner xml of element</returns>
        private static string GetInnerXML(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }
        #endregion 

    }
}
