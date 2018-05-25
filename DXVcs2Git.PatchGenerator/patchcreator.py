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
    repFullPath = os.path.join(storage, __getRepositoryFolder(repository), branch)
    __prepareRepository(repFullPath, branch, hash, repository)
    __copyFilesCore(repFullPath, destination, filesStr)
    os.chdir(cwd)
    pass

def __copyFilesCore(repFullPath, destination, filesStr):
    os.chdir(repFullPath)
    destinationDirName = os.path.dirname(destination)
    if not os.path.exists(destinationDirName):
        os.makedirs(destinationDirName)
    filesArr = __getFilesFromXml(filesStr)
    with zipfile.ZipFile(destination, 'w') as myzip:
        for file in filesArr:
            myzip.write(file)
    pass

def __getFilesFromXml(filesStr):
    parser = ET.XMLParser(encoding="utf-8")
    root = ET.fromstring(filesStr, parser=parser)

    result = []
    for simpe in root.iter('Simple'):
        if simpe.get('name') == 'NewPath':
            result.append(simpe.get('value'))
    return result

def errorRemoveReadonly(func, path, exc):
    excvalue = exc[1]
    if excvalue.errno == errno.EACCES:
        # change the file to be readable,writable,executable: 0777
        os.chmod(path, stat.S_IRWXU | stat.S_IRWXG | stat.S_IRWXO)
        # retry
        func(path)
    pass

def __prepareRepository(repFullPath, branch, hash, repository):
    if not os.path.exists(repFullPath):
        os.makedirs(repFullPath)
        __createNewRep(repFullPath, repository, branch)
    elif not os.path.exists(os.path.join(repFullPath, '.git', 'index')):
        shutil.rmtree(repFullPath, ignore_errors=False, onerror=errorRemoveReadonly)
        os.makedirs(repFullPath)
        __createNewRep(repFullPath, repository, branch)
    else:
        os.chdir(repFullPath)
        __updateRep(repFullPath, branch, hash)
    pass

def __getRepositoryFolder(repository):
    return repository.split(':')[-1].rsplit('.git', 1)[0]
    
def __updateRep(repFullPath, branch, hash):
    __rungit(rf"clean -d -f -x")
    __rungit(rf"fetch origin {branch}")
    __rungit("checkout -f " + hash)
    pass

def __createNewRep(repFullPath, repository, branch):
    os.chdir(repFullPath)
    __rungit(rf"clone -b {branch} --single-branch {repository} .")
    pass

def __createStorageDir(storage):
    if not os.path.isdir(storage):
        os.mkdir(storage);
    pass

def __rungit(gitargs):
    return subprocess.check_output("git.exe " + gitargs)
