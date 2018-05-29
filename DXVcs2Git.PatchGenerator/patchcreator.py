import os
import stat
import signal
import subprocess
import xml.etree.ElementTree as ET
import zipfile

import shutil
import errno

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

def __copyFilesCore(repFullPath, destination, filesStr, files):
    os.chdir(repFullPath)
    destinationDirName = os.path.dirname(destination)
    if not os.path.exists(destinationDirName):
        os.makedirs(destinationDirName)
    with zipfile.ZipFile(destination, 'w') as myzip:
        myzip.writestr(f"patch.info", filesStr)
        for file in files:
            myzip.write(file)
    pass

def __getFilesFromXml(filesStr):
    parser = ET.XMLParser(encoding="utf-8")
    root = ET.fromstring(filesStr, parser=parser)

    result = []
    for properties in root.iter('Properties'):
        simpe = __getFileFromProperiesNode(properties)
        if simpe != None:
            result.append(simpe.get('value'))
    return result

def __getFileFromProperiesNode(properties):
    cachedSimple = None
    for simpe in properties.iter('Simple'):
        name = simpe.get('name')
        if name == 'SyncAction':
            if simpe.get('value') == 'Delete':
                return None
        if name == 'NewPath':
            cachedSimple = simpe
    return cachedSimple

def errorRemoveReadonly(func, path, exc):
    excvalue = exc[1]
    if excvalue.errno == errno.EACCES:
        # change the file to be readable,writable,executable: 0777
        os.chmod(path, stat.S_IRWXU | stat.S_IRWXG | stat.S_IRWXO)
        # retry
        func(path)
    pass

def __prepareRepository(repFullPath, branch, hash, repository, files):
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
    
def __updateRep(repFullPath, branch, hash, files):
    if len(files) == 0:
        return
    __rungit(rf"clean -d -f -x")
    __rungit(rf"fetch origin {branch}")

    sparsecheckoutpath = os.path.join(repFullPath, '.git', 'info', 'sparse-checkout')
    with open(sparsecheckoutpath, 'w', encoding='utf-8') as sparsecheckoutfile:
        sparsecheckoutfile.writelines(map(lambda s: s + '\n', files))

    __rungit(rf"read-tree -mu {hash}")
    pass

def __createNewRep(repFullPath, repository, branch):
    os.chdir(repFullPath)
    __rungit(rf"clone -b {branch} {repository} .")
    __rungit(rf"config core.sparsecheckout true")
    pass

def __createStorageDir(storage):
    if not os.path.isdir(storage):
        os.mkdir(storage);
    pass

def __rungit(gitargs):
    return subprocess.check_output("git.exe " + gitargs)
