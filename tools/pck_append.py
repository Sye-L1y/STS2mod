#!/usr/bin/env python3
"""把 PckPacker 不支持的文件（.tscn 场景）追加进已生成的 Godot 4 pck。

PckPacker 只转 .png（-> .ctex）和拷贝 .json，.tscn 会被跳过（exit 2）。
本工具在 build 后运行：读出 pck 目录里所有旧条目，把新文件的数据追加到文件尾，
再重写目录。Godot 挂载 pck 时按条目路径索引，追加不影响旧资源。

用法： python tools/pck_append.py <mod.pck> <源目录> <res 前缀> [扩展名...]
示例： python tools/pck_append.py src/MyChar/bin/Release/MyChar.pck src/MyChar/assets/scenes MyChar .tscn
  -> 把源目录下所有 .tscn 以 MyChar/scenes/<名>.tscn 追加进 pck。
"""

from __future__ import annotations

import hashlib
import struct
import sys
from pathlib import Path

MAGIC = 0x43504447  # "GDPC"


def read_dir(data: bytes):
    """读出 pck 头与目录条目（不校验 md5）。"""
    magic, ver, vmaj, vmin, vpat = struct.unpack_from("<IIIII", data, 0)
    if magic != MAGIC:
        raise ValueError("not a Godot pck (bad magic)")
    pack_flags, file_base = struct.unpack_from("<IQ", data, 20)
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
        foff, fsize = struct.unpack_from("<QQ", data, off)
        off += 16 + 16
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
    return ver, vmaj, vmin, vpat, pack_flags, file_base, entries


def main() -> int:
    if len(sys.argv) < 5:
        print(__doc__)
        return 1
    pck_path = Path(sys.argv[1])
    src_dir = Path(sys.argv[2])
    res_prefix = sys.argv[3].strip("/")
    exts = tuple(s.lower() for s in sys.argv[4:])

    files = sorted(p for p in src_dir.rglob("*") if p.is_file() and p.suffix.lower() in exts)
    if not files:
        print(f"pck_append: {src_dir} 下没有 {exts} 文件，跳过")
        return 0

    data = bytearray(pck_path.read_bytes())
    ver, vmaj, vmin, vpat, pack_flags, file_base, entries = read_dir(data)
    known = {e["path"] for e in entries}
    print(f"pck_append: 现 {len(entries)} 个条目（engine {vmaj}.{vmin}.{vpat}）")

    # 新文件数据追加到文件尾（16 字节对齐）
    added = 0
    for f in files:
        rel = f.relative_to(src_dir).as_posix()
        internal = f"{res_prefix}/{rel}"
        if internal in known:
            continue
        payload = f.read_bytes()
        pad = (-len(data)) % 16
        data += b"\x00" * pad
        # entries 里的 offset 统一存「绝对文件偏移」（与 read_dir 的输出一致，
        # read_dir 返回 foff + file_base）。写目录时会统一减回 file_base。
        offset = len(data)
        entries.append({
            "path": internal,
            "offset": offset,
            "size": len(payload),
            "flags": 0,
            "md5": hashlib.md5(payload).digest(),
        })
        data += payload
        added += 1
        print(f"  + {internal}  ({len(payload)} bytes)")

    if not added:
        print("pck_append: 没有需要追加的文件")
        return 0

    # 重新编目录写到文件尾（目录偏移本身是绝对文件偏移）
    # 旧条目的 offset 在 read_dir 里已加上 file_base 变成绝对值，写回时再减掉。
    #
    # 路径字段约定（必须和 PckPacker / Godot 的读取方式完全一致）：
    #   plen = 路径字节的 4 字节对齐长度，补齐的 \x00 算在 plen 之内。
    #   Godot 读到 plen 后「不做额外对齐」，直接取 metadata；写成
    #   「plen = 真实长度 + 外部补齐」会让之后的所有字段错位，整个 pck 的资源全部失效。
    dir_buf = bytearray()
    dir_buf += struct.pack("<I", len(entries))
    for e in entries:
        raw = e["path"].encode("utf-8")
        padded = raw + b"\x00" * ((-len(raw)) % 4)
        dir_buf += struct.pack("<I", len(padded)) + padded
        dir_buf += struct.pack("<QQ", e["offset"] - file_base, e["size"])
        dir_buf += e.get("md5") or hashlib.md5(data[e["offset"]:e["offset"] + e["size"]]).digest()
        if ver >= 2:
            dir_buf += struct.pack("<I", e["flags"])

    dir_offset = len(data)
    data += dir_buf
    struct.pack_into("<Q", data, 32, dir_offset)
    pck_path.write_bytes(bytes(data))
    print(f"pck_append: 追加 {added} 个，共 {len(entries)} 个条目 -> {pck_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
