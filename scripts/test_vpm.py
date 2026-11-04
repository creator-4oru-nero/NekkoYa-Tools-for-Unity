import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import zipfile
from build_vpm import ROOT, PACKAGE, build, entry, listing


class VpmTests(unittest.TestCase):
    def test_archive_and_listing(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory)
            version = json.loads((ROOT / 'Packages' / PACKAGE / 'package.json').read_text())['version']
            path = build(output, 'v' + version)
            first = path.read_bytes()
            self.assertEqual(first, build(output).read_bytes())
            source = ROOT / 'Packages' / PACKAGE
            with zipfile.ZipFile(path) as archive:
                self.assertIn('package.json', archive.namelist())
                self.assertEqual(set(archive.namelist()), {p.relative_to(source).as_posix() for p in source.rglob('*') if p.is_file()})
                for name in archive.namelist():
                    self.assertEqual(archive.read(name), (source / name).read_bytes())
            manifest = entry(first, 'v' + version)
            self.assertEqual(manifest['zipSHA256'], hashlib.sha256(first).hexdigest())
            self.assertTrue(manifest['url'].endswith('/v' + version + '/' + path.name))
            self.assertEqual(listing([manifest])['packages'][PACKAGE]['versions'][version], manifest)
            with self.assertRaises(ValueError):
                entry(first, 'v0.0.0')
            with self.assertRaises(ValueError):
                listing([manifest, manifest])
            with self.assertRaises(ValueError):
                build(output, 'v0.0.0')

    def test_listing_keeps_multiple_versions(self):
        manifests = [{'name': PACKAGE, 'version': v} for v in ('1.0.0-pre.1', '1.0.0', '1.1.0')]
        self.assertEqual(len(listing(manifests)['packages'][PACKAGE]['versions']), 3)


if __name__ == '__main__':
    unittest.main()
