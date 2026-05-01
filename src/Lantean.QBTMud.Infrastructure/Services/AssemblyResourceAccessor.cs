using System.Reflection;
using Lantean.QBTMud.Application.Services;
using Microsoft.Extensions.Options;

namespace Lantean.QBTMud.Infrastructure.Services
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
            : this(typeof(AssemblyResourceAccessor).Assembly)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyResourceAccessor"/> class.
        /// </summary>
        /// <param name="options">The resource accessor configuration.</param>
        public AssemblyResourceAccessor(IOptions<AssemblyResourceAccessorOptions> options)
            : this(GetResourceAssembly(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyResourceAccessor"/> class.
        /// </summary>
        /// <param name="assembly">The assembly that contains embedded resources.</param>
        public AssemblyResourceAccessor(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            _assembly = assembly;
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

        private static Assembly GetResourceAssembly(IOptions<AssemblyResourceAccessorOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.Value.ResourceAssembly
                ?? throw new InvalidOperationException("The resource assembly option must be configured.");
        }
    }
}
