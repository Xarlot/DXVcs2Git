import socket
import sys
import urllib.parse
from http.server import BaseHTTPRequestHandler, HTTPServer
from optparse import OptionParser
import dotmap
import gitlab
import json
import ctypes

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

        print("do_post")
        pass
    def parse_post(self, data):
        json_str = str(data, 'utf-8')
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

    def should_force_synctask(self, mergerequest, content):
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
        print("Merge hook action: " + content.object_attributes.action)
        print("Merge hook merge status: " + content.object_attributes.merge_status)
        print("Merge hook author: " + user.name)
        print("Merge hook target branch: " + content.object_attributes.target_branch)
        print("Merge hook source branch: " + content.object_attributes.source_branch)

        project = self.gl.projects.get(content.object_attributes.target_project_id)
        mergerequest = project.mergerequests.get(content.object_attributes.id)

        if not self.should_force_synctask(mergerequest, content):
            return



        pass


def update_project_hook(project, hook, hook_addr):
    base_url_split = urllib.parse.urlsplit(hook.url)
    base_url = "{0.hostname}".format(base_url_split)
    base_port = "{0.port}".format(base_url_split)
    base_path = "{0.path}".format(base_url_split)

    if (base_path != "/{0}/".format(hook_addr)):
        return

    local_ip = socket.gethostbyname(socket.gethostname())
    if (base_url == local_ip):
        return
    url = list(base_url_split)
    url[1] = local_ip + ':' + base_port
    new_url = urllib.parse.urlunsplit(url)

    hook.url = new_url
    hook.save()
    print('Project {0}. Web hook updated to {1}'.format(project.http_url_to_repo, new_url))

    pass

def do_listener_work(url, gitlab, supportsendingmessages, serviceuser, taskname):
    httpd = HTTPServer(url, HttpHandler)
    httpd.RequestHandlerClass.gl = gitlab
    httpd.RequestHandlerClass.supportsendingmessages = supportsendingmessages
    httpd.RequestHandlerClass.serviceuser = serviceuser
    httpd.RequestHandlerClass.taskname = taskname

    httpd.serve_forever()
    pass

def main(argv):
    parser = OptionParser()
    parser.add_option("-w", "--webhook", default="sharedwebhook", action="store", type="string", dest="webhook")
    parser.add_option("-i", "--timeout", dest="timeout", default=30)
    parser.add_option("-t", "--task", dest="task")
    parser.add_option("-s", "--server", dest="server")
    parser.add_option("-l", "--login", dest="login", action="store", type="string", default="dxvcs2git.tester")
    parser.add_option("-r", "--repo", dest="repo")
    parser.add_option("-p", "--password", dest="password")
    parser.add_option("-a", "--auth", dest="token")
    parser.add_option("-u", "--update", dest="update", default=False)

    (options, args) = parser.parse_args()

    farmintegrator = ctypes.cdll.LoadLibrary(r"C:\GitHub\DXVcs2Git\DXVcs2Git.Core\bin\x64\Export\DXVcs2Git.Core.dll")
    farmintegrator.Start()

    supportsendingmessages = True if options.task else False
    serviceuser = options.login
    taskname = options.task
    gl = gitlab.Gitlab(options.server, options.token, api_version=4)

    do_listener_work(('', 8080), gl, supportsendingmessages, serviceuser, taskname)
    pass

if __name__ == "__main__":
   main(sys.argv[1:])