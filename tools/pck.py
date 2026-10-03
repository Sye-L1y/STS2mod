#!/usr/bin/env python3
"""Minimal Godot 4 .pck reader (list / extract). Pure stdlib."""
import struct, sys, os, json

MAGIC = 0x43504447  # "GDPC"


def read_pck(path):
    with open(path, "rb") as f:
        data = f.read()
    if len(data) < 100:
        raise ValueError("file too small")
    magic, ver, vmaj, vmin, vpat = struct.unpack_from("<IIIII", data, 0)
    if magic != MAGIC:
        raise ValueError("not a Godot pck (bad magic)")
    pack_flags, file_base = struct.unpack_from("<IQ", data, 20)
    # Godot 4.4+: directory_offset (uint64) at 32, then 16 reserved uint32 -> header ends at 104
    (dir_offset,) = struct.unpack_from("<Q", data, 32)
    off = dir_offset if dir_offset else 104
    (count,) = struct.unpack_from("<I", data, off)
    off += 4
    entries = []
    for _ in range(count):
        (plen,) = struct.unpack_from("<I", data, off)
        off += 4
        raw = data[off:off + plen]
        off += plen
        # path padded to 4 bytes
        pad = (4 - (plen % 4)) % 4
        off += pad
        foff, fsize = struct.unpack_from("<QQ", data, off)
        off += 16
        md5 = data[off:off + 16]
        off += 16
        eflags = 0
        if ver >= 2:
            (eflags,) = struct.unpack_from("<I", data, off)
            off += 4
        entries.append({
            "path": raw.rstrip(b"\x00").decode("utf-8", "replace"),
            "offset": foff + file_base,
            "size": fsize,
            "flags": eflags,
        })
    return {"version": ver, "engine": f"{vmaj}.{vmin}.{vpat}", "flags": pack_flags, "files": entries, "data": data}


def main():
    if len(sys.argv) < 3:
        print("usage: pck.py list <file.pck>")
        print("       pck.py extract <file.pck> <outdir> [path-filter-substr]")
        print("       pck.py cat <file.pck> <internal/path>")
        return 1
    cmd, pckpath = sys.argv[1], sys.argv[2]
    p = read_pck(pckpath)
    if cmd == "list":
        print(f"# version={p['version']} engine={p['engine']} files={len(p['files'])}")
        for e in p["files"]:
            print(f"{e['size']:>10}  {e['path']}")
    elif cmd == "cat":
        want = sys.argv[3]
        for e in p["files"]:
            if e["path"] == want:
                sys.stdout.buffer.write(p["data"][e["offset"]:e["offset"] + e["size"]])
                return 0
        print("not found", file=sys.stderr)
        return 1
    elif cmd == "extract":
        outdir = sys.argv[3]
        filt = sys.argv[4] if len(sys.argv) > 4 else ""
        n = 0
        for e in p["files"]:
            if filt and filt not in e["path"]:
                continue
            dst = os.path.join(outdir, e["path"].replace("/", os.sep))
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            with open(dst, "wb") as g:
                g.write(p["data"][e["offset"]:e["offset"] + e["size"]])
            n += 1
        print(f"extracted {n} files to {outdir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())