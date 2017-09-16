using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLocalizationPacker.Packing {
	/**<summary>From: https://github.com/dougbenham/TerrariaPatcher/blob/master/IL.cs</summary>*/
	public static class IL {
		public static void MakeLargeAddressAware(string file) {
			using (var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)) {
				const int IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

				var br = new BinaryReader(stream);
				var bw = new BinaryWriter(stream);

				if (br.ReadInt16() != 0x5A4D)       //No MZ Header
					return;

				br.BaseStream.Position = 0x3C;
				var peloc = br.ReadInt32();         //Get the PE header location.

				br.BaseStream.Position = peloc;
				if (br.ReadInt32() != 0x4550)       //No PE header
					return;

				br.BaseStream.Position += 0x12;

				var position = br.BaseStream.Position;
				var flags = br.ReadInt16();
				bool isLAA = (flags & IMAGE_FILE_LARGE_ADDRESS_AWARE) == IMAGE_FILE_LARGE_ADDRESS_AWARE;
				if (isLAA)                          //Already Large Address Aware
					return;

				flags |= IMAGE_FILE_LARGE_ADDRESS_AWARE;

				bw.Seek((int)position, SeekOrigin.Begin);
				bw.Write(flags);
				bw.Flush();
			}
		}
	}
}
