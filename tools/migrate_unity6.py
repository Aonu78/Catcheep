"""One-time migration of serialized TMP DLL references using Unity's own map."""
from pathlib import Path
import json
import shutil

root = Path(__file__).resolve().parents[1]
package = next((root / 'Library/PackageCache').glob('com.unity.ugui@*'))
records = json.loads((package / 'PackageConversionData.json').read_text())['assetRecords']
records = [r for r in records if 'fileID:' in r['target']]
changed = 0
for path in (root / 'Assets').rglob('*'):
    if path.suffix not in {'.unity', '.prefab', '.asset', '.mat'}:
        continue
    data = path.read_bytes()
    if not data.startswith(b'%YAML'):
        continue
    updated = data
    for record in records:
        updated = updated.replace(record['target'].encode(), record['replacement'].encode())
        updated = updated.replace(record['target'].replace('89f0137620f6af44b9ba852b4190e64e', 'b5bd0d848a86e48409fe56688d66ecb5').encode(), record['replacement'].encode())
    if updated != data:
        path.write_bytes(updated)
        changed += 1

# Keep the original assets available for recovery, outside Unity's import tree.
source = root / 'Assets/TextMesh Pro/Plugins'
destination = root / 'LegacyPlugins/TextMesh Pro/Plugins'
if source.exists():
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(str(source), str(destination))
    shutil.move(str(source) + '.meta', str(destination) + '.meta')
print(f'Migrated TMP references in {changed} assets.')
