using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLocalizationPacker.Patching {
	//https://github.com/dougbenham/TerrariaPatcher/blob/master/IL.cs
	/**<summary>The main helper class for scanning and modifying assemblies.</summary>*/
	public static class IL {
		//===== LARGE ADDRESS AWARE ======
		#region Large Address Aware

		/**<summary>Patches the executable to allow more memory usage. This is needed after Mono.cecil writes to the assembly.</summary>*/
		public static void MakeLargeAddressAware(string file) {
			using (var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)) {
				MakeLargeAddressAware(stream);
			}
		}

		/**<summary>Patches the executable stream to allow more memory usage. This is needed after Mono.cecil writes to the assembly. The passed stream must be readable AND writable.</summary>*/
		public static void MakeLargeAddressAware(Stream stream) {
			const int IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

			stream.Flush(); // Flush any unfinished changes from patching.
			stream.Seek((int)0, SeekOrigin.Begin); // Make sure we're at the beginning
			var br = new BinaryReader(stream);
			var bw = new BinaryWriter(stream);

			if (br.ReadInt16() != 0x5A4D)       //No MZ Header
				return;

			stream.Position = 0x3C;
			var peloc = br.ReadInt32();         //Get the PE header location.

			stream.Position = peloc;
			if (br.ReadInt32() != 0x4550)       //No PE header
				return;

			stream.Position += 0x12;

			var position = stream.Position;
			var flags = br.ReadInt16();
			bool isLAA = (flags & IMAGE_FILE_LARGE_ADDRESS_AWARE) == IMAGE_FILE_LARGE_ADDRESS_AWARE;
			if (isLAA)                          //Already Large Address Aware
				return;

			flags |= IMAGE_FILE_LARGE_ADDRESS_AWARE;

			stream.Seek((int)position, SeekOrigin.Begin);
			bw.Write(flags);
			bw.Flush();
		}

		#endregion
	}
}
