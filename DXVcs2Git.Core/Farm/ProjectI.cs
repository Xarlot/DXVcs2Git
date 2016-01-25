using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using DevExpress.DXCCTray;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVcs2Git.UI.Farm {
    public class ServerI {
        string farm;
        public string Farm { get { return farm; } set { farm = value; } }
        string server;
        public string Server { get { return server; } set { server = value; } }
        public string HyperName {
            get {
                try {
                    return Server.Split('_')[0];
                }
                catch {
                    return "Unknown";
                }
            }
        }
        string host;
        public string Host { get { return host; } set { host = value; } }
        string vmid;
        public string Vmid { get { return vmid; } set { vmid = value; } }
        string status;
        public string Status { get { return status; } set { status = value; } }
        string project;
        public string Project { get { return project; } set { project = value; } }
        string details;
        public string Details { get { return details; } set { details = value; } }
        List<string> types;
        public List<string> Types { get { return types; } set { types = value; } }
        bool running;
        public bool Running { get { return running; } set { running = value; } }
        public object Update;
        int remained;
        public int Remained { get { return remained; } set { remained = value; } }
    }


    public class ProjectI : ProjectTagI {
        ProjectTagCollection tags_;
        [DisplayName("Tags")]
        public ProjectTagCollection tags { get { return tags_; } set { tags_ = value; } }
    }
    public class ProjectTagCollection {
        string[] tags;
        public string[] Tags {
            get { return tags; }
        }
        public ProjectTagCollection(string[] tags) {
            this.tags = tags;
        }
        public bool Contains(string tag) {
            for (int i = 0; i < tags.Length; i++) {
                if (tag == tags[i])
                    return true;
            }
            return false;
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tags.Length; i++) {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(tags[i]);
            }
            return sb.ToString();
        }
    }



    enum InfoType {
        Projects, Servers, Notifications
    };

    public class ProjectTagI {
        String farm_;
        [DisplayName("Farm")]
        public String farm { get { return farm_; } set { farm_ = value; } }
        String name_;
        [DisplayName("Name")]
        public String name { get { return name_; } set { name_ = value; } }
        String server_;
        [DisplayName("Server")]
        public String server { get { return server_; } set { server_ = value; } }
        String activity_;
        [DisplayName("Activity")]
        public String activity { get { return activity_; } set { activity_ = value; } }
        String queuename_;
        [DisplayName("QueueName")]
        public String queuename { get { return queuename_; } set { queuename_ = value; } }
        String weburl_;
        [DisplayName("WebURL")]
        public String weburl { get { return weburl_; } set { weburl_ = value; } }
        String buildstage_;
        [DisplayName("BuildStage")]
        public String buildstage { get { return buildstage_; } set { buildstage_ = value; } }
        IntegrationStatus buildstatus_;
        [DisplayName("BuildStatus")]
        public IntegrationStatus buildstatus { get { return buildstatus_; } set { buildstatus_ = value; } }
        String status_;
        [DisplayName("Status")]
        public String status { get { return status_; } set { status_ = value; } }
        DateTime lastbuilddate_;
        [DisplayName("LastBuildDate")]
        public DateTime lastbuilddate { get { return lastbuilddate_; } set { lastbuilddate_ = value; } }
        String lastbuildlabel_;
        [DisplayName("LastBuildLabel")]
        public String lastbuildlabel { get { return lastbuildlabel_; } set { lastbuildlabel_ = value; } }
        DateTime nextbuildtime_;
        [DisplayName("NextBuildTime")]
        public DateTime nextbuildtime { get { return nextbuildtime_; } set { nextbuildtime_ = value; } }
        String details_;
        [DisplayName("Details")]
        public String details { get { return details_; } set { details_ = value; } }
        Int32 icon_;
        [DisplayName(" ")]
        public Int32 icon { get { return icon_; } set { icon_ = value; } }
        String types_;
        [DisplayName("types")]
        public String types { get { return types_; } set { types_ = value; } }
        String tag_;
        [DisplayName("Tag")]
        public String tag { get { return tag_; } set { tag_ = value; } }
        string forcer_;
        [DisplayName("Forcer")]
        public string forcer { get { return forcer_; } set { forcer_ = value; } }
        BindingList<ProjectI> projectChilds_ = new BindingList<ProjectI>();
        public BindingList<ProjectI> projectChilds { get { return projectChilds_; } set { projectChilds_ = value; } }
        BindingList<ProjectI> currentProjectChilds_ = new BindingList<ProjectI>();
        public BindingList<ProjectI> currentProjectChilds { get { return currentProjectChilds_; } set { currentProjectChilds_ = value; } }
        ProjectTagI parent;
        public ProjectTagI Parent { get { return parent; } set { parent = value; } }
        public object Update;
        public bool IsBuildIfModification;
        public string Version {
            get {
                if (tag != null) {
                    string[] tags = tag.Split(',');
                    foreach (string t in tags) {
                        int v;
                        string vStr = t.Trim();
                        if (int.TryParse(vStr.Replace(".", string.Empty), out v)) {
                            return vStr;
                        }
                    }
                }
                return "N/A";
            }
        }
    }
}
