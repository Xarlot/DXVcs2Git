import argparse
import sys
import gitlab
import socket
import urllib.parse

def update_project_hook(project, hook):
    base_url_split = urllib.parse.urlsplit(hook.url)
    base_url = "{0.hostname}".format(base_url_split)
    base_port = "{0.port}".format(base_url_split)

    local_ip = socket.gethostbyname(socket.gethostname())
    if (base_url == local_ip):
        pass
    url = list(base_url_split)
    url[1] = local_ip + ':' + base_port
    new_url = urllib.parse.urlunsplit(url)

    # hook.url = new_url
    # hook.save()
    print('Project {0}. Web hook updated to {1}'.format(project.http_url_to_repo, new_url))

    pass

def main(argv):
    parser = argparse.ArgumentParser()
    parser.parse_args()

    gl = gitlab.Gitlab('http://gitserver', 'FZ8YxktKW95wsxybykxs', api_version=4)
    projects = gl.projects.list()
    for project in projects:
        for hook in project.hooks.list():
            update_project_hook(project, hook)
        print(project)
    pass

if __name__ == "__main__":
   main(sys.argv[1:])