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
using ClosedXML.Excel;

namespace IO.Swagger.Lib.V3.Services
{
    public class XlsFragmentObjectConverterService : IFragmentObjectConverterService
    {
        public Type[] SupportedFragmentObjectTypes => new Type[] { typeof(XlsFragmentObject) };

        public object ConvertFragmentObject(IFragmentObject fragmentObject, ContentEnum content = ContentEnum.Normal, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            if (!SupportedFragmentObjectTypes.Contains(fragmentObject.GetType()))
            {
                throw new XlsFragmentEvaluationException($"XlsFragmentObjectConverterService does not support fragment conversion for fragment object of type {fragmentObject.GetType()}!");
            }

            if (!(fragmentObject is XlsFragmentObject xlsFragmentObject))
            {
                throw new AmlFragmentEvaluationException($"Unable to convert object of type {fragmentObject.GetType()} to 'XlsFragmentObject'!");
            }

            JsonConverter converter = new XlsJsonConverter(content, extent);
            return JsonConvert.SerializeObject(xlsFragmentObject.XLObject, Newtonsoft.Json.Formatting.Indented, converter);
        }
    }

    /**
     * A JsonConverter that converts an XLWorkbook, XLWorksheet, XLCell, XLCells or a raw value to a JSON representation. The converter refers to the parameters 'content' and 
     * 'extent' as defined by "Details of the AAS, part 2".
     */
    class XlsJsonConverter : JsonConverter
    {
        ContentEnum Content;
        ExtentEnum Extent;

        public XlsJsonConverter(ContentEnum content = ContentEnum.Normal, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            Content = content;
            Extent = extent;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(XLWorkbook).IsAssignableFrom(objectType) || typeof(IXLWorksheet).IsAssignableFrom(objectType) ||
                typeof(IXLCell).IsAssignableFrom(objectType) || typeof(IXLCells).IsAssignableFrom(objectType) ||
                typeof(XLCellValue).IsAssignableFrom(objectType) || typeof(string).IsAssignableFrom(objectType);
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

            JContainer result;

            bool valueOnly = Content == ContentEnum.Value;

            if (value is XLWorkbook workbook)
            {
                result = CompileJson(workbook, valueOnly);
            }
            else if (value is IXLWorksheet worksheet)
            {
                result = CompileJson(worksheet, valueOnly);
            }
            else if (value is IXLCells cells)
            {
                result = CompileJson(cells, valueOnly);
            }
            else if (value is IXLCell cell)
            {
                result = CompileJson(cell, valueOnly);
            }
            else if (value is XLCellValue cellValue)
            {
                result = CompileJson(cellValue, valueOnly);
            }
            else if (value is string)
            {
                result = new JObject
                {
                    ["value"] = value as string
                };

                if (!valueOnly)
                {
                    result["type"] = "formula";
                }
            }
            else
            {
                throw new XlsFragmentEvaluationException("Unable to convert object to a suitbale type: " + value);
            }

            result.WriteTo(writer);
            return;

        }

        private JContainer CompileJson(XLWorkbook workbook, bool valueOnly)
        {
            JObject result = new JObject();
            if (!valueOnly)
            {
                result["type"] = "workbook";
                result["author"] = workbook.Author;
                result["worksheets"] = new JArray();
            }

            foreach (var worksheet in workbook.Worksheets)
            {
                var worksheetJson = CompileJson(worksheet, valueOnly);
                if (valueOnly)
                {
                    result[worksheet.Name] = worksheetJson;
                }
                else
                {
                    (result["worksheets"] as JArray).Add(worksheetJson);
                }
            }

            return result;
        }

        private JContainer CompileJson(IXLWorksheet worksheet, bool valueOnly)
        {
            JContainer cells = CompileJson(worksheet.Cells(), valueOnly);

            if (valueOnly)
            {
                return cells;
            }

            JObject result = new JObject
            {
                ["type"] = "worksheet",
                ["name"] = worksheet.Name
            };

            result["cells"] = cells;

            return result;
        }

        private JContainer CompileJson(IXLCells cells, bool valueOnly)
        {
            JContainer result;
            if (valueOnly)
            {
                result = new JObject();

                foreach (var cell in cells)
                {
                    result[cell.Address.ToString()] = cell.Value.ToString();
                }

            }
            else
            {
                result = new JArray();

                foreach (var cell in cells)
                {
                    result.Add(CompileJson(cell, valueOnly));
                }
            }

            return result;
        }

        private JObject CompileJson(IXLCell cell, bool valueOnly)
        {
            if (valueOnly)
            {
                return new JObject
                {
                    ["value"] = cell.Value.ToString()
                };
            }

            return new JObject
            {
                ["type"] = "cell",
                ["address"] = cell.Address.ToString(),
                ["formula"] = cell.FormulaA1.ToString(),
                ["value"] = cell.Value.ToString(),
            };
        }

        private JObject CompileJson(XLCellValue cellValue, bool valueOnly)
        {
            if (valueOnly)
            {
                return new JObject
                {
                    ["value"] = cellValue.ToString()
                };
            }

            return new JObject
            {
                ["type"] = "cellValue",
                ["value"] = cellValue.ToString(),
            };
        }

    }


}
