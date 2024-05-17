using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxServerStandardBib.Interfaces;
using System.IO;
using AasxServerStandardBib.Exceptions;
using Extensions;
using ClosedXML.Excel;

namespace AasxServerStandardBib.Services
{
    public struct XlsFragmentObject : IFragmentObject
    {
        public string Fragment { get; set; }
        public object XLObject;
    }

    public class XlsFragmentObjectRetrievalService : IFragmentObjectRetrievalService
    {
        public string[] SupportedFragmentTypes => new string[] { "xls", "xlsx" };

        public IFragmentObject GetFragmentObject(byte[] file, string fragmentType, string fragment)
        {
            if (!SupportedFragmentTypes.Contains(fragmentType))
            {
                throw new XlsFragmentEvaluationException($"XlsFragmentObjectRetrievalService does not support fragment retrieval for fragment type {fragmentType}!");
            }

            XLWorkbook workbook = LoadXlsDocument(file);
            return new XlsFragmentObject() { Fragment = fragment, XLObject = FindFragmentObject(workbook, fragment) };
        }

        private static XLWorkbook LoadXlsDocument(byte[] xlsFileContent)
        {
            try
            {
                return new XLWorkbook(new MemoryStream(xlsFileContent));
            }
            catch
            {
                throw new XlsFragmentEvaluationException($"Unable to load XLS file from stream.");
            }
        }

        private static object FindFragmentObject(XLWorkbook workbook, string xlsFragment)
        {
            var xlsExpression = xlsFragment.Trim('/');

            try
            {
                if (xlsExpression.Length == 0)
                {
                    // provided expression references the complete workbook
                    return workbook;
                }
                else if (xlsExpression.StartsWith("="))
                {
                    // provided expression is a formula
                    return workbook.Evaluate(xlsExpression.Substring(1));
                }
                else if (xlsExpression.Contains(':'))
                {
                    // provided expression is reference to a cell range
                    return workbook.Cells(xlsExpression);
                }
                else if (xlsExpression.Contains('!'))
                {
                    // provided expression is a reference to a single cell
                    return workbook.Cell(xlsExpression);
                }
                else
                {
                    // provided expression is a reference to a worksheet
                    return workbook.Worksheet(xlsExpression);
                }

            }
            catch
            {
                throw new XlsFragmentEvaluationException("An error occurred while evaluating Excel expression '" + xlsExpression + "'!");
            }

        }
    }

    /**
     * An exception that indicates that something went wrong while evaluating an XLS fragment.
     */
    public class XlsFragmentEvaluationException : FragmentException
    {

        public XlsFragmentEvaluationException(string message) : base(message)
        {
        }

        public XlsFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
