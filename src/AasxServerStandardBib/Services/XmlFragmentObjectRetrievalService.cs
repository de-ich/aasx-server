using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxServerStandardBib.Interfaces;
using System.IO;
using AasxServerStandardBib.Exceptions;
using Extensions;
using System.Xml.XPath;
using System.Xml;
using System.Xml.Linq;
using Aml.Engine.CAEX;

namespace AasxServerStandardBib.Services
{
    public struct XmlFragmentObject : IFragmentObject
    {
        public string Fragment { get; set; }
        public IEnumerable<XObject> Nodes;
    }

    public class XmlFragmentObjectRetrievalService : IFragmentObjectRetrievalService
    {
        public string[] SupportedFragmentTypes => new string[] { "xml" };

        public IFragmentObject GetFragmentObject(byte[] file, string fragmentType, string fragment)
        {
            if (!SupportedFragmentTypes.Contains(fragmentType))
            {
                throw new XmlFragmentEvaluationException($"XmlFragmentObjectRetrievalService does not support fragment retrieval for fragment type {fragmentType}!");
            }

            XDocument xmlDocument = LoadXmlDocument(file);
            return new XmlFragmentObject() { Fragment = fragment, Nodes = FindFragmentObjects(xmlDocument, fragment) };
        }

        public static XDocument LoadXmlDocument(byte[] xmlFileContent)
        {
            try
            {
                return XDocument.Load(new MemoryStream(xmlFileContent));
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Unable to load XML file from stream.");
            }
        }

        private static IEnumerable<XObject> FindFragmentObjects(XDocument xmlDocument, string xmlFragment)
        {
            // select the root element if the fragment is empty
            var xPath = xmlFragment == null || xmlFragment.Length == 0 ? "/*" : xmlFragment;

            XmlNamespaceManager manager = CreateNamespaceManager(xmlDocument);

            object result;
            try
            {
                result = xmlDocument.XPathEvaluate(xPath, manager);
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Unable to compile xPath query '" + xPath + "'.");
            }

            IEnumerable<XObject> nodes;
            try
            {
                nodes = ((IEnumerable<object>)result).Cast<XObject>();
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Evaluating xPath query '" + xPath + "' did not return a node list.");
            }

            if (nodes.Count() == 0)
            {
                throw new XmlFragmentEvaluationException($"Evaluating xPath query '" + xPath + "' did not return a result.");
            }

            return nodes;
        }

        private static XmlNamespaceManager CreateNamespaceManager(XDocument xmlDocument)
        {
            XPathNavigator navigator = xmlDocument.CreateNavigator();
            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
            navigator.MoveToFollowing(XPathNodeType.Element);
            IDictionary<string, string> namespaces = navigator.GetNamespacesInScope(XmlNamespaceScope.All);
            foreach (KeyValuePair<string, string> ns in namespaces)
            {
                manager.AddNamespace(ns.Key, ns.Value);
            }

            return manager;
        }
    }

    /**
     * An exception that indicates that something went wrong while evaluating an XML fragment.
     */
    public class XmlFragmentEvaluationException : FragmentException
    {

        public XmlFragmentEvaluationException(string message) : base(message)
        {
        }

        public XmlFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
