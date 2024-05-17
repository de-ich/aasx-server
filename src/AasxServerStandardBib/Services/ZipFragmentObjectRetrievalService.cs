using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxServerStandardBib.Interfaces;
using System.IO;
using AasxServerStandardBib.Exceptions;
using Extensions;
using System.IO.Compression;

namespace AasxServerStandardBib.Services
{
    public struct ZipFragmentObject : IFragmentObject
    {
        public string Fragment { get; set; }
        public ZipArchive Archive;
        public object ZipObject;
    }

    public class ZipFragmentObjectRetrievalService : IFragmentObjectRetrievalService
    {
        public string[] SupportedFragmentTypes => new string[] { "zip" };

        public IFragmentObject GetFragmentObject(byte[] file, string fragmentType, string fragment)
        {
            if (!SupportedFragmentTypes.Contains(fragmentType))
            {
                throw new ZipFragmentEvaluationException($"ZipFragmentObjectRetrievalService does not support fragment retrieval for fragment type {fragmentType}!");
            }

            ZipArchive archive = LoadZipArchive(file);

            return new ZipFragmentObject() { Fragment = fragment, Archive = archive, ZipObject = FindFragmentObject(archive, fragment) };
        }

        public static ZipArchive LoadZipArchive(byte[] zipFileContent)
        {
            try
            {
                return new ZipArchive(new MemoryStream(zipFileContent));
            }
            catch
            {
                throw new ZipFragmentEvaluationException($"Unable to load ZIP archive from stream.");
            }
        }

        private static object FindFragmentObject(ZipArchive archive, string zipFragment)
        {
            zipFragment = zipFragment.Trim('/');

            if (zipFragment.Length == 0)
            {
                return zipFragment;
            }

            try
            {
                var entry = archive.GetEntry(zipFragment);

                if (entry != null)
                {
                    if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith("\\"))
                    {
                        return entry;
                    }
                    else
                    {
                        // path represents a directory
                        return zipFragment;
                    }
                }
                else
                {
                    // might be a directory that does not have its own entry
                    var entries = archive.Entries.Where(e => e.FullName.StartsWith(zipFragment));

                    if (entries.Count() > 0)
                    {
                        return zipFragment;
                    }
                    else
                    {
                        return null;
                    }

                }
            }
            catch
            {
                throw new ZipFragmentEvaluationException($"An error occurred while evaluating zip fragment '" + zipFragment);
            }
        }
    }

    /**
     * An exception that indicates that something went wrong while evaluating a ZIP fragment.
     */
    public class ZipFragmentEvaluationException : FragmentException
    {

        public ZipFragmentEvaluationException(string message) : base(message)
        {
        }

        public ZipFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
