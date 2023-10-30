import struct
import sys
import functools
import itertools
import array
import math
import os

import xml.etree.ElementTree as ET

global opsPrefabFolder
opsPrefabFolder = ""

class OpsInfo:
	def __init__(self):
		self.asset = "";
		self.name = "";
		self.preferredasset1 = "";
		self.preferredasset2 = "";
		# Order is x, y, z
		self.pos = [0.0, 0.0, 0.0]
		self.rot = [0.0, 0.0, 0.0, 0.0] # in quaterion
		self.scl = [1.0, 1.0, 1.0]
		self.materialdiffuse = [1.0, 1.0, 1.0, 1.0]
		self.materialemission = [0.0, 0.0, 0.0]

def readcstr(f):
    toeof = iter(functools.partial(f.read, 1), b'')
    return (b''.join(itertools.takewhile(b'\0'.__ne__, toeof))).decode("ASCII")

def parse_commadouble(s):
	lst = s.replace(" ", "").split(",")
	for i in range(len(lst)):
		lst[i] = float(lst[i])
	return lst

def arr_rad_to_deg(a):
	for i in range(len(a)):
		a[i] = (a[i] / (2 * math.pi)) * 360

def arr_deg_to_rad(a):
	for i in range(len(a)):
		a[i] = (a[i] / 360) * (2 * math.pi)

def arr_rad_to_quat(a):
	if len(a) < 4:
		a.append(0.0)
	cy = math.cos(a[2] * 0.5)
	sy = math.sin(a[2] * 0.5)
	cp = math.cos(a[1] * 0.5)
	sp = math.sin(a[1] * 0.5)
	cr = math.cos(a[0] * 0.5)
	sr = math.sin(a[0] * 0.5)
	a[0] = cr * cp * cy + sr * sp * sy
	a[1] = sr * cp * cy - cr * sp * sy
	a[2] = cr * sp * cy + sr * cp * sy
	a[3] = cr * cp * sy - sr * sp * cy

def arr_normalize_deg(a):
	for i in range(len(a)):
		a[i] %= 360
		if a[i] < 0:
			a[i] += 360

def arr_deg(a):
	for i in range(len(a)):
		a[i] = (a[i] / (2 * math.pi)) * 360

def arr_quat_to_mat3(m, q):
	sqrt2 = math.sqrt(2)
	q0 = sqrt2 * q[0]
	q1 = sqrt2 * q[1]
	q2 = sqrt2 * q[2]
	q3 = sqrt2 * q[3]

	qda = q0 * q1
	qdb = q0 * q2
	qdc = q0 * q3
	qaa = q1 * q1
	qab = q1 * q2
	qac = q1 * q3
	qbb = q2 * q2
	qbc = q2 * q3
	qcc = q3 * q3

	m[(0 * 3) + 0] = 1.0 - qbb - qcc
	m[(0 * 3) + 1] = qdc + qab
	m[(0 * 3) + 2] = -qdb + qac

	m[(1 * 3) + 0] = -qdc + qab
	m[(1 * 3) + 1] = 1.0 - qaa - qcc
	m[(1 * 3) + 2] = qda + qbc

	m[(2 * 3) + 0] = qdb + qac
	m[(2 * 3) + 1] = -qda + qbc
	m[(2 * 3) + 2] = 1.0 - qaa - qbb

def arr_mat3_normalized_to_eul2(mat, eul1, eul2):
	cy = math.hypot(mat[(0 * 3) + 0], mat[(0 * 3) + 1])

	epsilon = 1.192092896e-07
	if cy > 16.0 * epsilon:

		eul1[0] = math.atan2(mat[(1 * 3) + 2], mat[(2 * 3) + 2])
		eul1[1] = math.atan2(-mat[(0 * 3) + 2], cy)
		eul1[2] = math.atan2(mat[(0 * 3) + 1], mat[(0 * 3) + 0])

		eul2[0] = math.atan2(-mat[(1 * 3) + 2], -mat[(2 * 3) + 2])
		eul2[1] = math.atan2(-mat[(0 * 3) + 2], -cy)
		eul2[2] = math.atan2(-mat[(0 * 3) + 1], -mat[(0 * 3) + 0])
	else:
		eul1[0] = math.atan2(-mat[(2 * 3) + 1], mat[(1 * 3) + 1])
		eul1[1] = math.atan2(-mat[(0 * 3) + 2], cy)
		eul1[2] = 0.0

		eul2[0] = eul1[0]
		eul2[1] = eul1[1]
		eul2[2] = eul1[2]

