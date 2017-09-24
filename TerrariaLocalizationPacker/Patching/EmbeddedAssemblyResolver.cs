using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace TerrariaLocalizationPacker.Patching {
	/**<summary>Resolves assemblies for the patcher by checking for embedded resources.</summary>*/
	public class EmbeddedAssemblyResolver : BaseAssemblyResolver {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The collection of assembly resource names.</summary>*/
		private Dictionary<string, string> assemblyResources;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the embedded assembly resolver.</summary>*/
		public EmbeddedAssemblyResolver() {
			assemblyResources = new Dictionary<string, string>();
		}

		#endregion
		//========== RESOLVING ===========
		#region Resolving

		/**<summary>Resolves the assembly name.</summary>*/
		public override AssemblyDefinition Resolve(AssemblyNameReference name) {
			if (name == null)
				throw new ArgumentException("AssemblyNameReference is null.");

			var executingAssembly = Assembly.GetExecutingAssembly();

			// Attempt to read a predefined resource assembly
			if (assemblyResources.ContainsKey(name.Name)) {
				using (Stream stream = executingAssembly.GetManifestResourceStream(assemblyResources[name.Name])) {
					if (stream != null)
						return ModuleDefinition.ReadModule(stream).Assembly;
				}
			}
			// Attempt to read a dll resource assembly
			using (Stream stream = executingAssembly.GetManifestResourceStream(name.Name + ".dll")) {
				if (stream != null)
					return ModuleDefinition.ReadModule(stream).Assembly;
			}
			// Attempt to read an exe resource assembly
			using (Stream stream = executingAssembly.GetManifestResourceStream(name.Name + ".exe")) {
				if (stream != null)
					return ModuleDefinition.ReadModule(stream).Assembly;
			}

			return base.Resolve(name);
		}
		/**<summary>Adds an assembly name as a resource name to be resolved later.</summary>*/
		public void AddEmbeddedAssembly(string assemblyName, string resourceName) {
			assemblyResources.Add(assemblyName, resourceName);
		}

		#endregion
	}
}
