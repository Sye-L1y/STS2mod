#!/usr/bin/env python3
"""按 Godot 引擎的真实读法校验 Godot 4 pck 目录是否被破坏。

和 tools/pck.py 的关键区别：本脚本**不做任何对齐补偿**，完全照 Godot
`PackedSourcePCK` 的顺序读：plen(4) -> path(plen 字节，补齐含在 plen 内)
-> offset(8) -> size(8) -> md5(16) -> flags(4, ver>=2)。
真实的数据绝对位置 = offset + file_base。

只要 pck 目录里有一条记录的写法与 Godot 约定不符（例如 plen 写成"真实长度
+ 外部补齐"），Godot 就会从该条起全部错位读取，表现为游戏里「资源全部为空」。
本脚本能立刻抓出这类问题。

用法：
    python tools/pck_verify.py <a.pck> [b.pck ...]
退出码 0 = 全部通过。
"""

from __future__ import annotations

import hashlib
import struct
import sys
from pathlib import Path

MAGIC = 0x43504447  # "GDPC"


def verify(path: Path) -> bool:
    raw = path.read_bytes()
    magic, ver, vmaj, vmin, vpat = struct.unpack_from("<IIIII", raw, 0)
    if magic != MAGIC:
        print(f"[FAIL] {path.name}: magic 不是 GDPC")
        return False
    pack_flags, file_base = struct.unpack_from("<IQ", raw, 20)
    (dir_offset,) = struct.unpack_from("<Q", raw, 32)
    print(f"— {path.name}  ver={ver} engine={vmaj}.{vmin}.{vpat} flags={pack_flags} "
          f"file_base={file_base} dir_offset={dir_offset} size={len(raw):,}")

    if not (0 < dir_offset < len(raw)):
        print(f"[FAIL] dir_offset 越界：{dir_offset}")
        return False

    off = dir_offset
    (count,) = struct.unpack_from("<I", raw, off)
    off += 4

    problems: list[str] = []
    seen: dict[str, int] = {}
    try:
        for _ in range(count):
            (plen,) = struct.unpack_from("<I", raw, off)
            off += 4
            if plen % 4 != 0:
                problems.append(f"plen={plen} 不是 4 的倍数（路径补齐必须算进 plen）")
            pb = raw[off:off + plen]
            off += plen
            name = pb.rstrip(b"\x00").decode("utf-8", "replace")
            if b"\x00" in pb[:len(pb.rstrip(b"\x00"))]:
                problems.append(f"路径 {name!r} 中间出现 NUL")
            foff, fsize = struct.unpack_from("<QQ", raw, off)
            off += 16
            md5 = raw[off:off + 16]
            off += 16
            if ver >= 2:
                off += 4  # flags

            seen[name] = seen.get(name, 0) + 1
            abs_off = foff + file_base
            if abs_off < 0 or abs_off + fsize > len(raw):
                problems.append(f"{name}: 数据越界 abs={abs_off} size={fsize}")
                continue
            if fsize and hashlib.md5(raw[abs_off:abs_off + fsize]).digest() != md5:
                problems.append(f"{name}: 内容 md5 与目录不符（偏移/长度错误）")
    except struct.error as ex:
        problems.append(f"目录解析越界（写法与 Godot 约定不符，读飞了）：{ex} at off={off}")

    dup = [n for n, c in seen.items() if c > 1]
    if off != len(raw):
        problems.append(f"目录解析在 {off} 结束，但文件长度是 {len(raw)}（差 {len(raw) - off}）")
    if dup:
        problems.append(f"重复路径 {len(dup)} 个，例如 {dup[:3]}")

    print(f"  条目 {count} 条，目录结束 {off} / 文件 {len(raw)}")
    if problems:
        print(f"[FAIL] {path.name}: 发现 {len(problems)} 个问题")
        for p in problems[:10]:
            print("   -", p)
        return False
    print(f"[OK]   {path.name}: 目录可被 Godot 正确读取，全部条目 md5 一致")
    return True


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    return 0 if all(verify(Path(a)) for a in sys.argv[1:]) else 1


if __name__ == "__main__":
    raise SystemExit(main())
