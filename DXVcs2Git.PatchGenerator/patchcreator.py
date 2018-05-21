import os
import subprocess
import xml.etree.ElementTree as ET

from sevenzutils import SevenZUtils

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
    for file in filesArr:
        SevenZUtils.make_archive(file, destination)
    pass

def __getFilesFromXml(filesStr):
    parser = ET.XMLParser(encoding="utf-8")
    root = ET.fromstring(filesStr, parser=parser)

    result = []
    for simpe in root.iter('Simple'):
        if simpe.get('name') == 'NewPath':
            result.append(simpe.get('value'))
    return result

def __prepareRepository(repFullPath, branch, hash, repository):
    if not os.path.exists(repFullPath):
        os.makedirs(repFullPath)
        __createNewRep(repFullPath, repository, branch)
    else:
        os.chdir(repFullPath)
        __updateRep(repFullPath, branch, hash)
    pass

def __getRepositoryFolder(repository):
    return repository.split(':')[-1].rsplit('.git', 1)[0]
    
def __updateRep(repFullPath, branch, hash):
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