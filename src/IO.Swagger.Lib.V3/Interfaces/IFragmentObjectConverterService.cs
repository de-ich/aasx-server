using AasxServerStandardBib.Interfaces;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IFragmentObjectConverterService
    {
        Type[] SupportedFragmentObjectTypes { get; }

        object ConvertFragmentObject(IFragmentObject fragmentObject, ContentEnum content = ContentEnum.Normal, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue);
    }
}
