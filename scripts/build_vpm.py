"""Build a deterministic VPM ZIP and a listing of all published package releases."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = 'com.nekkoya.tools'
REPOSITORY = 'creator-4oru-nero/NekkoYa-Tools-VPM'
LISTING_URL = 'https://creator-4oru-nero.github.io/NekkoYa-Tools-VPM/index.json'


def validate(manifest):
    if manifest['name'] != PACKAGE:
        raise ValueError('Unexpected package name')
    if not re.fullmatch(r'\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?', manifest['version']):
        raise ValueError('Invalid package version')
    return manifest['version']


def build(output, tag=None):
    source = ROOT / 'Packages' / PACKAGE
    manifest = json.loads((source / 'package.json').read_text())
    version = validate(manifest)
    if tag is not None and tag != 'v' + version:
        raise ValueError('Release tag must match package.json: v' + version)
    output.mkdir(parents=True, exist_ok=True)
    target = output / f'{PACKAGE}-{version}.zip'
    with zipfile.ZipFile(target, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(source.rglob('*')):
            if path.is_symlink():
                raise ValueError('Symlink in package: ' + str(path))
            if path.is_file():
                info = zipfile.ZipInfo(path.relative_to(source).as_posix(), (2020, 1, 1, 0, 0, 0))
                info.compress_type = zipfile.ZIP_DEFLATED
                info.external_attr = 0o100644 << 16
                archive.writestr(info, path.read_bytes())
    return target


def entry(data, tag):
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        if archive.testzip() is not None:
            raise ValueError('Corrupt ZIP')
        manifest = json.loads(archive.read('package.json'))
    version = validate(manifest)
    if tag != 'v' + version:
        raise ValueError('Release tag and ZIP version differ')
    manifest['url'] = f'https://github.com/{REPOSITORY}/releases/download/{tag}/{PACKAGE}-{version}.zip'
    manifest['zipSHA256'] = hashlib.sha256(data).hexdigest()
    return manifest


def listing(entries):
    versions = {}
    for manifest in entries:
        version = validate(manifest)
        if version in versions:
            raise ValueError('Duplicate version: ' + version)
        versions[version] = manifest
    return {'name': 'NekkoYa Tools for Unity', 'id': 'com.nekkoya.vpm',
            'author': 'NekkoYa', 'url': LISTING_URL,
            'packages': {PACKAGE: {'versions': versions}}}


def published_entries():
    # gh uses the workflow token; only public release ZIPs are advertised to VCC.
    pages = json.loads(subprocess.check_output(
        ['gh', 'api', '--paginate', '--slurp', f'repos/{REPOSITORY}/releases']))
    for release in (item for page in pages for item in page):
        if release['draft']:
            continue
        tag = release['tag_name']
        if not tag.startswith('v'):
            continue  # Original tool archives are not VPM package releases.
        expected = f'{PACKAGE}-{tag[1:]}.zip'
        if not any(asset['name'] == expected for asset in release['assets']):
            continue
        data = subprocess.check_output(['gh', 'release', 'download', tag,
                                        '--repo', REPOSITORY, '--pattern', expected, '--output', '-'])
        yield entry(data, tag)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['build', 'index'])
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--tag')
    args = parser.parse_args()
    if args.command == 'build':
        print(build(args.output, args.tag))
    else:
        result = listing(published_entries())
        if not result['packages'][PACKAGE]['versions']:
            raise SystemExit('No published VPM packages found; refusing empty deployment')
        args.output.mkdir(parents=True, exist_ok=True)
        (args.output / 'index.json').write_text(json.dumps(result, indent=2) + '\n')
        (args.output / 'index.html').write_text(
            '<!doctype html><html lang="en"><meta charset="utf-8"><title>NekkoYa VCC Repository</title>'
            '<h1>NekkoYa Tools for Unity</h1><p><a href="vcc://vpm/addRepo?url=' + LISTING_URL + '">'
            'Add repository to VCC</a></p><p>Or add this URL in VCC Settings → Packages → Add Repository:</p>'
            '<p><a href="index.json">' + LISTING_URL + '</a></p></html>')
