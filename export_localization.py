"""Parse Odin-serialized LocalizationConfig.asset into CSV."""
import csv
from collections import defaultdict
from pathlib import Path

ASSET = Path("Assets/Prefabs/Config/LocalizationConfig.asset")
OUT = Path("localization_export.csv")

SYSLANG = {
    10: "English",
    40: "ChineseSimplified",
    41: "ChineseTraditional",
    21: "Italian",
    15: "German",
    28: "Portuguese",
    14: "French",
    34: "Spanish",
    30: "Russian",
    22: "Japanese",
    23: "Korean",
}

def decode_yaml(raw):
    if raw is None:
        return ""
    s = raw
    if len(s) >= 2 and s[0] == '"' and s[-1] == '"':
        s = s[1:-1]
        try:
            s = s.encode("utf-8").decode("unicode_escape")
        except Exception:
            pass
    return s

text = ASSET.read_text(encoding="utf-8")
nodes = []
cur = {}
for line in text.splitlines():
    s = line.strip()
    if s.startswith("- Name:"):
        if cur: nodes.append(cur)
        cur = {"Name": s[len("- Name:"):].strip()}
    elif s.startswith("Name:"):
        if cur: nodes.append(cur)
        cur = {"Name": s[len("Name:"):].strip()}
    elif s.startswith("Entry:"):
        cur["Entry"] = s[len("Entry:"):].strip()
    elif s.startswith("Data:"):
        cur["Data"] = s[len("Data:"):].strip()
if cur: nodes.append(cur)

result = defaultdict(dict)
seen_langs = []

lang_positions = []
for idx, nd in enumerate(nodes):
    if nd.get("Name") == "$k" and nd.get("Entry") == "3":
        try:
            lang_int = int(nd.get("Data", "0"))
            lang_positions.append((idx, lang_int))
        except ValueError:
            pass
lang_positions.append((len(nodes), None))

for li in range(len(lang_positions) - 1):
    start, lang_int = lang_positions[li]
    end, _ = lang_positions[li + 1]
    lang_name = SYSLANG.get(lang_int, f"Lang_{lang_int}")
    if lang_name not in seen_langs:
        seen_langs.append(lang_name)
    j = start + 1
    while j < end:
        nd = nodes[j]
        if nd.get("Name") == "$k" and nd.get("Entry") == "1":
            key = decode_yaml(nd.get("Data", ""))
            text_val = None
            k = j + 1
            while k < end:
                nd2 = nodes[k]
                if nd2.get("Name") == "$k" and nd2.get("Entry") == "1":
                    break
                if nd2.get("Name") == "text" and nd2.get("Entry") == "1":
                    text_val = decode_yaml(nd2.get("Data", ""))
                    break
                k += 1
            if text_val is not None:
                result[key][lang_name] = text_val
            j = k
        else:
            j += 1

fields = ["Key"] + seen_langs
with OUT.open("w", encoding="utf-8-sig", newline="") as f:
    w = csv.writer(f)
    w.writerow(fields)
    for key in sorted(result.keys()):
        w.writerow([key] + [result[key].get(lang, "") for lang in seen_langs])

print(f"Languages: {seen_langs}")
print(f"Keys: {len(result)}")
print(f"Wrote: {OUT.resolve()}")
