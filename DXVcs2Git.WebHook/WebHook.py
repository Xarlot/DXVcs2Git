import json
import subprocess
import sys
import threading
import dotmap
import gitlab
import untangle
import time

from http.server import BaseHTTPRequestHandler, HTTPServer
from optparse import OptionParser
from subprocess import DEVNULL

class checkThread (threading.Thread):
    def __init__(self, gl, synctasks):
        threading.Thread.__init__(self)
        self.gl = gl
        self.synctasks = synctasks

    def run(self):
        while True:
            self.do_watch()
            time.sleep(600)
    pass

    def do_watch(self):
        print('start watch thread sync')
        tasks = list()
        for branch in self.synctasks.values():
            for task in branch:
                tasks.append(dotmap.DotMap(task))
        for task in tasks:
            if not task.has_key("user"):
                continue
            project = self.gl.projects.get(task.ssh)
            if project is None:
                continue
            mergerequests = project.mergerequests.list(state="opened")
            for mr in mergerequests:
                if mr.assignee is not None and mr.assignee["username"] == task.user:
                    forced = self.ForceBuild(task.task, task.user)
                    if (forced == 0):
                        print("Build forced by watcher. Task " + task.task)
                    else:
                        print("Force build by watcher failed. Task " + task.task)
        print ('end watch thread sync')
    pass

    def ForceBuild(self, taskname, username):
        p = subprocess.Popen(
            r'DXVcs2Git.FarmIntegrator.exe -t "{0}" -f "{1}"'.format(taskname, username),
            stdout=DEVNULL, stdin=DEVNULL, shell=False)
        p.communicate()
        return p.wait()
        pass

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
        if (content.object_kind == "pipeline"):
            self.process_pipeline(content)
        if (content.object_kind == 'note'):
            self.process_note(content)
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

    def hasgittoolsconfig(self, notes):
        for note in notes:
            if not note.body.startswith('<'):
                continue
            return True
        return False
        pass
    def getgittoolsconfig(self, notes):
        for note in notes:
            if not note.body.startswith('<'):
                continue
            return note
        return None
        pass
    def shouldautomergeongreenbuild(self, note):
        if not note.body.startswith('<'):
            return False
        if note.body.find(r'"AssignToSyncService" value="True"') >= 0:
            return True
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
        
        projectID = content.object_attributes.source_project_id
        project = self.gl.projects.get(projectID)
        targetProject = self.gl.projects.get(content.object_attributes.target_project_id)
        mergerequest = targetProject.mergerequests.get(content.object_attributes.iid)
        notes = mergerequest.notes.list()
        if not self.hasgittoolsconfig(notes):
            pipelines = project.pipelines.list()
            for p in pipelines:
                if p.sha == content.object_attributes.last_commit.id and p.status == "failed":
                    print("force pipeline")
                    p.retry()
                    break

        ssh = content.object_attributes.target.path_with_namespace
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
            forcebuild = self.ForceBuild(forcetaskname, user.username)
            if forcebuild == 0:
                print("build forced")
            else:
                print("force build failed")

        pass
    def ForceBuild(self, taskname, username):
        p = subprocess.Popen(r'DXVcs2Git.FarmIntegrator.exe -t "{0}" -f "{1}"'.format(taskname, username), stdout=DEVNULL, stdin=DEVNULL, shell=False )
        p.communicate()
        return p.wait()
        pass

    def process_note(self, content):
        if not content.object_attributes.noteable_type == "MergeRequest":
            return
        print("Merge request note web hook received.")
        if content.work_in_progress:
            print("Work in progress. Auto assign in disabled.")
            return

        targetProject = self.gl.projects.get(content.project_id)
        mergerequest = targetProject.mergerequests.get(content.merge_request.iid)
        notes = mergerequest.notes.list()
        if not self.hasgittoolsconfig(notes):
            print ('Merge request has no gittols config. Auto merge is disabled.')
            return
        note = self.getgittoolsconfig(notes)
        if note is None:
            return
        shouldsyncongreen = self.shouldautomergeongreenbuild(note)
        if (shouldsyncongreen is not True):
            print("Auto sync is disabled by gittools config.")
            return
        sourceProject = self.gl.projects.get(mergerequest.source_project_id)
        lastcommit = sourceProject.commits.get(content.merge_request.last_commit.id)
        if lastcommit is None:
            print("Cant find last commit id. Autosync is disabled.")
            return
        statuses = lastcommit.statuses.list()
        if statuses is None or len(statuses) == 0:
            print("last commit has no statuses. Autosync is disabled.")
        if statuses[0].status != 'success':
            return
        config = untangle.parse(note.body)
        if config.Complex.Properties.Complex.Properties.Simple[3]['value'] == "True":
            syncservice = config.Complex.Properties.Complex.Properties.Simple[1]['value']
            user = self.gl.users.list(username=syncservice)[0]
            print("user is found" + syncservice)
            if user:
                print("assign mr to sync user " + user.name)
                mergerequest.assignee_id = user.id
                mergerequest.save()
        pass
    def process_pipeline(self, content):
        print("Pipeline web hook received.")
        projectPath = content.project.path_with_namespace
        project = self.gl.projects.get(projectPath)
        mergeRequests = project.mergerequests.list(state="opened")
        for mr in mergeRequests:
            if mr.sha == content.sha and mr.merge_status == "can_be_merged" and not mr.work_in_progress:
                print("merge request is found " + mr.title)
                ssh = "{0}@{1}".format(r'git', projectPath)
                branch_path = r'branch_{0}'.format(mr.target_branch)
                synctasksnames = self.synctasks.get(branch_path)
                if not synctasksnames:
                    continue
                userName = None
                for v in synctasksnames:
                    if v.get("ssh") == ssh:
                        userName = v.get("user")
                        break
                if userName:
                    user = self.gl.users.list(username=userName)[0]
                    print("user is found" + userName)
                    if user:
                        print("assign mr to sync user")
                        mr.assignee = user
                        mr.save()
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

    check = checkThread(gl, synctasks)
    check.start()

    do_listener_work(('', 8080), gl, synctasks, supportsendingmessages, serviceuser, taskname)
    pass

if __name__ == "__main__":
   main(sys.argv[1:])