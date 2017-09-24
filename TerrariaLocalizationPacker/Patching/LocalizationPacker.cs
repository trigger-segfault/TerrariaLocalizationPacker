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
			var parameters = new ReaderParameters{ AssemblyResolver = resolver };
			var AsmDefinition = AssemblyDefinition.ReadAssembly(ExePath, parameters);
			var ModDefinition = AsmDefinition.MainModule;

			bool filesFound = false;
			for (int i = 0; i < ModDefinition.Resources.Count; i++) {
				EmbeddedResource er = ModDefinition.Resources[i] as EmbeddedResource;
				if (er != null && er.Name.StartsWith("Terraria.Localization.Content.") && er.Name.EndsWith(".json")) {
					string path = Path.Combine(InputDirectory, er.Name);
					if (File.Exists(path)) {
						ManifestResourceAttributes attributes = er.Attributes;
						ModDefinition.Resources.RemoveAt(i);
						er = new EmbeddedResource(er.Name, er.Attributes, File.ReadAllBytes(path));
						ModDefinition.Resources.Insert(i, er);
						filesFound = true;
					}
				}
			}

			if (filesFound) {
				AsmDefinition.Write(ExePath);
				// Wait for the exe to be closed by AsmDefinition.Write()
				Thread.Sleep(400);
				IL.MakeLargeAddressAware(ExePath);
			}

			return filesFound;
		}

		#endregion
	}
}
