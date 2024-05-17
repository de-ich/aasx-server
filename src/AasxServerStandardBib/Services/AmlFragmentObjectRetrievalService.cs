using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxServerStandardBib.Interfaces;
using Aml.Engine.CAEX;
using Aml.Engine.AmlObjects;
using System.IO;
using Aml.Engine.CAEX.Extensions;
using System.Text.RegularExpressions;
using AasxServerStandardBib.Exceptions;
using Extensions;

namespace AasxServerStandardBib.Services
{
    public struct AmlFragmentObject : IFragmentObject
    {
        public string Fragment { get; set; }
        public CAEXBasicObject CaexObject;
        public byte[] Blob;
    }

    public class AmlFragmentObjectRetrievalService : IFragmentObjectRetrievalService
    {
        public string[] SupportedFragmentTypes => new string[] { "aml", "aml20", "aml21" };

        public IFragmentObject GetFragmentObject(byte[] file, string fragmentType, string fragment)
        {
            if (!SupportedFragmentTypes.Contains(fragmentType))
            {
                throw new AmlFragmentEvaluationException($"AmlFragmentObjectRetrievalService does not support fragment retrieval for fragment type {fragmentType}!");
            }

            var caexDocument = LoadCaexDocument(file, out AutomationMLContainer amlContainer);

            var fragmentObject = FindFragmentObject(caexDocument, fragment);

            var ret = new AmlFragmentObject() { Fragment = fragment, CaexObject = fragmentObject, Blob = null};

            if (fragmentObject is ExternalInterfaceType externalInterface && amlContainer != null)
            {
                var refURI = externalInterface.Attribute["refURI"]?.Value;

                if (refURI == null)
                {
                    throw new AmlFragmentEvaluationException("Trying to evaluate extent 'WithBlobValue' but referenced ExternalDataConnector does not provide a 'refURI' attribute!");
                }

                ret.Blob = amlContainer.GetPart(new Uri(refURI)).GetStream().ToByteArray();
            }

            return ret;
        }

        public static CAEXDocument LoadCaexDocument(byte[] amlFileContent, out AutomationMLContainer amlContainer)
        {
            amlContainer = null;
            try
            {
                // first try: 'normal' AML file
                return CAEXDocument.LoadFromBinary(amlFileContent);
            }
            catch
            {
                // second try: AMLX package
                try
                {
                    amlContainer = new AutomationMLContainer(new MemoryStream(amlFileContent));
                    return CAEXDocument.LoadFromStream(amlContainer.RootDocumentStream());
                }
                catch
                {
                    throw new AmlFragmentEvaluationException($"Unable to load AML file/container from stream.");
                }
            }
        }

        private static CAEXBasicObject FindFragmentObject(CAEXDocument caexDocument, string amlFragment)
        {
            CAEXBasicObject fragmentObject;

            string caexPath = amlFragment.Trim('/');
            if (caexPath.Length == 0)
            {
                fragmentObject = caexDocument.CAEXFile;
            }
            else
            {

                Regex guidFragmentRegEx = new Regex(@"^([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                // If the path starts with an ID, we cannot use 'FindByPath'. Hence, we need to first find the element identified by the ID, get its CAEX path, and then build the resulting CAEX path.
                if (guidFragmentRegEx.IsMatch(caexPath))
                {
                    MatchCollection idFragmentMatches = guidFragmentRegEx.Matches(amlFragment);
                    var id = idFragmentMatches[0].Groups[1].ToString();
                    var caexObject = caexDocument.FindByID(id);

                    if (caexObject == null)
                    {
                        throw new AmlFragmentEvaluationException($"Unable to locate element with ID '" + id + "' within AML file.");
                    }

                    caexPath = caexObject.GetFullNodePath() + idFragmentMatches[0].Groups[2].ToString();
                }

                fragmentObject = caexDocument.FindByPath(caexPath);
            }

            if (fragmentObject == null)
            {
                throw new AmlFragmentEvaluationException($"Unable to locate element with path '" + amlFragment + "' within AML file.");
            }

            return fragmentObject;
        }
    }

    /**
     * An exception that indicates that something went wrong while evaluating an AML20 fragment.
     */
    public class AmlFragmentEvaluationException : FragmentException
    {

        public AmlFragmentEvaluationException(string message) : base(message)
        {
        }

        public AmlFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
