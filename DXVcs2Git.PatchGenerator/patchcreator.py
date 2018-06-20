import os
import stat
import signal
import subprocess
from lxml import etree
import zipfile

import shutil
import errno

from typing import List

cwd = os.getcwd()

def copyFiles(storage, repository, branch, hash, destination, filesStr):
    filesStr = filesStr.replace('п»ї', '')
    __createStorageDir(storage)
    repFullPath = os.path.join(storage, __getRepositoryFolder(repository))
    files = __getFilesFromXml(filesStr)
    __prepareRepository(repFullPath, branch, hash, repository, files)
    __copyFilesCore(repFullPath, destination, filesStr, files)
    os.chdir(cwd)
    pass

def __copyFilesCore(repFullPath, destination, filesStr, files: []):
    os.chdir(repFullPath)
    destinationDirName = os.path.dirname(destination)
    if not os.path.exists(destinationDirName):
        os.makedirs(destinationDirName)
    with zipfile.ZipFile(destination, 'w') as myzip:
        myzip.writestr(f"patch.info", filesStr)
        myzip.writestr(f"files.txt", os.linesep.join([str(x) for x in files]))
        for file in files:
            myzip.write(file)
    pass

def __getFilesFromXml(filesStr) -> []:
    root = etree.XML(filesStr)

    result = []
    for simple in root.xpath('.//Items/Complex/Properties/Simple[@name="NewPath"]'):
        if __isFileNeeded(simple) == True:
            result = result + [simple.get('value')]
    return result

def __isFileNeeded(node):
    parentnode = node.getparent()
    for simpe in parentnode.iter('Simple'):
        if simpe.get('name') == 'SyncAction':
            value = simpe.get('value')
            if value == None or value == 'Delete':
                return False
            return True
    return False
    pass


def errorRemoveReadonly(func, path, exc):
    excvalue = exc[1]
    if excvalue.errno == errno.EACCES:
        # change the file to be readable,writable,executable: 0777
        os.chmod(path, stat.S_IRWXU | stat.S_IRWXG | stat.S_IRWXO)
        # retry
        func(path)
    pass

def __prepareRepository(repFullPath, branch, hash, repository, files: []):
    if not os.path.exists(repFullPath):
        os.makedirs(repFullPath)
        __createNewRep(repFullPath, repository, branch)
    elif not os.path.exists(os.path.join(repFullPath, '.git', 'index')):
        shutil.rmtree(repFullPath, ignore_errors=False, onerror=errorRemoveReadonly)
        os.makedirs(repFullPath)
        __createNewRep(repFullPath, repository, branch)
    os.chdir(repFullPath)
    __updateRep(repFullPath, branch, hash, files)
    pass

def __getRepositoryFolder(repository):
    return repository.split(':')[-1].rsplit('.git', 1)[0]

def __updateRep(repFullPath, branch, hash, files: []):
    if len(files) == 0:
        return

    sparsecheckoutpath = os.path.join(repFullPath, '.git', 'info', 'sparse-checkout')
    if os.path.isfile(sparsecheckoutpath):
        os.remove(sparsecheckoutpath)

    __rungit(rf"fetch origin {branch}")

    for file in files:
        __rungit(rf"checkout -f {hash} {file}")

    __rungit(rf"clean -dfx")
    pass

def __createNewRep(repFullPath, repository, branch):
    os.chdir(repFullPath)
    __rungit(rf"clone -b {branch} {repository} .")
    __rungit(rf"config core.sparsecheckout true")
    pass

def __createStorageDir(storage):
    if not os.path.isdir(storage):
        os.mkdir(storage)
    pass

def __rungit(gitargs):
    return subprocess.check_output("git.exe " + gitargs)
