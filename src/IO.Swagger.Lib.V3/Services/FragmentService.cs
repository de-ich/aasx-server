
using AasxServer;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using AasxServerStandardBib.Transformers;
using Extensions;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AdminShellNS.AdminShellUtil;

namespace IO.Swagger.Lib.V3.Services
{
    public class FragmentService : IFragmentService
    {
        private readonly IAppLogger<ISubmodelService> _logger;
        

        public FragmentService(IAppLogger<ISubmodelService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
        }

        public object GetFragment(byte[] file, string fragmentType, string fragment, ContentEnum content = ContentEnum.Normal, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            object? fragmentValue = null;

            switch (fragmentType)
            {
                case "aml":
                case "aml20":
                case "aml21":
                    fragmentValue = this.EvalGetAMLFragment(file, fragment, content, level, extent);
                    break;
                case "xml":
                    fragmentValue = this.EvalGetXMLFragment(file, fragment, content, level, extent);
                    break;
                case "zip":
                    fragmentValue = this.EvalGetZipFragment(file, fragment, content, level, extent);
                    break;
                case "xls":
                case "xlsx":
                    fragmentValue = this.EvalGetXLSFragment(file, fragment, content, level, extent);
                    break;
                // possibility to add support for more fragment types in the future
                default:
                    throw new NotFoundException($"Unsupported fragment format. Fragment type '{fragmentType}' is not supported.");
                    break;
            }

            if (fragmentValue == null)
            {
                throw new NotFoundException($"Unable to locate value for {fragmentType} fragment '{fragment}'");
            }

            return fragmentValue;
        }
    }
}
