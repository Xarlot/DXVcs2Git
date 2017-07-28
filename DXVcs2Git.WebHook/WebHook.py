import socket
import sys
import urllib.parse
from http.server import BaseHTTPRequestHandler, HTTPServer
from optparse import OptionParser
import dotmap
import gitlab
import json
import subprocess
from subprocess import DEVNULL

class HttpHandler(BaseHTTPRequestHandler):
    def do_HEAD(self):
        self.send_response(200)
        self.send_header("Content-type", "text/html")
        self.end_headers()
        print("do_head")
        pass

    def do_GET(self):
        self.send_response(200)
        print("do_get")
        pass

    def do_POST(self):
        self.send_response(200)
        self.send_header('Content-type', 'text/html')
        self.end_headers()

        content_length = int(self.headers['Content-Length'])  # <--- Gets the size of data
        post_data = self.rfile.read(content_length)  # <--- Gets the data itself

        self.parse_post(post_data)

        pass
    def parse_post(self, data):
        webhook_content = json.loads(data)
        self.process_webhook(webhook_content)
        pass

    def process_webhook(self, webhook_content):
        content = dotmap.DotMap(webhook_content)

        if (content.object_kind == "merge_request"):
            self.process_mergerequest(content)
        pass

    def is_mergerequest_opened(self, content):
        if (content.object_attributes.state == "opened" or content.object_attributes.state == "reopened"):
            return True
        return False
        pass

    def should_force_synctask(self, content):
        assignee = content.assignee.username

        if not assignee or not assignee.startswith("dxvcs2git."):
            print("Force sync rejected because assignee is not set or not sync task.")
            return False

        if content.object_attributes.work_in_progress == "true":
            print("Force sync rejected because merge request has work in process flag.")
            return False

        if content.object_attributes.merge_status == "unchecked" or content.object_attributes.merge_status == "can_be_merged":
            return True

        print("Force sync rejected because merge request can`t be merged automatically.")
        return False
        pass

    def process_mergerequest(self, content):
        print("Merge request web hook received.")
        if not self.is_mergerequest_opened(content):
            return
        user = self.gl.users.get(content.object_attributes.author_id)
        targetbranch = content.object_attributes.target_branch
        print("Merge hook action: " + content.object_attributes.action)
        print("Merge hook merge status: " + content.object_attributes.merge_status)
        print("Merge hook author: " + user.name)
        print("Merge hook target branch: " + targetbranch)
        print("Merge hook source branch: " + content.object_attributes.source_branch)

        if not self.should_force_synctask(content):
            return
        ssh = "{0}@{1}".format(r'git', content.object_attributes.target.path_with_namespace)
        branch_path = r'branch_{0}'.format(targetbranch)
        synctasksnames = self.synctasks.get(branch_path)
        if not synctasksnames:
            return
        forcetaskname = None
        for v in synctasksnames:
            if v.get("ssh") == ssh:
                forcetaskname = v.get("task")
                break

        if forcetaskname:
            forcebuild = self.ForceBuild(forcetaskname)
            if forcebuild == 0:
                print("build forced")
            else:
                print("force build failed")

        pass
    def ForceBuild(self, taskname):
        p = subprocess.Popen(r'DXVcs2Git.FarmIntegrator.exe -t "{0}"'.format(taskname), stdout=DEVNULL, stdin=DEVNULL, shell=False )
        p.communicate()
        return p.wait()
        pass

def do_listener_work(url, gitlab, synctasks, supportsendingmessages, serviceuser, taskname):
    httpd = HTTPServer(url, HttpHandler)
    httpd.RequestHandlerClass.gl = gitlab
    httpd.RequestHandlerClass.supportsendingmessages = supportsendingmessages
    httpd.RequestHandlerClass.serviceuser = serviceuser
    httpd.RequestHandlerClass.taskname = taskname
    httpd.RequestHandlerClass.synctasks = synctasks

    httpd.serve_forever()
    pass

def ParseTasks(tasks):
    with open(tasks) as json_data:
        return json.load(json_data)
    pass

def main(argv):
    parser = OptionParser()
    parser.add_option("-i", "--timeout", dest="timeout", default=30)
    parser.add_option("-t", "--task", dest="task")
    parser.add_option("-s", "--server", dest="server")
    parser.add_option("-l", "--login", dest="login", action="store", type="string", default="dxvcs2git.tester")
    parser.add_option("-r", "--repo", dest="repo")
    parser.add_option("-p", "--password", dest="password")
    parser.add_option("-a", "--auth", dest="token")
    parser.add_option("--tasklist", dest="tasklist", default="tasks.json")

    (options, args) = parser.parse_args()

    supportsendingmessages = True if options.task else False
    serviceuser = options.login
    taskname = options.task
    gl = gitlab.Gitlab(options.server, options.token, api_version=4)
    synctasks = ParseTasks(options.tasklist) if options.tasklist else {}

    do_listener_work(('', 8080), gl, synctasks, supportsendingmessages, serviceuser, taskname)
    pass

if __name__ == "__main__":
   main(sys.argv[1:])