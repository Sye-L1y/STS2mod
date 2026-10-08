import struct
import sys

PCK = r"F:\sts2\Slay the Spire 2\SlayTheSpire2.pck"

with open(PCK, "rb") as f:
    magic = f.read(4)
    assert magic == b"GDPC", magic
    fmt = struct.unpack("<I", f.read(4))[0]
    vmajor, vminor, vpatch = struct.unpack("<III", f.read(12))
    flags = struct.unpack("<I", f.read(4))[0]
    file_base = struct.unpack("<Q", f.read(8))[0]
    f.read(16 * 4)  # reserved
    count = struct.unpack("<I", f.read(4))[0]
    print("fmt", fmt, "ver", vmajor, vminor, vpatch, "flags", flags, "base", file_base, "count", count)

    entries = {}
    for _ in range(count):
        plen = struct.unpack("<I", f.read(4))[0]
        path = f.read(plen).rstrip(b"\x00").decode("utf-8", "replace")
        off, size = struct.unpack("<QQ", f.read(16))
        f.read(16)  # md5
        eflags = struct.unpack("<I", f.read(4))[0] if fmt >= 2 else 0
        entries[path] = (off, size, eflags)

    hits = [p for p in entries if "potion_atlas" in p and p.endswith(".tres")]
    hits.sort()
    print("potion atlas tres count:", len(hits))
    for p in hits[:8]:
        print(" ", p)
    if not hits:
        hits = [p for p in entries if "potion" in p.lower() and p.endswith(".tres")][:8]
        for p in hits:
            print("alt:", p)

    # read first few tres
    for p in hits[:6]:
        off, size, eflags = entries[p]
        f.seek((file_base + off) if file_base else off)
        data = f.read(size)
        print("====", p, "size", size, "flags", eflags)
        try:
            print(data.decode("utf-8", "replace")[:600])
        except Exception as e:
            print("decode err", e)