def arr_mat3_normalized_to_eul(eul, mat):
	eul1 = [0.0, 0.0, 0.0]
	eul2 = [0.0, 0.0, 0.0]

	arr_mat3_normalized_to_eul2(mat, eul1, eul2)

	# return best, which is just the one with lowest values it in
	if (math.fabs(eul1[0]) + math.fabs(eul1[1]) + math.fabs(eul1[2])) > (math.fabs(eul2[0]) + math.fabs(eul2[1]) + math.fabs(eul2[2])):
		eul[0] = eul2[0]
		eul[1] = eul2[1]
		eul[2] = eul2[2]
	else:
		eul[0] = eul1[0]
		eul[1] = eul1[1]
		eul[2] = eul1[2]

def arr_quat_to_rad(a):
	unit_mat = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]
	arr_quat_to_mat3(unit_mat, a)
	if len(a) == 4:
		del a[-1]
	arr_mat3_normalized_to_eul(a, unit_mat)

def process_asset_xml(ops_info):
	fn = ops_info.asset
	ops_info.preferredasset1 = fn
	# special exceptions
	if fn == "O_C03TBL00":
		ops_info.preferredasset2 = "c0tbl00"
		return
	if fn == "O_C03TBL01":
		ops_info.preferredasset2 = "c0tbl01"
		return
	if fn == "O_C03TBL02":
		ops_info.preferredasset2 = "c0tbl02"
		return
	if fn == "O_C24DOR01":
		ops_info.preferredasset2 = "24dor01"
		return
	if fn == "O_F30KAG28":
		ops_info.preferredasset2 = "s30kag28"
		return
	if fn == "O_F44EVT00_GS":
		ops_info.preferredasset2 = "f44evt_coaster_gs"
		return
	if fn == "O_M21ETC01":
		ops_info.preferredasset2 = "m21ect01"
		return
	if fn == "O_M60EVT47":
		ops_info.preferredasset2 = "s00evt47"
		return
	if fn == "O_R02LIG00":
		ops_info.preferredasset2 = "o_r02lig00"
		return
	if fn == "O_S00FLS00":
		ops_info.preferredasset2 = "light00"
		return
	if fn == "O_S00SKYW":
		ops_info.preferredasset2 = "s00skyw_sky"
		return
	if fn == "O_S50KMO50":
		ops_info.preferredasset2 = "s50obj50"
		return
	if fn == "O_S61KMO45B":
		ops_info.preferredasset2 = "s61kag45b"
		return
	if fn == "O_S61KMO46":
		ops_info.preferredasset2 = "s61kag46"
		return
	if fn == "O_T00EVT61":
		ops_info.preferredasset2 = "t00ev61"
		return
	if fn == "O_T03EVT00":
		ops_info.preferredasset2 = "t02evt00"
		return
	if fn == "O_T10TIG03":
		ops_info.preferredasset2 = "t10lig03"
		return
	if fn == "O_T40KMO01":
		ops_info.preferredasset2 = "t40lmo01"
		return
	if fn == "O_T50KMO01H":
		ops_info.preferredasset2 = "t50kag01h"
		return
	if fn == "O_TESTRIGD":
		ops_info.preferredasset2 = "rigidtest"
		return
	if fn == "O_V03TRN02":
		ops_info.preferredasset2 = "v03tm02"
		return
	# chop off O
	fn = fn[2:]
	# lowercase
	fn = fn.lower()
	ops_info.preferredasset2 = fn

def read_xml(f):
	lst = []
	root = ET.fromstring(f.read().decode("utf-8-sig"))
	for child in root:
		if child.tag == "MapObjects":
			for cchild in child:
				attr = cchild.attrib
				if "asset" in attr and "name" in attr and "pos" in attr and "rot" in attr and "scl" in attr:
					ops_info = OpsInfo()
					ops_info.asset = attr["asset"]
					ops_info.name = attr["name"]
					ops_info.pos = parse_commadouble(attr["pos"])
					ops_info.rot = parse_commadouble(attr["rot"])
					ops_info.scl = parse_commadouble(attr["scl"])
					ops_info.materialdiffuse = parse_commadouble(attr["materialDiffuse"])
					ops_info.materialemission = parse_commadouble(attr["materialEmission"])
					# reverse X position
					ops_info.pos[0] = -ops_info.pos[0]
					# subtract 90 rotation (radian)
					ops_info.rot[0] -= (math.pi * 2) / 4
					# reverse Y rotation (radian)
					ops_info.rot[1] = -ops_info.rot[1]
					arr_rad_to_quat(ops_info.rot)
					process_asset_xml(ops_info)
					lst.append(ops_info)
	return lst

