using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Resolves qbtmud build metadata embedded in the assembly.
    /// </summary>
    public interface IAppBuildInfoService
    {
        /// <summary>
        /// Gets current qbtmud build information.
        /// </summary>
        /// <returns>The current build information.</returns>
        AppBuildInfo GetCurrentBuildInfo();
    }
}
