import struct
import sys
import functools
import itertools
import array

def readcstr(f):
    toeof = iter(functools.partial(f.read, 1), b'')
    return (b''.join(itertools.takewhile(b'\0'.__ne__, toeof))).decode("ASCII")

for arg in sys.argv[1:]:
	with open(arg, "rb") as f:
		opslength = 312
		if f.read(20) == b"ED6AC_OPSFILE_VER2.0":
			opslength = 332
		else:
			f.seek(0)
		length = struct.unpack("H", f.read(2))
		if length[0] == 0:
			continue
		print("FILE: " + arg)
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
