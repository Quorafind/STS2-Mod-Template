#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import struct
import sys
from dataclasses import dataclass
from pathlib import Path


MAGIC = b"GDPC"
PCK_PADDING = 32
PACK_REL_FILEBASE = 1 << 1


@dataclass
class FileEntry:
    source_path: Path
    pack_path: str
    size: int
    md5: bytes
    offset: int = 0


def pad_to(value: int, alignment: int) -> int:
    remainder = value % alignment
    if remainder == 0:
        return 0
    return alignment - remainder


def normalize_pack_path(root_dir: Path, file_path: Path) -> str:
    relative_path = file_path.relative_to(root_dir).as_posix()
    return relative_path


def collect_files(root_dir: Path) -> list[FileEntry]:
    files: list[FileEntry] = []
    for source_path in sorted(path for path in root_dir.rglob("*") if path.is_file()):
        data = source_path.read_bytes()
        files.append(
            FileEntry(
                source_path=source_path,
                pack_path=normalize_pack_path(root_dir, source_path),
                size=len(data),
                md5=hashlib.md5(data).digest(),
            )
        )
    if not files:
        raise ValueError("打包目录为空，没有可写入 PCK 的文件")
    return files


def compute_offsets(
    files: list[FileEntry],
    pack_version: int,
    engine_major: int,
    engine_minor: int,
    engine_patch: int,
) -> tuple[int, int, int]:
    header_size = 4 + 4 * 4
    if pack_version >= 2:
        header_size += 4 + 8
        if pack_version >= 3:
            header_size += 8
    header_size += 16 * 4

    file_base = header_size
    if pack_version >= 3:
        file_base += pad_to(file_base, PCK_PADDING)

    cursor = file_base
    for entry in files:
        entry.offset = cursor
        cursor += entry.size
        cursor += pad_to(cursor, PCK_PADDING)

    dir_offset = 0
    if pack_version >= 3:
        dir_offset = cursor + pad_to(cursor, PCK_PADDING)

    return header_size, file_base, dir_offset


def write_directory(
    handle,
    files: list[FileEntry],
    pack_version: int,
    engine_major: int,
    engine_minor: int,
    file_base: int,
):
    handle.write(struct.pack("<I", len(files)))
    add_res_prefix = not (engine_major == 4 and engine_minor >= 4)
    for entry in files:
        pack_path = entry.pack_path
        if add_res_prefix and not pack_path.startswith("res://"):
            pack_path = f"res://{pack_path}"
        path_bytes = pack_path.encode("utf-8")
        padded_len = len(path_bytes) + pad_to(len(path_bytes), 4)
        handle.write(struct.pack("<I", padded_len))
        handle.write(path_bytes)
        handle.write(b"\x00" * (padded_len - len(path_bytes)))
        stored_offset = entry.offset
        if pack_version >= 2:
            stored_offset -= file_base
        handle.write(struct.pack("<Q", stored_offset))
        handle.write(struct.pack("<Q", entry.size))
        handle.write(entry.md5)
        if pack_version >= 2:
            handle.write(struct.pack("<I", 0))


def create_pck(
    input_dir: Path,
    output_file: Path,
    pack_version: int,
    engine_major: int,
    engine_minor: int,
    engine_patch: int,
) -> None:
    files = collect_files(input_dir)
    _, file_base, dir_offset = compute_offsets(files, pack_version, engine_major, engine_minor, engine_patch)

    output_file.parent.mkdir(parents=True, exist_ok=True)
    with output_file.open("wb") as handle:
        handle.write(MAGIC)
        handle.write(struct.pack("<I", pack_version))
        handle.write(struct.pack("<I", engine_major))
        handle.write(struct.pack("<I", engine_minor))
        handle.write(struct.pack("<I", engine_patch))

        if pack_version >= 2:
            handle.write(struct.pack("<I", PACK_REL_FILEBASE))
            handle.write(struct.pack("<Q", file_base))
            if pack_version >= 3:
                handle.write(struct.pack("<Q", dir_offset))

        for _ in range(16):
            handle.write(struct.pack("<I", 0))

        if pack_version >= 3:
            handle.write(b"\x00" * pad_to(handle.tell(), PCK_PADDING))

        for entry in files:
            current = handle.tell()
            if current != entry.offset:
                raise ValueError(f"文件偏移计算错误：{entry.pack_path}，期望 {entry.offset}，实际 {current}")
            handle.write(entry.source_path.read_bytes())
            handle.write(b"\x00" * pad_to(handle.tell(), PCK_PADDING))

        if pack_version >= 3:
            handle.write(b"\x00" * pad_to(handle.tell(), PCK_PADDING))
        write_directory(handle, files, pack_version, engine_major, engine_minor, file_base)


def parse_engine_version(version: str) -> tuple[int, int, int]:
    parts = version.split(".")
    if len(parts) < 2:
        raise ValueError("引擎版本至少要写成 major.minor")
    major = int(parts[0])
    minor = int(parts[1])
    patch = int(parts[2]) if len(parts) >= 3 else 0
    return major, minor, patch


def main() -> int:
    parser = argparse.ArgumentParser(description="最小可用的 Godot 4 PCK 打包脚本")
    parser.add_argument("input_dir", type=Path, help="待打包目录")
    parser.add_argument("-o", "--output", type=Path, required=True, help="输出 PCK 文件")
    parser.add_argument("--engine-version", default="4.5.1", help="Godot 引擎版本，默认 4.5.1")
    parser.add_argument("--pack-version", type=int, default=3, help="PCK 版本，默认 3")
    args = parser.parse_args()

    input_dir = args.input_dir.resolve()
    if not input_dir.is_dir():
        print(f"[ERROR] 输入目录不存在：{input_dir}", file=sys.stderr)
        return 1

    try:
        engine_major, engine_minor, engine_patch = parse_engine_version(args.engine_version)
        create_pck(
            input_dir=input_dir,
            output_file=args.output.resolve(),
            pack_version=args.pack_version,
            engine_major=engine_major,
            engine_minor=engine_minor,
            engine_patch=engine_patch,
        )
        print(f"[DONE] 已打包：{args.output}")
        return 0
    except Exception as exc:
        print(f"[ERROR] {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
