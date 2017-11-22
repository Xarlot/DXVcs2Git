import argparse
import os
import shutil
import subprocess
import xml.etree.ElementTree as ET
import re
import urllib.request
from pathlib import Path


script_path = os.path.dirname(os.path.realpath(__file__))
nuget_path = os.path.realpath(script_path + './Nuget.exe')
package_path = os.path.realpath(script_path + './files')
spec_path = os.path.realpath(script_path + './GitTools_UI_Addin_Dependencies.nuspec')
spec_namespace = '{http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd}'

parser = argparse.ArgumentParser()
parser.add_argument('--push', dest='push', action='store_true', help='push the package')
parser.add_argument('--label', dest='label', action='store_true', help='use the latest label')
args = parser.parse_args()

def guess_version():
    csproj = f'{script_path}/../DXVcs2Git.UI/DXVcs2Git.UI.csproj'
    with open(csproj, 'r') as file:
        body = file.read()
    m = re.compile(r'DevExpress\.Data\.v(\d\d)\.(\d)').search(body)
    return (int(m[1]), int(m[2]))
    
major, minor = guess_version()
version = f'v{major}.{minor}'

def expand_path(path):
    path = path.replace('{v}', version)
    path = path.replace('{label}', 'Label' if args.label else '')
    base = path[:path.index('{last}')]
    last = sorted(os.listdir(base))[-1]
    return path.replace('{last}', last)
    
def clickonce_path():
    return expand_path(r'\\corp\builds\release\Build{label}.Wpf.ClickOnceDemos.{v}\WpfDemos\{last}')

def clear_dir(path):
    if os.path.exists(path):
        shutil.rmtree(path)

def parse_spec(path):
    tree = ET.parse(path)
    root = tree.getroot()
    return [os.path.basename(file.attrib['src']) for file in root[1]]

def bump_version(path):
    tree = ET.parse(path)
    root = tree.getroot()
    version = root[0].find(spec_namespace + 'version').text
    split = [int(i) for i in version.split('.')]
    split[-1] = split[-1] + 1
    new_version = '.'.join([str(x) for x in split])
    with open(path, 'r') as file:
        body = file.read()
    with open(path, 'w') as file:
        file.write(body.replace(version, new_version))
    
def DownloadNuget():
	nugetFile = Path(nuget_path)
	if not nugetFile.is_file():
		urllib.request.urlretrieve("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", nuget_path)
	
bump_version(spec_path)
clear_dir(package_path)
os.makedirs(package_path)
source = clickonce_path()

print('source:', source)
print('dest:', package_path)

for file in parse_spec(spec_path):
    shutil.copyfile(source + '\\' + file, package_path + '\\' + file)
DownloadNuget()
subprocess.check_call([nuget_path, 'pack', spec_path])
package_file = sorted([x for x in os.listdir('.') if x.endswith('nupkg')])[-1]
print('package created:', package_file)
if args.push:
    subprocess.check_call([nuget_path, 'push', package_file, '-Source', 'http://nuget.devexpress.dev', 'ac6fc8ff1a5a4ff4941e16f26ca24883'])