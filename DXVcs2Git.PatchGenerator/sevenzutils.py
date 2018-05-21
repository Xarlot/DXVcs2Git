import subprocess
from pathlib import Path
import os
from distutils import dir_util
_7zFolderName =  os.path.join(os.getcwd(), '7z')
_7z = os.path.join(_7zFolderName, r'7z.exe')
shared7zPath = Path(os.path.join('\\\\hyperwpf01', 'Share', '7z'))
if not Path(_7z).exists():
    dir_util.copy_tree(shared7zPath, _7zFolderName)

class SevenZUtils:
    @staticmethod
    def extract_archive(archive_path, output_path):
        if Path(archive_path).exists():
            subprocess.check_call([_7z, 'x', '-y', '-aoa', archive_path, '-o' + output_path])
        else:
            raise Exception('File not found: ' + archive_path)
        pass
    @staticmethod
    def make_archive(archive_path, output_file):
        subprocess.check_call([_7z, 'a', '-mx0', '-tzip','-r', output_file, archive_path])
        pass

    @staticmethod
    def check_archieve(archive_path):
        subprocess.check_call([_7z, 't', archive_path])