# Momo native capture — failed original preserved

User supplied both archive parts from `momo-authored-20261031-104717-828afa35`. `receipt.json` records archive SHA256, all original file SHA256/bytes and image dimensions. `native/` retains all38 entries unchanged, including32 decoded720×1280 images, logs, XML and source records. Archive CRC, safe paths and duplicate entries were checked before extraction.

The native result is **0 passed / 1 failed / 0 skipped**, source `0efef45aacddd055f67cbe0b7ee4ade7d08b950e`. The stopped comparison `stop-0241` reports5362 pixels outside the allowed actor region. The natural Before shows Noah's bottom-right caption; the natural After hides it through the existing caption collision policy. Additional-character captures omitted the Before-only caption control used by the fixed Noah capture. No proof of an approved Momo image, completed full QA, APK or Pixel execution is inferred from this failed run.

The scoped capture fix and Windows single-Momo retry are described in `docs/momo-caption-comparison-fix-2026-10-31.md`. No received result or image is rewritten as a passing run.
