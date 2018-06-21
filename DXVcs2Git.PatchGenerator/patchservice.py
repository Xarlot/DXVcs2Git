import cgi
import concurrent
import datetime
import json
import re
import threading
import urllib
import traceback
from collections import namedtuple
from concurrent.futures import as_completed
from enum import Enum
from http.server import HTTPServer, BaseHTTPRequestHandler
from optparse import OptionParser
from queue import Queue
from threading import Thread
import sys
import os
from pebble import ProcessPool, ProcessExpired
import patchcreator
import base64

Chunk = namedtuple('Chunk', 'hash data')
ChunkStatusInfo = namedtuple('ChunkStatusInfo', 'hash status link dt chunk error')
DelayedTasksHash = namedtuple('DelayedTasksHash', 'repo branch')
RunningTask = namedtuple('RunningTask', 'repo branch hash')

class ChunkStatus(Enum):
    NonRunning = 0,
    Running = 1
    Failed = 2
    Success = 3

def string2base64(s):
    return base64.b64encode(s.encode('utf-8')).decode('utf-8')

def process_chunk_execute_on_git(runningtask, cache, link, content):
    patchcreator.copyFiles(cache, runningtask.repo, runningtask.branch, runningtask.hash, link, content)
    pass


class HttpHandler(BaseHTTPRequestHandler):
    def do_HEAD(self):
        self.send_response(200)
        self.send_header("content-type", "text/html")
        self.end_headers()
        print("do_head")
        pass

    def do_POST(self):
        if None != re.search('/api/v1/createpatch/*', self.path):
            ctype, pdict = cgi.parse_header(self.headers.get('content-type'))
            if (ctype == 'application/json'):
                hash = urllib.parse.urlsplit(self.path).path.split('/')[-1]
                length = int(self.headers.get('content-length'), 0)
                data = self.rfile.read(length)
                chunk = Chunk(hash=hash, data=data)
                self.server.queue.put(chunk)
            self.send_response(200)
            self.end_headers()
        else:
            self.send_response(403)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
        print("do_get")
        pass

    def do_GET(self):
        if None != re.search('/api/v1/getpatch/*', self.path):
            hash = urllib.parse.urlsplit(self.path).path.split('/')[-1]
            chunk_status = self.server.get_taskstatusinfo(hash)
            if chunk_status == None:
                self.send_response(403)
                self.send_header('Content-type', 'text/html')
                self.end_headers()
                return

            self.send_response(200)
            self.send_header('Content-type', 'text/html')
            self.send_header('timestamp', chunk_status.dt)
            status = chunk_status.status
            if status == ChunkStatus.NonRunning:
                self.send_header('chunk_status', 'notrunning')
            elif status == ChunkStatus.Success:
                self.send_header('chunk_status', 'success')
                self.send_header('link', chunk_status.link)
            elif status == ChunkStatus.Running:
                self.send_header('chunk_status', 'running')
            elif status == ChunkStatus.Failed:
                self.send_header('chunk_status', 'failed')
                self.send_header('error', string2base64(chunk_status.error))

            self.end_headers()

        else:
            self.send_response(200)
            self.send_header('Content-type', 'text/html')
            self.end_headers()
        pass


