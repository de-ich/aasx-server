using IO.Swagger.Models;
using System.Collections.Generic;
using System.IO;

namespace IO.Swagger.Lib.V3.Interfaces
{
    public interface IFragmentService
    {
        object GetFragment(byte[] file, string fragmentType, string fragment, ContentEnum content = ContentEnum.Normal, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue);
    }
}
