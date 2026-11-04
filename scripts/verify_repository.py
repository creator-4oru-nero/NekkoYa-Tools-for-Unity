"""Validate original archive, UPM metadata and translated format strings.

Run with Python 3. Optional: tree-sitter and tree-sitter-c-sharp enable syntax parsing.
Unity compilation and Editor tests must run separately in Unity 2022.3.22f1.
"""
from pathlib import Path
import collections
import hashlib
import json
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "Packages/com.nekkoya.tools"
ORIGINAL = ROOT / "Original/v0.1.0"
failures = []

def check(condition, message):
    if not condition:
        failures.append(message)

with zipfile.ZipFile(ORIGINAL / "source.zip") as archive:
    original_files = set()
    for info in archive.infolist():
        if info.is_dir():
            continue
        relative = Path(*Path(info.filename).parts[1:])
        original_files.add(relative.as_posix())
        path = ORIGINAL / relative
        check(path.is_file() and hashlib.sha256(path.read_bytes()).digest() == hashlib.sha256(archive.read(info)).digest(), "Original changed: " + str(relative))
    check(original_files == {p.relative_to(ORIGINAL).as_posix() for p in ORIGINAL.rglob('*') if p.is_file() and p.name != 'source.zip'}, "Original file set differs")
    print(f"Original archive: {len(original_files)} files checked byte for byte")

manifest = json.loads((PACKAGE / "package.json").read_text())
physbone_original = ROOT / 'Original/PhysBoneOrganizer/v0.1.0/NekkoYaPhysBoneOrganizer.cs'
check(physbone_original.exists() and hashlib.sha256(physbone_original.read_bytes()).hexdigest() ==
      '8c8c0c0b3424073cf73bc02cdb21098c89f36203fe5b8e920e9074d5c249986e', 'Original PhysBone Organizer changed')
print('PhysBone Organizer original: uploaded source SHA-256 checked')
check(manifest["version"] == "1.0.0-pre.2", "Keep unreleased candidate version until approval")
check(manifest["unity"] == "2022.3", "Unexpected Unity version")
assemblies = {}
for path in PACKAGE.rglob('*.asmdef'):
    data = json.loads(path.read_text())
    check(data['name'] not in assemblies, 'Duplicate assembly: '+data['name'])
    check(data.get('includePlatforms') == ['Editor'], 'Non-Editor assembly: '+str(path))
    assemblies[data['name']] = data
for name,data in assemblies.items():
    for dependency in data.get('references',[]):
        check(dependency in assemblies, f'{name} has unresolved assembly {dependency}')
    visited = set()
    def walk(current, chain):
        check(current not in chain, 'Assembly cycle: '+str(chain+[current]))
        if current in chain or current in visited: return
        visited.add(current)
        for target in assemblies.get(current,{}).get('references',[]): walk(target,chain+[current])
    walk(name,[])

guids = {}
for path in PACKAGE.rglob('*.meta'):
    match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(), re.M)
    check(match is not None, 'Invalid meta: '+str(path))
    if match:
        check(match[1] not in guids, 'Duplicate GUID: '+str(path))
        guids[match[1]] = path
for path in PACKAGE.rglob('*'):
    if path.name.endswith('.meta'): continue
    check(path.with_name(path.name+'.meta').exists(), 'Missing meta: '+str(path))
for path in PACKAGE.rglob('*.asset'):
    for guid in re.findall(r'm_Script: \{[^}]*guid: ([0-9a-f]{32})',path.read_text()):
        check(guid in guids,'Preset references missing script: '+str(path))
for path in PACKAGE.rglob('*.cs.meta'):
    rel=path.relative_to(PACKAGE/'Editor') if path.is_relative_to(PACKAGE/'Editor') else None
    if rel:
        candidates=list(ORIGINAL.rglob(path.name))
        if candidates:
            check(path.read_bytes()==candidates[0].read_bytes(),'Existing script GUID/meta changed: '+str(path))

literal=r'"(?:\\.|[^"\\])*"'
catalog_text=(PACKAGE/'Editor/Common/NekkoYaTranslations.cs').read_text()
entries=re.findall(r'\{\s*('+literal+r'), new\[\] \{ ('+literal+r'), ('+literal+r') \} \}',catalog_text)
catalog={}
for raw in entries:
    jp,en,kr=map(json.loads,raw)
    check(jp not in catalog,'Duplicate translation: '+jp)
    catalog[jp]=(en,kr)
    # 置換用の番号とフォーマット指定を保つ。{Name}等の説明文は対象外。
    fields=lambda text: collections.Counter(re.findall(r'\{\d+(?::[^{}]+)?\}',text))
    for value in (en,kr):
        check(bool(value),'Empty translation: '+jp)
        check(fields(jp)==fields(value),'Format fields differ: '+jp)
used=set()
for path in PACKAGE.rglob('*.cs'):
    if '/Tests/' in str(path) or path.name=='NekkoYaTranslations.cs': continue
    source=path.read_text()
    for match in re.finditer(r'(?:L10n|NekkoYaLocalization)\.Text\(('+literal+r')\)',source):
        key=json.loads(match[1]); used.add(key)
        check(key in catalog,'Missing translation: '+key)
    check('Tools/NekkoYa/' not in source,'Old menu path: '+str(path))
    for menu in re.findall(r'\[MenuItem\("([^"]+)"',source):
        check(not re.search('[\u3040-\u30ff\u3400-\u9fff]',menu),'Non-English menu: '+menu)
    for line in source.splitlines():
        if 'Debug.Log' in line:
            check('[NekkoYa][' in line,'Log missing tool prefix: '+str(path))
check(len(catalog) >= 500,'Translation catalog unexpectedly small')
print(f'UPM: {len(assemblies)} Editor assemblies; {len(guids)} unique GUIDs')
print(f'Localization: {len(catalog)} templates, {len(used)} used keys, matching format fields')

try:
    from tree_sitter import Language, Parser
    import tree_sitter_c_sharp
except ImportError:
    print('C# syntax parsing skipped: install tree-sitter and tree-sitter-c-sharp, or run Unity compilation')
else:
    parser=Parser(Language(tree_sitter_c_sharp.language()))
    for path in PACKAGE.rglob('*.cs'):
        tree=parser.parse(path.read_bytes())
        check(not tree.root_node.has_error,'C# syntax error: '+str(path))
    print(f'C# syntax parsed: {len(list(PACKAGE.rglob("*.cs")))} files (not Unity compilation)')

for failure in failures: print('FAIL:',failure)
if failures: raise SystemExit(1)
print('Repository checks passed. Unity Editor compilation / tests are not run by this script.')
