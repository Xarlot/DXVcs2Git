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
    repFullPathClone = repFullPath + "_cache"
    __prepareRepository(repFullPath, repFullPathClone, branch, hash, repository, files)
    __copyFilesCore(repFullPathClone, destination, filesStr, files)
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

def __prepareRepository(repFullPath, repFullPathClone, branch, hash, repository, files: []):
    if os.path.exists(os.path.join(repFullPath, '.git', 'info', 'sparse-checkout')):
        shutil.rmtree(repFullPath, ignore_errors=False, onerror=errorRemoveReadonly)
    if not os.path.exists(repFullPath):
        os.makedirs(repFullPath)
        __createNewRep(repFullPath, repository)
    elif not os.path.exists(os.path.join(repFullPath, '.git', 'HEAD')):
        shutil.rmtree(repFullPath, ignore_errors=False, onerror=errorRemoveReadonly)
        os.makedirs(repFullPath)
        __createNewRep(repFullPath, repository)
    os.chdir(repFullPath)
    __updateRep(repFullPath, repFullPathClone, branch, hash, files)
    pass

def __getRepositoryFolder(repository):
    return repository.split(':')[-1].rsplit('.git', 1)[0]

def __updateRep(repFullPath, repFullPathClone, branch, hash, files: []):
    if len(files) == 0:
        return

    __rungit(rf"fetch origin {branch}")

    if os.path.exists(os.path.join(repFullPathClone)):
        shutil.rmtree(repFullPathClone, ignore_errors=False, onerror=errorRemoveReadonly)

    __CloneRep(repFullPath, repFullPathClone)
    os.chdir(repFullPathClone)
    __rungit(rf"config core.sparsecheckout true")
    sparsecheckoutpath = os.path.join(repFullPathClone, '.git', 'info', 'sparse-checkout')
    with open(sparsecheckoutpath, 'w', encoding='utf-8') as sparsecheckoutfile:
        sparsecheckoutfile.writelines(map(lambda s: s + '\n', files))

    __rungit(rf"checkout {hash}")
    pass

def __createNewRep(repFullPath, repository):
    os.chdir(repFullPath)
    __rungit(rf"clone -n {repository} .")
    pass

def __CloneRep(repFullPathSource, repFullPathClone):
    __rungit(rf"clone -n {repFullPathSource} {repFullPathClone}")
    pass

def __createStorageDir(storage):
    if not os.path.isdir(storage):
        os.mkdir(storage)
    pass

def __rungit(gitargs):
    return subprocess.check_output("git.exe " + gitargs)
