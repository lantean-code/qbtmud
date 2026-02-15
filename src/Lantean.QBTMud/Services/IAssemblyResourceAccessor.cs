namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides access to assembly-embedded resources.
    /// </summary>
    public interface IAssemblyResourceAccessor
    {
        /// <summary>
        /// Gets the manifest resource names exposed by the underlying assembly.
        /// </summary>
        /// <returns>The manifest resource names.</returns>
        string[] GetManifestResourceNames();

        /// <summary>
        /// Gets the stream for the specified manifest resource.
        /// </summary>
        /// <param name="name">The manifest resource name.</param>
        /// <returns>The resource stream when found; otherwise, <c>null</c>.</returns>
        Stream? GetManifestResourceStream(string name);
    }
}
