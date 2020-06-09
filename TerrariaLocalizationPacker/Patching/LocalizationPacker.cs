using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;

namespace TerrariaLocalizationPacker.Patching {
	public class LocalizationPacker {
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>The path to terraria's executable.</summary>*/
		public static string ExePath { get; set; } = "";
		/**<summary>The output directory for the localization files.</summary>*/
		public static string OutputDirectory { get; set; } = "";
		/**<summary>The input directory for the localization files.</summary>*/
		public static string InputDirectory { get; set; } = "";
		/**<summary>Gets the path to terraria's backup.</summary>*/
		public static string BackupPath {
			get { return ExePath + ".bak"; }
		}
		/**<summary>Gets the directory of the Terraria executable.</summary>*/
		public static string ExeDirectory {
			get { return Path.GetDirectoryName(ExePath); }
		}
		/**<summary>Gets the directory of this application.</summary>*/
		public static string AppDirectory {
			get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
		}

		#endregion
		//=========== PACKING ============
		#region Packing

		/**<summary>Restores the Terraria backup.</summary>*/
		public static void Restore() {
			File.Copy(BackupPath, ExePath, true);
		}
		/**<summary>Unpacks the localization files from terraria.</summary>*/
		public static void Unpack() {
			// Backup the file first
			if (!File.Exists(BackupPath)) {
				File.Copy(ExePath, BackupPath, false);
			}

			var AsmDefinition = AssemblyDefinition.ReadAssembly(ExePath);
			var ModDefinition = AsmDefinition.MainModule;

			foreach (Resource r in ModDefinition.Resources) {
				EmbeddedResource er = r as EmbeddedResource;
				if (er != null && er.Name.StartsWith("Terraria.Localization.Content.") && er.Name.EndsWith(".json")) {
					string path = Path.Combine(OutputDirectory, er.Name);
					File.WriteAllBytes(path, er.GetResourceData());
				}
			}
		}
		/**<summary>Repacks the localization files into terraria.</summary>*/
		public static bool Repack() {
			// Backup the file first
			if (!File.Exists(BackupPath)) {
				File.Copy(ExePath, BackupPath, false);
			}

			var resolver = new EmbeddedAssemblyResolver();
			var readerParameters = new ReaderParameters{
				AssemblyResolver = resolver,
				ReadingMode = ReadingMode.Deferred,
				//ReadSymbols = false,
				ReadWrite = true,  // New in updated Mono.Cecil version
			};
			var writerParameters = new WriterParameters{
				//WriteSymbols = false,
			};

			bool filesFound = false;
			// New usage of stream (combined with newer version of Mono.Cecil),
			//  allows us to do everything in one open-close), no more waiting till a file is "no longer in use"
			using (Stream stream = File.Open(ExePath, FileMode.Open, FileAccess.ReadWrite)) {
				var AsmDefinition = AssemblyDefinition.ReadAssembly(stream, readerParameters);
				var ModDefinition = AsmDefinition.MainModule;

				List<EmbeddedResource> dllResources = new List<EmbeddedResource>();
				for (int i = 0; i < ModDefinition.Resources.Count; i++) {
					EmbeddedResource er = ModDefinition.Resources[i] as EmbeddedResource;
					if (er == null)
						continue;
					//TODO: In the future these could be loosened to include non-json files, etc.
					//  Step 1: Ignore extension requirements
					//  Step 2: Only check for "Terraria.Localization." prefix, the "Content."
					//          part seems too specific. Naturally this must also be accounted
					//          for elsewhere in this program's code.
					if (er.Name.StartsWith("Terraria.Localization.Content.") && er.Name.EndsWith(".json")) {
						string path = Path.Combine(InputDirectory, er.Name);
						if (File.Exists(path)) {
							// Replace the resource with the new user-supplied one
							ManifestResourceAttributes attributes = er.Attributes;
							ModDefinition.Resources.RemoveAt(i);
							er = new EmbeddedResource(er.Name, er.Attributes, File.ReadAllBytes(path));
							ModDefinition.Resources.Insert(i, er);
							filesFound = true;
						}
					}
					// NOTE: as of Terraria 1.4, all dlls are prefixed with "Terraria.Libraries."
					//  and end with ".dll", Terraria resolves these at runtime by checking if the
					//  resource name ends with the assembly reference name.
					// I don't recall if Terraria prefixed these dll names in 1.3.5 or earlier,
					// so we'll assume all dlls are potential candidates.
					//else if (er.Name.StartsWith("Terraria.Libraries") && er.Name.EndsWith(".dll")) {
					else if (er.Name.EndsWith(".dll")) {
						dllResources.Add(er);
					}
				}

				if (filesFound) {
					for (int i = 0; i < dllResources.Count; i++) {
						resolver.AddPreloadedResource(dllResources[i].Name, dllResources[i].GetResourceData());
					}
					// Do not include the filename?  We're writing to the same place, so
					// this may help tell Mono.Cecil to avoid any unnecessary rewrites that
					// may require assembly reference resolution.
					//  In anycase we, still preload embedded references as a safety.
					AsmDefinition.Write(writerParameters);
					//AsmDefinition.Write(ExePath, writerParameters);

					// Wait for the exe to be closed by AsmDefinition.Write()
					//Thread.Sleep(400);

					// Mono.Cecil.ImageWriter will only add LargeAddressAware characteristic for 64-bit assemblies.
					// Terraria lists itself as a 32-bit assembly AND as large-address aware. This is different
					//  from the alternative "32-bit preferred" setting. As a result we lose this vital flag.
					//https://github.com/jbevain/cecil/blob/f6a871b023fe10015be0e97955143aedc1232110/Mono.Cecil.PE/ImageWriter.cs#L199-L203
					// We're now passing the same still-open stream that was just written to so that we can make our
					// changes without waiting for the file to no longer "be in use"... potentially being scanned by antivirus? who knows.
					IL.MakeLargeAddressAware(stream);
				}
			}

			return filesFound;
		}

		#endregion
	}
}
