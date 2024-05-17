using IO.Swagger.Models;
using System.Collections.Generic;
using System.IO;
using IO.Swagger.Lib.V3.Interfaces;
using Newtonsoft.Json;
using System;
using AasxServerStandardBib.Services;
using Newtonsoft.Json.Linq;
using System.Linq;
using AasxServerStandardBib.Interfaces;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;

namespace IO.Swagger.Lib.V3.Services
{
    public class XmlFragmentObjectConverterService : IFragmentObjectConverterService
    {
        public Type[] SupportedFragmentObjectTypes => new Type[] { typeof(XmlFragmentObject) };

        public object ConvertFragmentObject(IFragmentObject fragmentObject, ContentEnum content = ContentEnum.Normal, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            if (!SupportedFragmentObjectTypes.Contains(fragmentObject.GetType()))
            {
                throw new AmlFragmentEvaluationException($"AmlFragmentObjectConverterService does not support fragment conversion for fragment object of type {fragmentObject.GetType()}!");
            }

            if (!(fragmentObject is XmlFragmentObject xmlFragmentObject))
            {
                throw new AmlFragmentEvaluationException($"Unable to convert object of type {fragmentObject.GetType()} to 'XmlFragmentObject'!");
            }

            if (level == LevelEnum.Core)
            {
                foreach (var node in xmlFragmentObject.Nodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        RemoveDeeplements(node as XElement);
                    }
                }
            }

            JsonConverter converter = new XmlJsonConverter(xmlFragmentObject.Fragment, content, extent);
            return JsonConvert.SerializeObject(xmlFragmentObject.Nodes, Newtonsoft.Json.Formatting.Indented, converter);
        }

