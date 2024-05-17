using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Exceptions
{
    public class FragmentException : Exception
    {
        public FragmentException(string message) : base($"Unable to retrieve fragment. Reason: {message}")
        {
        }

        public FragmentException(string message, Exception innerException) : base($"Unable to retrieve fragment. Reason: {message}", innerException)
        {
        }
    }
}
