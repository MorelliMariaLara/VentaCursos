#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/content/videos"
mkdir -p "$OUT"
make_clip() {
  local name="$1" color="$2" label="$3" secs="$4" freq="$5"
  ffmpeg -y -f lavfi -i "color=c=${color}:s=1280x720:d=${secs}" \
    -f lavfi -i "sine=frequency=${freq}:duration=${secs}" \
    -vf "drawtext=text='${label}':fontcolor=white:fontsize=48:x=(w-text_w)/2:y=(h-text_h)/2" \
    -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest "$OUT/$name"
}
make_clip lesson-a.mp4 "#0B4F4A" "NEXA Leccion A" 6 440
make_clip lesson-b.mp4 "#123048" "NEXA Leccion B" 5 520
make_clip lesson-c.mp4 "#3E6B52" "NEXA Leccion C" 5 330
make_clip lesson-d.mp4 "#C45C26" "NEXA Leccion D" 5 660
echo "Videos listos en $OUT"
