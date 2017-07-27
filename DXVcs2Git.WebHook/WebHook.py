import socket
import sys
import urllib.parse
from http.server import BaseHTTPRequestHandler, HTTPServer
from optparse import OptionParser
import gitlab


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
        print("do_post")
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

def do_listener_work(url):
    httpd = HTTPServer(url, HttpHandler)
    httpd.serve_forever()
    pass

def main(argv):
    parser = OptionParser()
    parser.add_option("-w", "--webhook", default="sharedwebhook", action="store", type="string", dest="webhook")
    parser.add_option("-i", "--timeout", dest="timeout", default=30)
    parser.add_option("-t", "--task", dest="task")
    parser.add_option("-s", "--server", dest="server")
    parser.add_option("-l", "--login", dest="login")
    parser.add_option("-r", "--repo", dest="repo")
    parser.add_option("-p", "--password", dest="password")
    parser.add_option("-a", "--auth", dest="token")
    parser.add_option("-u", "--update", dest="update", default=False)

    (options, args) = parser.parse_args()

    supportsendingmessages = True if options.task else False

    gl = gitlab.Gitlab(options.server, options.token, api_version=4)
    if options.update:
        projects = gl.projects.list(all=True)
        for project in projects:
            for hook in project.hooks.list():
                update_project_hook(project, hook, options.webhook)
            print(project)

    do_listener_work(('', 8080))
    pass

if __name__ == "__main__":
   main(sys.argv[1:])