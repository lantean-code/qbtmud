using System.Reflection;

namespace Lantean.QBTMud.Infrastructure.Services
{
    /// <summary>
    /// Provides configuration for <see cref="AssemblyResourceAccessor"/>.
    /// </summary>
    public sealed class AssemblyResourceAccessorOptions
    {
        /// <summary>
        /// Gets the assembly that contains embedded resources.
        /// </summary>
        public Assembly? ResourceAssembly { get; set; }
    }
}
