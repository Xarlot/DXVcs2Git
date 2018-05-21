import cgi
import datetime
import json
import re
import sys
import threading
import time
import urllib
import patchcreator
from collections import namedtuple
from enum import Enum
from http.server import HTTPServer, BaseHTTPRequestHandler
from optparse import OptionParser
from queue import Queue
from threading import Thread

Chunk = namedtuple('Chunk', 'hash data')
ChunkStatusInfo = namedtuple('ChunkStatusInfo', 'hash data status link dt')

class ChunkStatus(Enum):
    NonRunning = 0,
    Running = 1
    Failed = 2
    Success = 3

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
                chunk = Chunk(hash = hash, data = data)
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
            chunk_status = self.server.get_chunkstatusinfo(hash)
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

            self.end_headers()

        else:
            self.send_response(200)
            self.send_header('Content-type', 'text/html')
            self.end_headers()

        pass

class MyHttpServer(HTTPServer):
    def get_chunkstatusinfo(self, hash):
        self.lock.acquire()
        try:
            return self.tasks[hash]
        except:
            return None
        finally:
            self.lock.release()
        pass
    def set_chunkstatusinfo(self, hash, chunkstatus):
        self.lock.acquire()
        try:
            self.tasks[hash] = chunkstatus
        finally:
            self.lock.release()
        pass
    def do_magic(self, chunkStatus):
        print("some magic")

        data = json.loads(chunkStatus.data)
        repo = data.get('repo')
        branch = data.get('branch')
        content = data.get('content')
        hash = chunkStatus.hash

        patchcreator.copyFiles(self.cache, repo, branch, hash, self.storage, content)

        return ChunkStatusInfo(hash=chunkStatus.hash, link='test', status=ChunkStatus.Success, data = chunkStatus.data, dt = chunkStatus.dt)
        pass
    def process_queue(self):
        print('thread started')
        while(True):
            chunk = self.queue.get(block=True)
            if (chunk == None):
                time.sleep(50)
                continue
            hash = chunk.hash
            data = chunk.data
            chunkStatus = self.get_chunkstatusinfo(hash)
            if chunkStatus == None:
                chunkStatus = ChunkStatusInfo(hash=hash, status=ChunkStatus.NonRunning, link='', data=data, dt=datetime.datetime.now())
                self.set_chunkstatusinfo(hash, chunkStatus)

            if chunkStatus.status is not ChunkStatus.NonRunning:
                continue

            chunkStatus = self.do_magic(chunkStatus)
            self.set_chunkstatusinfo(hash, chunkStatus)
    pass
def main(argv):
    parser = OptionParser()
    (options, args) = parser.parse_args()

    cache = r'c:\repo'
    storage = r'c:\repo2'

    url = ('', 8000)
    httpd = MyHttpServer(url, HttpHandler)
    httpd.lock = threading.Lock()
    httpd.queue = Queue()
    httpd.tasks = {}
    httpd.cache = cache
    httpd.storage = storage

    thread = Thread(target=httpd.process_queue)
    thread.start()

    httpd.serve_forever()

    pass

if __name__ == "__main__":
   main(sys.argv[1:])