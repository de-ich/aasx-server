using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;

namespace AasxServerBlazor.Data
{
    /// <summary>
    /// An interface that needs to be implemented by extensions that allow to browse into files in order to show the file contents within the Tree/TreePage
    /// </summary>
    public interface IAASServiceExtension
    {
        /// <summary>
        /// Whether this extension supports browsing into the given file.
        /// </summary>
        public bool IsSuitableFor(File file);

        /// <summary>
        /// Recursively generates the child items for the given file and adds them to the given fileItem.
        /// </summary>
        public abstract void CreateItems(Item fileItem, System.IO.Stream fileStream, string fileRestURL);
        /// <summary>
        /// Method 'TreePage.ViewNodeID(...)' will delegate to this implementation for items created by the specific extension.
        /// </summary>
        public string ViewNodeID(Item item);

        /// <summary>
        /// Method 'TreePage.ViewNodeType(...)' will delegate to this implementation for items created by the specific extension.
        /// </summary>
        public string ViewNodeType(Item item);

        /// <summary>
        /// Method 'TreePage.ViewNodeDetails(...)' will delegate to this implementation for items created by the specific extension.
        /// </summary>
        public string ViewNodeDetails(Item item, int line, int col);

        /// <summary>
        /// Method 'TreePage.ViewNodeInfo(...)' will delegate to this implementation for items created by the specific extension.
        /// </summary>
        public string ViewNodeInfo(Item item);

        /// <summary>
        /// Return the <fragment-type> to use in REST calls.
        /// </summary>
        public string GetFragmentType(Item item);

        /// <summary>
        /// Get the <fragment> to use in REST calls.
        /// </summary>
        public string GetFragment(Item item);

        /// <summary>
        /// Get the REST URL that can be used to retrieve the element that this item represents.
        /// </summary>
        public string GetRestURL(Item item);

        /// <summary>
        /// If the item represents a file (and if the extension supports file retrieval), returns a URL that can be used to retrieve the file; otherwise returns null.
        /// This will probably be something like <GetRestURL(item)>?content=raw.
        /// </summary>
        public string GetDownloadLink(Item item);

        /// <summary>
        /// Whether the given item created by an extension represents a File that should be browsed deeper (possibly by another extension).
        /// </summary>
        public bool RepresentsFileToBeBrowsed(Item item);
    }
}
