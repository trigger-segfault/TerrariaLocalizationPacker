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

		/**<summary>The collection of raw preloaded assembly resource names and data.</summary>*/
		private Dictionary<string, byte[]> preloadedResources;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the embedded assembly resolver.</summary>*/
		public EmbeddedAssemblyResolver() {
			assemblyResources = new Dictionary<string, string>();
			preloadedResources = new Dictionary<string, byte[]>();
		}

		#endregion
		//========== RESOLVING ===========
		#region Resolving

		/**<summary>Resolves the assembly name.</summary>*/
		public override AssemblyDefinition Resolve(AssemblyNameReference name) {
			if (name == null)
				throw new ArgumentException("AssemblyNameReference is null.");

			// //DEBUG: Remove me later
			// File.AppendAllLines("TerrariaLocalizationPacker.repack.log", new string[1] {
			// 	"[EmbeddedAssemblyResolver] Resolving: \"" + name.Name + "\""
			// });

			///////////////////////////////////////////////////////////////////
			// My mistake, this resolver is only used for Terraria-specific dlls.
			// This could be harmful with our own currently embedded Newtonsoft.Json library.
			///////////////////////////////////////////////////////////////////
			/*
			// If I recall correctly, we need this to resolve Mono.Cecil, which
			//  requires referencing itself when writing to an assembly.
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
			}*/
			
			// Search for Terraria references when writing, the same way its done in Terraria.
			// Hopefully this isn't needed anymore, but 1.4 can no-longer locate its resources due to
			// the prefixed "Terraria.Libraries." in the resource name, additionally, trimming off this
			// part alone is not enough for a full name resolution, as there is usually the developer
			// name for the associated assembly and possibly more.
			string targetResourceName = new AssemblyName(name.Name).Name + ".dll";
			foreach (KeyValuePair<string, byte[]> preloadedResource in this.preloadedResources) {
				if (preloadedResource.Key.EndsWith(targetResourceName)) {
					using (MemoryStream memoryStream = new MemoryStream(preloadedResource.Value))
						return ModuleDefinition.ReadModule(memoryStream).Assembly;
				}
			}

			// Some things we can resolve. For everything else,
			//  there's base.Resolve(AssemblyNameReference);
			return base.Resolve(name);
		}
		/**<summary>Adds an assembly name as a resource name to be resolved later.</summary>*/
		public void AddEmbeddedAssembly(string assemblyName, string resourceName) {
			assemblyResources.Add(assemblyName, resourceName);
		}
		/**<summary>Adds a preloaded assembly resource to be checked for later.</summary>*/
		public void AddPreloadedResource(string resourceName, byte[] resourceData) {
			preloadedResources.Add(resourceName, resourceData);
		}

		#endregion
	}
}