        /**
         * A utility method that can be used to remove 'deeply nested elements' from an XML element, i.e. elements that
         * are descendants but no direct children of the given object(s).
         */
        public static void RemoveDeeplements(XNode fragmentObject)
        {

            if (fragmentObject.NodeType == XmlNodeType.Element)
            {
                // select all children's children (the elements to be deleted)
                XElement nodeToDelete;

                while ((nodeToDelete = fragmentObject.XPathSelectElement("./*/*")) != null)
                {
                    nodeToDelete.Remove();
                }
            }
        }
    }

    /**
     * A JsonConverter that converts any XElement to a JSON representation. The converter refers to the parameters 'content' and 
     * 'extent' as defined by "Details of the AAS, part 2".
     * 
     * Note: The serialization algorithm for 'content=normal' is based on directly converting the XML node to JSON.
     */
    class XmlJsonConverter : JsonConverter
    {
        string BaseXpath;
        ContentEnum Content;
        ExtentEnum Extent;

        public XmlJsonConverter(string xmlFragment, ContentEnum content = ContentEnum.Normal, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            BaseXpath = xmlFragment;
            Content = content;
            Extent = extent;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IEnumerable<XObject>).IsAssignableFrom(objectType);
        }
        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new System.NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IEnumerable<XObject> nodeList;
            try
            {
                nodeList = (IEnumerable<XObject>)value;
            }
            catch
            {
                throw new XmlFragmentEvaluationException("Unable to convert object to IEnumerable<XObject>: " + value);
            }

            JContainer result = ConvertToJson(nodeList);
            result.WriteTo(writer);

            return;

        }

        private JContainer ConvertToJson(IEnumerable<XObject> nodeList)
        {
            if (nodeList.Count() == 1)
            {
                return ConvertToJson(nodeList.First());
            }
            else
            {
                JArray result = new JArray();

                foreach (var node in nodeList)
                {
                    result.Add(ConvertToJson(node));
                }

                return result;
            }
        }

        private JContainer ConvertToJson(XObject node)
        {
            JContainer result;

            if (Content == ContentEnum.Value)
            {
                // in case of a value-only serialization, we remove all comments and namespace-related information,
                // i.e. namespace declarations, namespace prefixes as well as schema location information
                var nodeWithoutNamespaces = RemoveAllNamespacesAndComments(node);
                result = JObject.FromObject(nodeWithoutNamespaces);
            }
            else if (Content == ContentEnum.Normal)
            {
                result = JObject.FromObject(node);
            }
            else if (Content == ContentEnum.Path)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    throw new XmlFragmentEvaluationException($"Fragment evaluation did not return an Element but a(n) " + node.NodeType + ". This is not supported when returning path information!");
                }

                var elementXpath = BaseXpath == null || BaseXpath.Length == 0 || BaseXpath == "/*" ? "/" + GetLocalXpathExpression(node as XElement) : BaseXpath;

                List<string> paths = CollectChildXpathPathsRecursively(node as XElement, elementXpath);
                result = JArray.FromObject(paths);
            }
            else
            {
                throw new XmlFragmentEvaluationException("Unsupported content modifier: " + Content);
            }

            return result;
        }

        private static XObject RemoveAllNamespacesAndComments(XObject xmlObject)
        {
            if (xmlObject is XElement)
            {
                XElement original = xmlObject as XElement;
                XElement copy = new XElement(original.Name.LocalName);
                copy.Add(original.Attributes().Where(att => !att.IsNamespaceDeclaration && att.Name.LocalName != "schemaLocation").Select(att => RemoveAllNamespacesAndComments(att)));
                copy.Add(original.Nodes().Where(n => n.NodeType != XmlNodeType.Comment).Select(el => RemoveAllNamespacesAndComments(el)));

                return copy;
            }
            else if (xmlObject is XAttribute)
            {
                XAttribute original = xmlObject as XAttribute;
                return new XAttribute(original.Name.LocalName, original.Value);
            }
            else
            {
                return xmlObject;
            }
        }

        private string GetLocalXpathExpression(XElement node)
        {
            var nodeName = node.Name;
            if (nodeName.NamespaceName?.Length == 0)
            {
                // the node is not associated with a namespace, so we can simply use the local name as xPath expression
                return nodeName.LocalName;
            }

            var ns = nodeName.Namespace;
            var nsPrefix = node.GetPrefixOfNamespace(ns);

            if (nsPrefix?.Length > 0)
            {
                // there is a prefix for the namespace of the node so we can use this for the xPath expression
                return nsPrefix + ":" + nodeName.LocalName;
            }

            // the node is in the default namespace (without any prefix); hence, we need to use some special xPath syntax
            // to be able to adress the node (see https://stackoverflow.com/a/2530023)
            return "*[namespace-uri()='" + ns.NamespaceName + "' and local-name()='" + nodeName.LocalName + "']";
        }

        private List<string> CollectChildXpathPathsRecursively(XElement xmlElement, string baseXpath)
        {
            var paths = new List<string>();
            paths.Add(baseXpath);

            Dictionary<string, List<XElement>> childDict = GetChildrenSortedByName(xmlElement);

            foreach (var key in childDict?.Keys)
            {
                var values = childDict[key];

                for (int i = 0; i < values.Count; i++)
                {
                    var childXpath = baseXpath + "/" + key;
                    if (values.Count > 1)
                    {
                        // if there are multiple children with the same name, the xPath query needs to contain an array accessor
                        childXpath += "[" + (i + 1) + "]";
                    }

                    paths.AddRange(CollectChildXpathPathsRecursively(values[i], childXpath));
                }
            }

            foreach (var attribute in xmlElement.Attributes())
            {
                var attributeXpath = baseXpath + "/@" + attribute.Name;
                paths.Add(attributeXpath);
            }

            return paths;
        }

        private Dictionary<string, List<XElement>> GetChildrenSortedByName(XElement xmlElement)
        {
            var childDict = new Dictionary<string, List<XElement>>();

            foreach (var child in xmlElement?.Elements().ToList())
            {
                var childName = GetLocalXpathExpression(child);
                List<XElement> childrenWithSameName;
                if (!childDict.TryGetValue(childName, out childrenWithSameName))
                {
                    childrenWithSameName = new List<XElement>();
                }

                childrenWithSameName.Add(child);
                childDict[childName] = childrenWithSameName;
            }

            return childDict;
        }

    }


}
