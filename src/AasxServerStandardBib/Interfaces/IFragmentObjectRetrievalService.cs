using System;
using System.Collections.Generic;
using System.IO;

namespace AasxServerStandardBib.Interfaces
{
    public interface IFragmentObjectRetrievalService
    {
        string[] SupportedFragmentTypes { get; }

        IFragmentObject GetFragmentObject(byte[] file, string fragmentType, string fragment);
    }
}