def read_plt(f):
	lst = []
	shouldbezero = struct.unpack("I", f.read(4))
	if shouldbezero[0] != 0:
		raise "Should be zero"
	length = struct.unpack("I", f.read(4))
	if length[0] == 0:
		return
	for i in range(length[0]):
		oldpos = f.tell()
		objname = readcstr(f)
		f.seek(oldpos + 32)
		oldpos = f.tell()
		assname = readcstr(f)
		f.seek(oldpos + 32)
		unk1, unk2, unk3, transformlen = struct.unpack("IIII", f.read(16))
		print("Object: " + objname)
		print("Asset: " + assname)
		for j in range(transformlen):
			print("-----------------")
			arr = array.array("f", f.read(64))
			print(str(arr[0]) + " " + str(arr[1]) + " " + str(arr[2]) + " " + str(arr[3]))
			print(str(arr[4]) + " " + str(arr[5]) + " " + str(arr[6]) + " " + str(arr[7]))
			print(str(arr[8]) + " " + str(arr[9]) + " " + str(arr[10]) + " " + str(arr[11]))
			print(str(arr[12]) + " " + str(arr[13]) + " " + str(arr[14]) + " " + str(arr[15]))

def read_ed6(f):
	lst = []
	opslength = 312
	if f.read(20) == b"ED6AC_OPSFILE_VER2.0":
		opslength = 332
	else:
		f.seek(0)
	length = struct.unpack("H", f.read(2))
	if length[0] == 0:
		return
	for i in range(length[0]):
		oldpos = f.tell()
		filename = readcstr(f)
		f.seek(oldpos + 12)
		arr = array.array("f", f.read(opslength-12))
		print("Object: " + filename)
		print("X: " + str(arr[63]))
		print("Y: " + str(arr[62]))
		print("Z: " + str(arr[61]))
		print("Rotation: " + str(arr[64]))

for arg in sys.argv[1:]:
	with open(arg, "rb") as f:
		#print("FILE: " + arg)
		ident, = struct.unpack("I", f.read(4))
		f.seek(0)
		is_xml = ident == 0x3CBFBBEF
		is_plt = ident == 0x00000000
		is_ed6_3 = ident == 0x41364445
		lst = []
		if is_xml:
			lst = read_xml(f)
		elif is_plt:
			read_plt(f)
		else:
			read_ed6(f)
		if False:
			for entry in lst:
				print("Asset: " + entry.asset)
				print("Name: " + entry.name)
				print("Prefname1: " + entry.preferredasset1)
				print("Prefname2: " + entry.preferredasset2)
				print("Position: " + str(entry.pos[0]) + " " + str(entry.pos[1]) + " " + str(entry.pos[2]))
				print("Rotation: " + str(entry.rot[0]) + " " + str(entry.rot[1]) + " " + str(entry.rot[2]) + " " + str(entry.rot[3]))
				print("Scale: " + str(entry.scl[0]) + " " + str(entry.scl[1]) + " " + str(entry.scl[2]))
				print("MaterialDiffuse: " + str(entry.materialdiffuse[0]) + " " + str(entry.materialdiffuse[1]) + " " + str(entry.materialdiffuse[2]) + " " + str(entry.materialdiffuse[3]))
				print("MaterialEmission: " + str(entry.materialemission[0]) + " " + str(entry.materialemission[1]) + " " + str(entry.materialemission[2]))
		else:
			entries_deduped = {}
			for entry in lst:
				entries_deduped[entry.asset] = True
			with open(arg + ".sorted.txt", "w") as wf:
				for x in sorted(entries_deduped.keys()):
					if opsPrefabFolder == "" or (not os.path.isfile(opsPrefabFolder + "/" + x + ".prefab")):
						wf.write(x + "\n")

