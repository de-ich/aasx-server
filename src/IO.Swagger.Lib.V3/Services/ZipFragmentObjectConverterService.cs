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
using System.IO.Compression;

namespace IO.Swagger.Lib.V3.Services
{
    public class ZipFragmentObjectConverterService : IFragmentObjectConverterService
    {
        public Type[] SupportedFragmentObjectTypes => new Type[] { typeof(ZipFragmentObject) };

        public object ConvertFragmentObject(IFragmentObject fragmentObject, ContentEnum content = ContentEnum.Normal, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            if (!SupportedFragmentObjectTypes.Contains(fragmentObject.GetType()))
            {
                throw new AmlFragmentEvaluationException($"ZipFragmentObjectConverterService does not support fragment conversion for fragment object of type {fragmentObject.GetType()}!");
            }

            if (!(fragmentObject is ZipFragmentObject zipFragmentObject))
            {
                throw new ZipFragmentEvaluationException($"Unable to convert object of type {fragmentObject.GetType()} to 'ZipFragmentObject'!");
            }

            if (extent == ExtentEnum.WithBlobValue)
            {
                var stream = GetFragmentObjectAsStream(zipFragmentObject.ZipObject);
                return stream;
            }
            else
            {

                JsonConverter converter = new ZipJsonConverter(zipFragmentObject.Archive, content, extent, level);
                return JsonConvert.SerializeObject(zipFragmentObject.ZipObject, Newtonsoft.Json.Formatting.Indented, converter);
            }
        }

        private static Stream GetFragmentObjectAsStream(object fragmentObject)
        {
            if (fragmentObject is string)
            {
                throw new ZipFragmentEvaluationException($"ZipFragment represents a folder. This is not supported when 'content' is set to 'raw'!");
            }
            
            if (!(fragmentObject is ZipArchiveEntry entry))
            {
                throw new ZipFragmentEvaluationException($"Unable to convert object of type {fragmentObject.GetType()} to ZipArchiveEntry!");
            }

            var fragmentObjectStream = entry.Open();
            var decompressedStream = new MemoryStream();

            // this will decompress the file
            fragmentObjectStream.CopyTo(decompressedStream);
            decompressedStream.Position = 0;

            return decompressedStream;
        }
    }

    /**
     * A JsonConverter that converts any ZipArchiveEntry to a JSON representation. The converter refers to the parameters 'content', 'level' and 
     * 'extent' as defined by "Details of the AAS, part 2".
     * 
     * Note: The serialization algorithm for 'content=normal' is based on directly converting the XML node to JSON.
     */
    public class ZipJsonConverter : JsonConverter
    {
        char[] PathSeparators = new char[] { '/', '\\' };
        ContentEnum Content;
        ExtentEnum Extent;
        LevelEnum Level;
        ZipArchive Archive;


        public ZipJsonConverter(ZipArchive archive, ContentEnum content = ContentEnum.Normal, ExtentEnum extent = ExtentEnum.WithoutBlobValue, LevelEnum level = LevelEnum.Deep)
        {
            Archive = archive;
            Content = content;
            Extent = extent;
            Level = level;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ZipArchiveEntry).IsAssignableFrom(objectType) || typeof(string).IsAssignableFrom(objectType);
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
            ZipArchiveEntry zipEntry = value as ZipArchiveEntry;

            JContainer result;

            if (Content == ContentEnum.Normal)
            {
                result = BuildJsonRecursively(Archive, zipEntry != null ? zipEntry.FullName : value as string, Level == LevelEnum.Deep);
            }
            else if (Content == ContentEnum.Path)
            {
                string basePath = "";
                if (zipEntry != null)
                {
                    basePath = zipEntry.FullName;
                }
                else
                {
                    basePath = value as string;
                }

                result = new JArray();
                result.Add(GetChildren(Archive, basePath, Level == LevelEnum.Deep));
            }
            else
            {
                throw new XmlFragmentEvaluationException("Unsupported content modifier: " + Content);
            }

            result.WriteTo(writer);
            return;

        }

        public JObject BuildJsonRecursively(ZipArchive archive, string path, bool deep, int level = 0)
        {

            ZipArchiveEntry zipEntry = archive.GetEntry(path);

            if (zipEntry != null && zipEntry.Name != "")
            {
                // file
                return new JObject
                {
                    ["type"] = "file",
                    ["name"] = zipEntry.Name,
                    ["fullName"] = zipEntry.FullName,
                    ["compressedLength"] = zipEntry.CompressedLength,
                    ["length"] = zipEntry.Length
                };
            }
            else
            {
                // directory
                var name = path.Trim(PathSeparators).Split(PathSeparators).Last();
                JObject result = new JObject
                {
                    ["type"] = "directory",
                    ["name"] = name,
                    ["fullName"] = path
                };

                if (deep || level == 0)
                {
                    // only go to the first level of children in case we are in 'core' mode

                    try
                    {
                        var children = GetChildren(archive, path, false);

                        if (children.Count() == 0)
                        {
                            return result;
                        }

                        JArray childArray = new JArray();

                        foreach (var child in children)
                        {
                            childArray.Add(BuildJsonRecursively(archive, child, deep, level + 1));
                        }

                        result["subEntries"] = childArray;
                    }
                    catch (Exception e)
                    {
                        throw new ZipFragmentEvaluationException("An internal error occurred while trying to recurse through the ZIP file!", e);
                    }

                }

                return result;
            }
        }

        private IEnumerable<string> GetChildren(ZipArchive archive, string path, bool deep)
        {
            SortedSet<string> result = new SortedSet<string>();

            foreach (var entry in archive?.Entries)
            {
                string entryPath = entry.FullName?.TrimEnd(PathSeparators).Replace("\\\\", "\\");
                string relativeEntryPath = GetRelativePath(path, entryPath);

                if (relativeEntryPath == null)
                {
                    continue;
                }

                string[] entryParts = relativeEntryPath.Trim(PathSeparators).Split(PathSeparators);

                if (deep || entryParts.Length == 2)
                {
                    // some zip files do not contain entries for folders, hence, we add the folder manually
                    int place = entryPath.LastIndexOf(entryParts.Last());
                    result.Add(entryPath.Remove(place, entryParts.Last().Length).TrimEnd(PathSeparators));
                }

                if (deep || entryParts.Length == 1)
                {
                    result.Add(entryPath);
                }
            }

            return result;
        }

        private string GetRelativePath(string sourcePath, string targetPath)
        {
            if (targetPath == null || !targetPath.StartsWith(sourcePath) || sourcePath == targetPath)
            {
                return null;
            }

            if (sourcePath == null || sourcePath.Length == 0)
            {
                return targetPath;
            }

            string relativePath = targetPath.Substring(sourcePath.Length, targetPath.Length - sourcePath.Length);

            if (relativePath.Length == 1 && PathSeparators.Contains(relativePath[0]))
            {
                // the paths are actually the same and only differ in a trailing slash
                return null;
            }

            return relativePath;
        }

    }

}
