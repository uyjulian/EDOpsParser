using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;

namespace OpsParser
{
	class OpsInfo
	{
		public OpsInfo()
		{
			asset = "";
			name = "";
			pos = new double[3];
			rot = new double[3];
			scl = new double[3];
			scl[0] = scl[1] = scl[2] = 1;
		}
		public String asset;
		public String name;
		// Order is x, y, z
		public double[] pos;
		public double[] rot; // in normalized degrees
		public double[] scl;
	}

	static class Program
	{
		static void parse_commadouble(String str, double[] arr)
		{
			var split = str.Replace(" ", "").Split(new char[] { ',' });
			for (var i = 0; i < split.Length; i += 1)
			{
				arr[i] = double.Parse(split[i], System.Globalization.CultureInfo.InvariantCulture);
			}
		}
		static void arr_rad_to_deg(double[] arr)
		{
			for (var i = 0; i < arr.Length; i += 1)
			{
				arr[i] = (arr[i] / (2 * Math.PI)) * 360;
			}
		}
		static void arr_normalize_deg(double[] arr)
		{
			for (var i = 0; i < arr.Length; i += 1)
			{
				arr[i] %= 360;
				if (arr[i] < 0)
				{
					arr[i] += 360;
				}
			}
		}
		static void read_xml(string filename, List<OpsInfo> ops_list)
		{
			var xml_doc = new XmlDocument();
			xml_doc.Load(filename);

			var assetObjects = xml_doc.GetElementsByTagName("AssetObject");

			for (var i = 0; i < assetObjects.Count; i += 1)
			{
				if (assetObjects[i] != null && assetObjects[i].Attributes != null)
				{
					var attr = assetObjects[i].Attributes;
					if (attr["asset"] != null && attr["name"] != null && attr["pos"] != null && attr["rot"] != null && attr["scl"] != null)
					{
						var opsInfo = new OpsInfo();
						opsInfo.asset = attr["asset"].InnerText;
						opsInfo.name = attr["name"].InnerText;
						parse_commadouble(attr["pos"].InnerText, opsInfo.pos);
						parse_commadouble(attr["rot"].InnerText, opsInfo.rot);
						parse_commadouble(attr["scl"].InnerText, opsInfo.scl);
						// reverse X position
						opsInfo.pos[0] = -opsInfo.pos[0];
						// reverse Y rotation
						opsInfo.rot[1] = -opsInfo.rot[1];
						arr_rad_to_deg(opsInfo.rot);
						arr_normalize_deg(opsInfo.rot);
						ops_list.Add(opsInfo);
					}
					
				}
			}
		}
		static void read_ed6(string filename, List<OpsInfo> ops_list, bool is_ed6_3)
		{
			var opslength = 312;
			if (is_ed6_3)
			{
				opslength = 332;
			}
			using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
			{
				if (is_ed6_3)
				{
					// skip header
					reader.BaseStream.Seek(20, SeekOrigin.Current);
				}
				var count = reader.ReadUInt16();
				byte[] filename_entry = new byte[12];
				byte[] entry = new byte[opslength - 12];
				float[] entry_floats = new float[entry.Length / sizeof(float)];
				for (var i = 0; i < count; i += 1)
				{
					var mdl_filename = "";
					reader.Read(filename_entry, 0, 12);
					for (var j = 0; j < 12; j += 1)
					{
						if (filename_entry[j] == 0)
						{
							mdl_filename = System.Text.Encoding.ASCII.GetString(filename_entry, 0, j);
							break;
						}
					}
					reader.Read(entry, 0, opslength - 12);
					for (var j = 0; j < entry_floats.Length; j += 1)
					{
						entry_floats[j] = BitConverter.ToSingle(entry, j * sizeof(float));
					}

					var opsInfo = new OpsInfo();
					opsInfo.asset = mdl_filename;
					opsInfo.pos[0] = entry_floats[63];
					opsInfo.pos[1] = entry_floats[62];
					opsInfo.pos[2] = entry_floats[61];
					opsInfo.rot[1] = entry_floats[64];
					arr_normalize_deg(opsInfo.rot);
					ops_list.Add(opsInfo);
				}
			}
		}
		static void Main(string[] args)
		{
			List<OpsInfo> ops_list = new List<OpsInfo>();
			var is_xml = false;
			var is_ed6_3 = false;
			using (var reader = new BinaryReader(File.Open(args[0], FileMode.Open)))
			{
				var ident = reader.ReadUInt32();
				is_xml = ident == 0x3CBFBBEF;
				is_ed6_3 = ident == 0x41364445;
			}
			
			if (is_xml)
			{
				read_xml(args[0], ops_list);
			}
			else
			{
				read_ed6(args[0], ops_list, is_ed6_3);
			}

			for (var i = 0; i < ops_list.Count; i += 1)
			{
				var entry = ops_list[i];
				Console.WriteLine("Asset: " + entry.asset);
				Console.WriteLine("Name: " + entry.name);
				Console.WriteLine("Position: " + entry.pos[0] + " " + entry.pos[1] + " " + entry.pos[2]);
				Console.WriteLine("Rotation: " + entry.rot[0] + " " + entry.rot[1] + " " + entry.rot[2]);
				Console.WriteLine("Scale: " + entry.scl[0] + " " + entry.scl[1] + " " + entry.scl[2]);
			}
		}
	}
}
