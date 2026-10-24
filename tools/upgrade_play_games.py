"""Install the official GPGS v2 Unity package, retaining the old SDK for recovery."""
from pathlib import Path
import shutil
import tarfile

root = Path(__file__).resolve().parents[1]
for relative in ['GooglePlayGames', 'PlayServicesResolver', 'Plugins/Android', 'Plugins/iOS']:
    source = root / 'Assets' / relative
    destination = root / 'LegacyPlugins' / relative
    if source.exists():
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(source), str(destination))
        meta = Path(str(source) + '.meta')
        if meta.exists():
            shutil.move(str(meta), str(destination) + '.meta')

package = root / 'Temp/Migration/GooglePlayGamesPlugin-2.2.1.unitypackage'
with tarfile.open(package) as archive:
    members = {m.name: m for m in archive.getmembers()}
    for name, member in members.items():
        if not name.endswith('/pathname'):
            continue
        relative = archive.extractfile(member).read().decode('utf-8').strip()
        target = (root / relative).resolve()
        if not target.is_relative_to(root / 'Assets'):
            raise ValueError(f'Unexpected package target: {relative}')
        prefix = name.rsplit('/', 1)[0]
        asset = members.get(prefix + '/asset')
        if asset is not None:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.extractfile(asset).read())
        else:
            target.mkdir(parents=True, exist_ok=True)
        meta = members.get(prefix + '/asset.meta')
        if meta is not None:
            Path(str(target) + '.meta').write_bytes(archive.extractfile(meta).read())
print('Installed official Google Play Games 2.2.1 and External Dependency Manager 1.2.182.')
