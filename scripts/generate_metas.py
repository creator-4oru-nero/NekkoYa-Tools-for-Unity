"""Create deterministic Unity metadata only for files without an existing .meta."""
from pathlib import Path
import hashlib

root = Path(__file__).resolve().parents[1] / 'Packages/com.nekkoya.tools'
for path in sorted(root.rglob('*')):
    if path.name.endswith('.meta'):
        continue
    meta = path.with_name(path.name + '.meta')
    if meta.exists():
        continue
    guid = hashlib.sha256(('nekkoya.tools/' + path.relative_to(root).as_posix()).encode()).hexdigest()[:32]
    text = 'fileFormatVersion: 2\nguid: ' + guid + '\n'
    if path.is_dir():
        text += 'folderAsset: yes\n'
    importer = 'MonoImporter' if path.suffix == '.cs' else 'AssemblyDefinitionImporter' if path.suffix == '.asmdef' else 'DefaultImporter'
    text += importer + ':\n  externalObjects: {}\n'
    if importer == 'MonoImporter':
        text += '  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'
    text += '  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    meta.write_text(text, encoding='utf-8')
