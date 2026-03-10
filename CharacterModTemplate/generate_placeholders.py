#!/usr/bin/env python3
"""
Generate placeholder images for a character mod.
Requires: pip install Pillow

Usage: python generate_placeholders.py <pck_root_dir>

Creates minimal colored placeholder images so the mod can run without
real art assets. Replace these with actual art before releasing.
"""
from __future__ import annotations

import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    print("WARNING: Pillow not installed. Run: pip install Pillow")
    print("Skipping placeholder image generation.")
    sys.exit(0)


# Theme color for your character (RGB)
THEME_COLOR = (79, 195, 247)  # Light blue - change to match your character
DARK_COLOR = (26, 82, 118)
TEXT_COLOR = (255, 255, 255)


def make_solid(w: int, h: int, color: tuple, text: str = "") -> Image.Image:
    """Create a solid-color image with optional centered text."""
    img = Image.new("RGBA", (w, h), color + (255,))
    if text:
        draw = ImageDraw.Draw(img)
        try:
            font = ImageFont.truetype("arial.ttf", min(w, h) // 6)
        except OSError:
            font = ImageFont.load_default()
        bbox = draw.textbbox((0, 0), text, font=font)
        tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
        draw.text(((w - tw) // 2, (h - th) // 2), text, fill=TEXT_COLOR, font=font)
    return img


def make_silhouette(w: int, h: int, color: tuple, label: str = "") -> Image.Image:
    """Create a simple character silhouette placeholder."""
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Simple body shape
    cx, cy = w // 2, h // 2
    # Head
    head_r = w // 6
    draw.ellipse([cx - head_r, h // 6, cx + head_r, h // 6 + head_r * 2], fill=color + (255,))
    # Body
    draw.rectangle([cx - w // 4, h // 6 + head_r * 2, cx + w // 4, h - h // 6], fill=color + (255,))
    if label:
        try:
            font = ImageFont.truetype("arial.ttf", w // 8)
        except OSError:
            font = ImageFont.load_default()
        bbox = draw.textbbox((0, 0), label, font=font)
        tw = bbox[2] - bbox[0]
        draw.text(((w - tw) // 2, h - h // 8), label, fill=TEXT_COLOR, font=font)
    return img


def ensure_parent(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)


def generate(pck_root: Path):
    """Generate all placeholder images into the PCK source directory."""

    # Character select icon (64x64)
    p = pck_root / "images" / "packed" / "character_select" / "char_select_mycharacter.png"
    ensure_parent(p)
    make_solid(64, 64, THEME_COLOR, "MC").save(p)

    # Character select locked icon
    p = pck_root / "images" / "packed" / "character_select" / "char_select_mycharacter_locked.png"
    make_solid(64, 64, (80, 80, 80), "?").save(p)

    # Character idle sprite (combat)
    p = pck_root / "images" / "characters" / "mycharacter" / "mycharacter_idle.png"
    ensure_parent(p)
    make_silhouette(200, 400, THEME_COLOR, "IDLE").save(p)

    # Top panel icon
    p = pck_root / "images" / "ui" / "top_panel" / "character_icon_my_character.png"
    ensure_parent(p)
    make_solid(48, 48, THEME_COLOR, "MC").save(p)

    # Top panel icon outline
    p = pck_root / "images" / "ui" / "top_panel" / "character_icon_my_character_outline.png"
    make_solid(48, 48, DARK_COLOR, "MC").save(p)

    # Character select portrait
    p = pck_root / "images" / "ui" / "charSelect" / "mycharacterPortrait.jpg"
    ensure_parent(p)
    make_solid(600, 800, DARK_COLOR, "PORTRAIT").convert("RGB").save(p)

    # Card portraits (one per card)
    card_ids = ["my_strike", "my_defend", "signature_strike", "signature_skill"]
    for card_id in card_ids:
        p = pck_root / "images" / "packed" / "card_portraits" / "mycharacter" / f"{card_id}.png"
        ensure_parent(p)
        make_solid(250, 190, THEME_COLOR, card_id.replace("_", "\n")).save(p)

    # Relic icon
    p = pck_root / "images" / "relics" / "starter_relic.png"
    ensure_parent(p)
    make_solid(128, 128, THEME_COLOR, "RELIC").save(p)

    # Relic outline icon
    p = pck_root / "images" / "relics" / "outline" / "starter_relic.png"
    ensure_parent(p)
    make_solid(128, 128, DARK_COLOR, "RELIC").save(p)

    # Power icons
    power_ids = ["example_buff", "example_debuff"]
    for power_id in power_ids:
        p = pck_root / "images" / "powers" / f"{power_id}.png"
        ensure_parent(p)
        make_solid(128, 128, THEME_COLOR, power_id.split("_")[1][:4]).save(p)

    print(f"[DONE] Generated placeholder images in {pck_root}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <pck_root_dir>")
        sys.exit(1)
    generate(Path(sys.argv[1]))
