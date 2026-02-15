using System.Reflection;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides access to embedded resources in the current assembly.
    /// </summary>
    public sealed class AssemblyResourceAccessor : IAssemblyResourceAccessor
    {
        private readonly Assembly _assembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyResourceAccessor"/> class.
        /// </summary>
        public AssemblyResourceAccessor()
        {
            _assembly = typeof(AssemblyResourceAccessor).Assembly;
        }

        /// <inheritdoc />
        public string[] GetManifestResourceNames()
        {
            return _assembly.GetManifestResourceNames();
        }

        /// <inheritdoc />
        public Stream? GetManifestResourceStream(string name)
        {
            return _assembly.GetManifestResourceStream(name);
        }
    }
}
