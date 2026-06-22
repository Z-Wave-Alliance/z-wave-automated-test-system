#!/usr/bin/env python3

# SPDX-License-Identifier: BSD-3-Clause
# SPDX-FileCopyrightText: Z-Wave-Alliance <https://z-wavealliance.org>

# Generate MultilevelSensorTypes.json from AWG XML registry.

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_version(value: str | None) -> int:
    if not value:
        return 0
    value = value.strip()
    if "-" in value:
        return int(value.split("-")[0])
    return int(value)


def scale_display_name(name: str, unit: str | None) -> str:
    if unit:
        return f"{name} ({unit})"
    return name


def find_awg_xml(script_path: Path) -> Path | None:
    # Walk up parent folders and look for a sibling AWG repo.
    for parent in script_path.parents:
        candidate = (
            parent
            / "AWG"
            / "source"
            / "xml_registries"
            / "zwa_multlevel_sensor_command_class_list_of_multilevel_sensor_types_and_scales.xml"
        )
        if candidate.is_file():
            return candidate
    return None


def main() -> int:
    script_path = Path(__file__).resolve()
    definitions_dir = script_path.parent
    json_path = definitions_dir / "MultilevelSensorTypes.json"
    xml_path = find_awg_xml(script_path)

    if xml_path is None:
        print(
            "AWG XML not found. Expected in sibling repo: AWG/source/xml_registries/...",
            file=sys.stderr,
        )
        return 1

    root = ET.parse(xml_path).getroot()
    sensor_types = []

    for sensor_type in root.findall("SensorType"):
        if sensor_type.get("reserved") == "true":
            continue

        type_id = int(sensor_type.get("id"), 16)
        scales = []

        for scale in sensor_type.findall("Scale"):
            if scale.get("reserved") == "true":
                continue

            scale_id = int(scale.get("id"), 16)
            scale_name = scale.get("name")
            unit = scale.get("unit") or ""
            min_version = parse_version(scale.get("version"))

            scales.append(
                {
                    "Name": scale_display_name(scale_name, unit),
                    "Id": scale_id,
                    "MinVersion": min_version,
                }
            )

        if not scales:
            continue

        type_min_version = min(s["MinVersion"] for s in scales)
        sensor_types.append(
            {
                "Name": sensor_type.get("name"),
                "Id": type_id,
                "MinVersion": type_min_version,
                "Scales": scales,
            }
        )

    content = json.dumps(sensor_types, indent=4, ensure_ascii=False) + "\n"
    with json_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write(content)

    print(f"Wrote {len(sensor_types)} sensor types to {json_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