class MyHttpServer(HTTPServer):
    def get_taskstatusinfo(self, hash):
        self.tasks_lock.acquire()
        try:
            return self.tasks[hash]
        except:
            return None
        finally:
            self.tasks_lock.release()
        pass

    def set_taskstatusinfo(self, hash, taskstatus):
        self.tasks_lock.acquire()
        try:
            self.tasks[hash] = taskstatus
        finally:
            self.tasks_lock.release()
        pass

    def process_chunk(self, chunk):
        hash = chunk.hash
        print(rf"process_chunk for chunk {hash}")

        try:
            dt = datetime.datetime.now()
            link = os.path.join(self.storage, rf"{hash}.zip")
            data = json.loads(chunk.data)
            repo = data.get('repo')
            branch = data.get('branch')
            content = data.get('content')

            self.synctasks_lock.acquire()
            try:
                taskstatus = self.get_taskstatusinfo(hash)
                if taskstatus != None and taskstatus.status != ChunkStatus.NonRunning:
                    print(rf"Task {hash} is already registered and executed")
                    return taskstatus
                taskstatus = ChunkStatusInfo(hash=hash, link=link, status=ChunkStatus.NonRunning, dt=dt, chunk=chunk,
                                             error=None)
                self.set_taskstatusinfo(hash, taskstatus)

                runningtask_onrepo = next((x for x in self.runningtasks if x.repo == repo), None)
                runningtask = RunningTask(repo=repo, branch=branch, hash=hash)
                if runningtask_onrepo == None:
                    print(rf"Task with hash {hash} on repo {repo} branch {branch} is running.")

                    self.runningtasks.append(runningtask)
                    taskstatus = taskstatus._replace(status=ChunkStatus.Running)
                    self.set_taskstatusinfo(hash, taskstatus)
                else:
                    print(rf"Task with hash {hash} on repo {repo} branch {branch} is delayed.")
                    self.delayedtasks(runningtask)
                    return

            finally:
                self.synctasks_lock.release()

            cachedpath = os.path.join(self.storage, rf"{hash}.zip")
            if os.path.isfile(cachedpath):
                print(rf"Task with hash {hash} found in cache")
                return taskstatus._replace(status=ChunkStatus.Success)
            process_on_git_future = self.pool.schedule(process_chunk_execute_on_git,
                                                       args=[runningtask, self.cache, link, content],
                                                       timeout=self.git_timeout)
            try:
                process_on_git_future.result()
                return taskstatus._replace(status=ChunkStatus.Success)
            except TimeoutError as ex:
                stack = traceback.print_stack()
                return taskstatus._replace(status=ChunkStatus.Failed, error='Task TimeOut'.join(traceback.format_exception(etype=type(ex), value=ex, tb=ex.__traceback__)))
            except ProcessExpired as ex:
                return taskstatus._replace(status=ChunkStatus.Failed, error='Process Expired'.join(traceback.format_exception(etype=type(ex), value=ex, tb=ex.__traceback__)))
            except Exception as ex:
                return taskstatus._replace(status=ChunkStatus.Failed, error=''.join(traceback.format_exception(etype=type(ex), value=ex, tb=ex.__traceback__)))
        except Exception as ex:
            return ChunkStatusInfo(hash=chunk.hash, link=link, status=ChunkStatus.Failed, dt=dt, chunk=chunk, error=''.join(traceback.format_exception(etype=type(ex), value=ex, tb=ex.__traceback__)))
        pass

    def process_chunk_completed(self, chunkfuture):
        chunkstatus = chunkfuture.result()
        hash = chunkstatus.hash

        if chunkstatus.status == ChunkStatus.Running:
            print(rf"Task processing for hash {hash} is running already.")
            return
        if (chunkstatus.status == ChunkStatus.NonRunning):
            print(rf"Task processing for hash {hash} is delayed already.")
            return

        if (chunkstatus.status == ChunkStatus.Failed):
            print(rf"Task processing for hash {hash} is failed.")
        if (chunkstatus.status == ChunkStatus.Success):
            print(rf"Task processing for hash {hash} is completed.")

        self.synctasks_lock.acquire()
        try:
            runningtask_onrepo = next((x for x in self.runningtasks if x.hash == hash), None)
            if runningtask_onrepo != None:
                self.runningtasks.remove(runningtask_onrepo)
                delayedtask_onrepo = next((x for x in self.delayedtasks if x.repo == runningtask_onrepo.repo), None)
                if delayedtask_onrepo != None:
                    delayedtaskstatus = self.get_taskstatusinfo(delayedtask_onrepo.hash)
                    self.queue.put(delayedtaskstatus.chunk)

            self.set_taskstatusinfo(hash, chunkstatus)
        finally:
            self.synctasks_lock.release()

        pass

    def process_queue(self):
        print('process queue started')

        with concurrent.futures.ThreadPoolExecutor() as executor:
            while True:
                chunk = self.queue.get(block=True)
                chunk_future = executor.submit(self.process_chunk, chunk)
                chunk_future.add_done_callback(self.process_chunk_completed)

    pass


def main(argv):
    parser = OptionParser()
    parser.add_option("-i", "--timeout", dest="timeout", default=600)
    parser.add_option("-w", "--workers", dest="workers", default=5)
    parser.add_option("-c", "--cache", dest="cache", default='cache')
    parser.add_option("-s", "--storage", dest="storage", default='storage')

    (options, args) = parser.parse_args()

    url = ('', 8000)
    httpd = MyHttpServer(url, HttpHandler)
    httpd.tasks_lock = threading.Lock()
    httpd.delayedtasks_lock = threading.Lock()
    httpd.synctasks_lock = threading.Lock()
    httpd.queue = Queue()
    httpd.tasks = {}
    httpd.delayedtasks = []
    httpd.runningtasks = []
    httpd.cache = options.cache
    httpd.storage = options.storage
    httpd.pool = ProcessPool(max_workers=options.workers)
    httpd.git_timeout = options.timeout

    thread = Thread(target=httpd.process_queue)
    thread.start()

    httpd.serve_forever()

    pass


if __name__ == "__main__":
    main(sys.argv[1:])
