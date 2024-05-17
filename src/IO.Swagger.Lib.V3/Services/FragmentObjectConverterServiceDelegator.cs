using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Services;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class FragmentObjecConverterServiceDelegator : IFragmentObjectConverterService
{
    IReadOnlyList<IFragmentObjectConverterService> serviceDelegates = new List<IFragmentObjectConverterService>() {
            new AmlFragmentObjectConverterService(),
            new XmlFragmentObjectConverterService(),
            new ZipFragmentObjectConverterService(),
            new XlsFragmentObjectConverterService()
        };

    public Type[] SupportedFragmentObjectTypes => serviceDelegates.SelectMany(d => d.SupportedFragmentObjectTypes).ToArray();

    public object ConvertFragmentObject(IFragmentObject fragmentObject, ContentEnum content = ContentEnum.Normal, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
    {
        var serviceDelegate = serviceDelegates.FirstOrDefault(d => d.SupportedFragmentObjectTypes.Contains(fragmentObject.GetType()));

        if (serviceDelegate != null)
        {
            return serviceDelegate?.ConvertFragmentObject(fragmentObject, content, level, extent);
        }

        throw new NotFoundException($"Unsupported fragment object format. Fragment type '{fragmentObject.GetType()}' is not supported.");
    }
}