using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AasxServerStandardBib.Services
{
    public class FragmentObjectRetrievalServiceDelegator : IFragmentObjectRetrievalService
    {
        IReadOnlyList<IFragmentObjectRetrievalService> serviceDelegates = new List<IFragmentObjectRetrievalService>() {
            new AmlFragmentObjectRetrievalService(),
            new XmlFragmentObjectRetrievalService(),
            new ZipFragmentObjectRetrievalService(),
            new XlsFragmentObjectRetrievalService()
        };

        public string[] SupportedFragmentTypes => serviceDelegates.SelectMany(d => d.SupportedFragmentTypes).ToArray();

        public IFragmentObject GetFragmentObject(byte[] file, string fragmentType, string fragment)
        {
            var serviceDelegate = serviceDelegates.FirstOrDefault(d => d.SupportedFragmentTypes.Contains(fragmentType));

            if (serviceDelegate != null)
            {
                return serviceDelegate?.GetFragmentObject(file, fragmentType, fragment);
            }

            throw new NotFoundException($"Unsupported fragment format. Fragment type '{fragmentType}' is not supported.");
        }
    }
}
