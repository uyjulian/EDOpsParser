using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;

namespace OpsParser
{
	public class OpsInfo
	{
		public OpsInfo()
		{
			asset = "";
			name = "";
			preferredasset1 = "";
			preferredasset2 = "";
			pos = new double[3];
			rot = new double[4];
			scl = new double[3];
			scl[0] = scl[1] = scl[2] = 1;
			materialdiffuse = new double[4];
			materialdiffuse[0] = materialdiffuse[1] = materialdiffuse[2] = materialdiffuse[3] = 1;
			materialemission = new double[3];
		}
		public String asset;
		public String name;
		public String preferredasset1;
		public String preferredasset2;
		// Order is x, y, z
		public double[] pos;
		public double[] rot; // in quaterion
		public double[] scl;
		public double[] materialdiffuse;
		public double[] materialemission;
	}

	public static class OpsParserProgram
	{
		static void parse_commadouble(String str, double[] arr)
		{
			var split = str.Replace(" ", "").Split(new char[] { ',' });
			for (var i = 0; i < split.Length; i += 1)
			{
				String split_no_period = split[i];
				if (split_no_period.Substring(split_no_period.Length - 1, 1) == ".")
				{
					split_no_period = split_no_period.Substring(0, split_no_period.Length - 1);
				}
				arr[i] = double.Parse(split_no_period, System.Globalization.CultureInfo.InvariantCulture);
			}
		}
		static void arr_rad_to_deg(double[] arr)
		{
			for (var i = 0; i < arr.Length; i += 1)
			{
				arr[i] = (arr[i] / (2 * Math.PI)) * 360;
			}
		}
		static void arr_deg_to_rad(double[] arr)
		{
			for (var i = 0; i < arr.Length; i += 1)
			{
				arr[i] = (arr[i] / 360) * (2 * Math.PI);
			}
		}
		static void arr_rad_to_quat(double[] arr)
		{
			var cy = Math.Cos(arr[2] * 0.5);
			var sy = Math.Sin(arr[2] * 0.5);
			var cp = Math.Cos(arr[1] * 0.5);
			var sp = Math.Sin(arr[1] * 0.5);
			var cr = Math.Cos(arr[0] * 0.5);
			var sr = Math.Sin(arr[0] * 0.5);
			arr[0] = cr * cp * cy + sr * sp * sy;
			arr[1] = sr * cp * cy - cr * sp * sy;
			arr[2] = cr * sp * cy + sr * cp * sy;
			arr[3] = cr * cp * sy - sr * sp * cy;
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
		static void arr_deg(double[] arr)
		{
			for (var i = 0; i < arr.Length; i += 1)
			{
				arr[i] = (arr[i] / (2 * Math.PI)) * 360;
			}
		}
		static void process_asset_xml(OpsInfo ops_info)
		{
			String fn = ops_info.asset;
			ops_info.preferredasset1 = fn;
			// special exceptions
			// TODO: add special exceptions for ED85
			if (fn == "O_C03TBL00")
			{
				ops_info.preferredasset2 = "c0tbl00";
				return;
			}
			if (fn == "O_C03TBL01")
			{
				ops_info.preferredasset2 = "c0tbl01";
				return;
			}
			if (fn == "O_C03TBL02")
			{
				ops_info.preferredasset2 = "c0tbl02";
				return;
			}
			if (fn == "O_C24DOR01")
			{
				ops_info.preferredasset2 = "24dor01";
				return;
			}
			if (fn == "O_F30KAG28")
			{
				ops_info.preferredasset2 = "s30kag28";
				return;
			}
			if (fn == "O_F44EVT00_GS")
			{
				ops_info.preferredasset2 = "f44evt_coaster_gs";
				return;
			}
			if (fn == "O_M21ETC01")
			{
				ops_info.preferredasset2 = "m21ect01";
				return;
			}
			if (fn == "O_M60EVT47")
			{
				ops_info.preferredasset2 = "s00evt47";
				return;
			}
			if (fn == "O_R02LIG00")
			{
				ops_info.preferredasset2 = "o_r02lig00";
				return;
			}
			if (fn == "O_S00FLS00")
			{
				ops_info.preferredasset2 = "light00";
				return;
			}
			if (fn == "O_S00SKYW")
			{
				ops_info.preferredasset2 = "s00skyw_sky";
				return;
			}
			if (fn == "O_S50KMO50")
			{
				ops_info.preferredasset2 = "s50obj50";
				return;
			}
			if (fn == "O_S61KMO45B")
			{
				ops_info.preferredasset2 = "s61kag45b";
				return;
			}
			if (fn == "O_S61KMO46")
			{
				ops_info.preferredasset2 = "s61kag46";
				return;
			}
			if (fn == "O_T00EVT61")
			{
				ops_info.preferredasset2 = "t00ev61";
				return;
			}
			if (fn == "O_T03EVT00")
			{
				ops_info.preferredasset2 = "t02evt00";
				return;
			}
			if (fn == "O_T10TIG03")
			{
				ops_info.preferredasset2 = "t10lig03";
				return;
			}
			if (fn == "O_T40KMO01")
			{
				ops_info.preferredasset2 = "t40lmo01";
				return;
			}
			if (fn == "O_T50KMO01H")
			{
				ops_info.preferredasset2 = "t50kag01h";
				return;
			}
			if (fn == "O_TESTRIGD")
			{
				ops_info.preferredasset2 = "rigidtest";
				return;
			}
			if (fn == "O_V03TRN02")
			{
				ops_info.preferredasset2 = "v03tm02";
				return;
			}
			// chop off O_
			fn = fn.Substring(2);
			// lowercase
			fn = fn.ToLower();
			ops_info.preferredasset2 = fn;
		}
		public static void read_xml(string filename, LinkedList<OpsInfo> ops_list)
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
						parse_commadouble(attr["materialDiffuse"].InnerText, opsInfo.materialdiffuse);
						parse_commadouble(attr["materialEmission"].InnerText, opsInfo.materialemission);
						// reverse X position
						opsInfo.pos[0] = -opsInfo.pos[0];
						// subtract 90 rotation (radian)
						opsInfo.rot[0] -= (Math.PI * 2) / 4;
						// reverse Y rotation (radian)
						opsInfo.rot[1] = -opsInfo.rot[1];
						arr_rad_to_quat(opsInfo.rot);
						process_asset_xml(opsInfo);
						ops_list.AddLast(opsInfo);
					}
					
				}
			}
		}
		public static void read_plt(string filename, LinkedList<OpsInfo> ops_list)
		{
			using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
			{
				// skip null bytes
				reader.BaseStream.Seek(4, SeekOrigin.Current);
				var count = reader.ReadUInt32();

				byte[] filename_entry = new byte[32];
				float[] mat = new float[16];
				for (var i = 0; i < count; i += 1)
				{
					var objname = "";
					reader.Read(filename_entry, 0, 32);
					for (var j = 0; j < 32; j += 1)
					{
						if (filename_entry[j] == 0)
						{
							objname = System.Text.Encoding.ASCII.GetString(filename_entry, 0, j);
							break;
						}
					}
					var assname = "";
					reader.Read(filename_entry, 0, 32);
					for (var j = 0; j < 32; j += 1)
					{
						if (filename_entry[j] == 0)
						{
							assname = System.Text.Encoding.ASCII.GetString(filename_entry, 0, j);
							break;
						}
					}
					var unk1 = reader.ReadUInt32();
					var unk2 = reader.ReadUInt32();
					var unk3 = reader.ReadUInt32();
					var transformcount = reader.ReadUInt32();
					for (var j = 0; j < transformcount; j += 1)
					{
						for (var k = 0; k < 16; k += 1)
						{
							mat[k] = reader.ReadSingle();
						}
						var opsInfo = new OpsInfo();
						opsInfo.asset = assname;
						opsInfo.name = objname;
						opsInfo.pos[0] = mat[12];
						opsInfo.pos[1] = mat[13];
						opsInfo.pos[2] = mat[14];
						opsInfo.scl[0] = Math.Sqrt(Math.Pow(mat[0], 2) + Math.Pow(mat[4], 2) + Math.Pow(mat[8], 2));
						opsInfo.scl[1] = Math.Sqrt(Math.Pow(mat[1], 2) + Math.Pow(mat[5], 2) + Math.Pow(mat[9], 2));
						opsInfo.scl[2] = Math.Sqrt(Math.Pow(mat[2], 2) + Math.Pow(mat[6], 2) + Math.Pow(mat[10], 2));
						var tr = mat[0] + mat[5] + mat[10];
						if (tr > 0)
						{
							var S = Math.Sqrt(tr + 1.0) * 2; // S=4*qw
							opsInfo.rot[0] = ( 0.25 * S );
							opsInfo.rot[1] = ( (mat[9] - mat[6]) / S );
							opsInfo.rot[2] = ( (mat[2] - mat[8]) / S );
							opsInfo.rot[3] = ( (mat[4] - mat[1]) / S );
						}
						else if ((mat[0] > mat[5]) && (mat[0] > mat[10]))
						{
							var S = Math.Sqrt(1.0 + mat[0] - mat[5] - mat[10]) * 2; // S=4*qx
							opsInfo.rot[0] = ( (mat[9] - mat[6]) / S );
							opsInfo.rot[1] = ( 0.25 * S );
							opsInfo.rot[2] = ( (mat[1] + mat[4]) / S );
							opsInfo.rot[3] = ( (mat[2] + mat[8]) / S );
						}
						else if (mat[5] > mat[10])
						{
							var S = Math.Sqrt(1.0 + mat[5] - mat[0] - mat[10]) * 2; // S=4*qy
							opsInfo.rot[0] = ( (mat[2] - mat[8]) / S );
							opsInfo.rot[1] = ( (mat[1] + mat[4]) / S );
							opsInfo.rot[2] = ( 0.25 * S );
							opsInfo.rot[3] = ( (mat[6] + mat[9]) / S );
						}
						else
						{
							var S = Math.Sqrt(1.0 + mat[10] - mat[0] - mat[5]) * 2; // S=4*qz
							opsInfo.rot[0] = ( (mat[4] - mat[1]) / S );
							opsInfo.rot[1] = ( (mat[2] + mat[8]) / S );
							opsInfo.rot[2] = ( (mat[6] + mat[9]) / S );
							opsInfo.rot[3] = ( 0.25 * S );
						}
						// reverse X position
						opsInfo.pos[0] = -opsInfo.pos[0];
						// reverse X rotation (quat)
						opsInfo.rot[1] = -opsInfo.rot[1];
						// reverse Y rotation (quat)
						//opsInfo.rot[2] = -opsInfo.rot[2];
						process_asset_xml(opsInfo);
						ops_list.AddLast(opsInfo);
					}
				}
			}
		}
		static void process_asset_ed6(OpsInfo ops_info)
		{
			String fn = ops_info.asset;
			// chop off .x
			if (fn.Length >= 2)
			{
				fn = fn.Substring(0, fn.Length - 2);
			}
			ops_info.preferredasset1 = fn;
			// lowercase
			fn = fn.ToLower();
			ops_info.preferredasset2 = fn;
		}
		public static void read_ed6(string filename, LinkedList<OpsInfo> ops_list, bool is_ed6_3)
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
					arr_deg_to_rad(opsInfo.rot);
					arr_rad_to_quat(opsInfo.rot);
					process_asset_ed6(opsInfo);
					ops_list.AddLast(opsInfo);
				}
			}
		}
		static void Main(string[] args)
		{
			var path = args[0];

			LinkedList<OpsInfo> ops_list = new LinkedList<OpsInfo>();
			var is_xml = false;
			var is_plt = false;
			var is_ed6_3 = false;
			using (var reader = new BinaryReader(File.Open(path, FileMode.Open)))
			{
				var ident = reader.ReadUInt32();
				is_xml = ident == 0x3CBFBBEF;
				is_plt = ident == 0x00000000;
				is_ed6_3 = ident == 0x41364445;
			}
			
			if (is_xml)
			{
				read_xml(path, ops_list);
			}
			else if (is_plt)
			{
				read_plt(path, ops_list);
			}
			else
			{
				read_ed6(path, ops_list, is_ed6_3);
			}

			foreach (OpsInfo entry in ops_list)
			{
				Console.WriteLine("Asset: " + entry.asset);
				Console.WriteLine("Name: " + entry.name);
				Console.WriteLine("Prefname1: " + entry.preferredasset1);
				Console.WriteLine("Prefname2: " + entry.preferredasset2);
				Console.WriteLine("Position: " + entry.pos[0] + " " + entry.pos[1] + " " + entry.pos[2]);
				Console.WriteLine("Rotation: " + entry.rot[0] + " " + entry.rot[1] + " " + entry.rot[2] + " " + entry.rot[3]);
				Console.WriteLine("Scale: " + entry.scl[0] + " " + entry.scl[1] + " " + entry.scl[2]);
				Console.WriteLine("MaterialDiffuse: " + entry.materialdiffuse[0] + " " + entry.materialdiffuse[1] + " " + entry.materialdiffuse[2] + " " + entry.materialdiffuse[3]);
				Console.WriteLine("MaterialEmission: " + entry.materialemission[0] + " " + entry.materialemission[1] + " " + entry.materialemission[2]);
			}
		}
	}
}
