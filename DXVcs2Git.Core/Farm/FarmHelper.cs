using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Threading;
using DevExpress.CCNetSmart.Lib;
using DevExpress.DXCCTray;
using DXVcs2Git.Core;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVcs2Git.UI.Farm {
    public class FarmIntegrator {
        static readonly FarmHelper Instance;
        static Dispatcher Dispatcher { get; set; }
        static Action InvalidateCallback { get; set; }

        static FarmIntegrator() {
            Instance = new FarmHelper();
        }
        public static void Start(Dispatcher dispatcher, Action invalidateCallback) {
            Dispatcher = dispatcher;
            InvalidateCallback = invalidateCallback ?? (() => { });
            Instance.Refreshed += InstanceOnRefreshed;
            Instance.StartIntegrator();
        }
        static void InstanceOnRefreshed(object sender, EventArgs eventArgs) {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, InvalidateCallback);
        }
        public static void Stop() {
            Instance.Refreshed -= InstanceOnRefreshed;
            Instance.StopIntegrator();
        }
        public static void ForceBuild(string task) {
            if (string.IsNullOrEmpty(task))
                return;
            Instance.ForceBuild(task);
        }
        public static bool CanForceBuild(string task) {
            return Instance.IsRunning;
        }
        public static FarmStatus GetTaskStatus(string task) {
            return Instance.GetTaskStatus(task);
        }
        public static FarmExtendedStatus GetExtendedTaskStatus(string task) {
            return Instance.GetExtendedTaskStatus(task);
        }
    }

    public enum ActivityStatus {
        Unknown,
        Sleeping,
        Preparing,
        Pending,
        Building,
        Checking
    }

    public class FarmExtendedStatus {
        public string HyperName { get; set; }
        public string HyperHost { get; set; }
    }

    public class FarmStatus {
        protected bool Equals(FarmStatus other) {
            return BuildStatus == other.BuildStatus && ActivityStatus == other.ActivityStatus;
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((FarmStatus)obj);
        }
        public override int GetHashCode() {
            unchecked {
                return ((int)BuildStatus * 397) ^ (int)ActivityStatus;
            }
        }
        public FarmStatus() {
            BuildStatus = IntegrationStatus.Unknown;
            ActivityStatus = ActivityStatus.Unknown;
        }

        public IntegrationStatus BuildStatus { get; set; }
        public ActivityStatus ActivityStatus { get; set; }
        public string ActivityMessage { get; set; }
    }
    public class FarmHelper {
        public event EventHandler Refreshed;

        void RaiseRefreshed() {
            var refreshed = Refreshed;
            refreshed?.Invoke(this, EventArgs.Empty);
        }
        public FarmStatus GetTaskStatus(string task) {
            lock (this.syncLocker) {
                return CalcTaskStatus(task);
            }
        }
        public FarmExtendedStatus GetExtendedTaskStatus(string task) {
            lock (this.syncLocker) {
                return CalcExtendedTaskStatus(task);
            }
        }
        FarmExtendedStatus CalcExtendedTaskStatus(string task) {
            var farmStatus = new FarmExtendedStatus();
            ProjectTagI tag = FindTask(task);
            if (tag == null)
                return farmStatus;
            var server = FindServer(tag.farm, tag.server);
            farmStatus.HyperName = server.HyperName;
            farmStatus.HyperHost = server.Host;
            return farmStatus;
        }
        FarmStatus CalcTaskStatus(string task) {
            var farmStatus = new FarmStatus() {
                ActivityStatus = ActivityStatus.Unknown,
                BuildStatus = IntegrationStatus.Unknown,
            };
            ProjectTagI tag = FindTask(task);
            if (tag == null)
                return farmStatus;
            farmStatus.BuildStatus = tag.buildstatus;
            farmStatus.ActivityStatus = CalcActivityStatus(tag);
            farmStatus.ActivityMessage = tag.activity;
            return farmStatus;
        }
        ActivityStatus CalcActivityStatus(ProjectTagI tag) {
            string activity = tag.activity;
            if (activity == "Sleeping")
                return ActivityStatus.Sleeping;
            if (activity.StartsWith("Pending"))
                return ActivityStatus.Pending;
            if (activity.StartsWith("Preparing"))
                return ActivityStatus.Preparing;
            if (activity.StartsWith("Building"))
                return ActivityStatus.Building;
            if (activity.StartsWith("Checking"))
                return ActivityStatus.Checking;
            return ActivityStatus.Unknown;
        }
        public void ForceBuild(string task) {
            lock (syncLocker) {
                ForceBuildTag(task);
            }
        }
        void ForceBuildTag(string task) {
            ProjectTagI tag = FindTask(task);
            if (tag == null)
                return;
            ProjectTagI[] selectedRows = { tag };
            List<FarmProjectList> farmLists = GetFarmLists(new[] { tag });
            int messageProjectsCount = 0;
            StringBuilder sb = new StringBuilder();
            foreach (FarmProjectList list in farmLists) {
                foreach (string project in list.Projects) {
                    if (messageProjectsCount > 20) {
                        sb.Append("...");
                        break;
                    }
                    sb.Append($"{list.Name}\\{project}\n");
                    messageProjectsCount++;
                }
                if (messageProjectsCount > 20)
                    break;
            }
            if (CheckForceBuild(selectedRows, sb.ToString())) {
                List<string> stoppedList = new List<string>();
                foreach (FarmProjectList list in farmLists) {
                    foreach (DXCCTrayIntegrator integrator in integratorList) {
                        if (list.Name == integrator.Name) {
                            if (list.Projects.Count > 0) {
                                stoppedList.AddRange(integrator.ForceBuild(list.Projects.ToArray()));
                            }
                        }
                    }
                }
                if (stoppedList.Count > 0) {
                    StringBuilder sb2 = new StringBuilder();
                    messageProjectsCount = 0;
                    foreach (string project in stoppedList) {
                        if (messageProjectsCount > 20) {
                            sb2.Append("...");
                            break;
                        }
                        sb2.Append(project);
                        messageProjectsCount++;
                    }
                    Log.Error($"These projects was not forced, cause they are stopped:\n\n{sb2.ToString()}");
                }
            }
        }
        public bool IsRunning { get; private set; }
        bool CheckForceBuild(ProjectTagI[] projects, string projectPaths) {
            bool isShowMessage;
            if (projects.Length > 20) {
                Log.Error("\"Force Build\" is limited to 20 projects at a time.");
                return false;
            }
            bool isOk = CheckForceBuildTriggered(projects, out isShowMessage);
            if (isShowMessage) {
                return isOk;
            }
            return ShowForceBuildDialog(projectPaths);
        }
        bool ShowForceBuildDialog(string projects) {
            return true;
        }
        bool CheckForceBuildTriggered(ProjectTagI[] projects, out bool isShowMessage) {
            StringBuilder triggeredProjects = new StringBuilder();
            foreach (ProjectTagI proj in projects) {
                if (proj.IsBuildIfModification) {
                    triggeredProjects.AppendLine(proj.name);
                }
            }
            isShowMessage = false;
            if (triggeredProjects.Length > 0) {
                isShowMessage = true;
                return true;
            }
            return true;
        }
        List<FarmProjectList> GetFarmLists(ProjectTagI[] selectedRows) {
            Dictionary<string, bool> projectExists = new Dictionary<string, bool>();
            List<FarmProjectList> farmLists = new List<FarmProjectList>();
            GetFarmListsInternal(projectExists, farmLists, selectedRows);
            return farmLists;
        }
        void GetFarmListsInternal(Dictionary<string, bool> projectExists, List<FarmProjectList> farmLists, ProjectTagI[] selectedRows) {
            foreach (ProjectTagI row in selectedRows) {
                string farm = row.farm;
                string projectName = row.name;
                bool farmExists = false;
                foreach (FarmProjectList list in farmLists) {
                    if (list.Name == farm) {
                        farmExists = true;
                        string fullName = GetProjectTagName(farm, projectName, string.Empty);
                        if (!projectExists.ContainsKey(fullName)) {
                            projectExists.Add(fullName, true);
                            list.Projects.Add(projectName);
                        }
                    }
                }
                if (!farmExists) {
                    projectExists.Add(GetProjectTagName(farm, projectName, string.Empty), true);
                    FarmProjectList newList = new FarmProjectList(farm);
                    newList.Projects.Add(projectName);
                    farmLists.Add(newList);
                }
            }
        }
        string GetProjectTagName(string farm, string project, string tag) {
            return string.Format(CultureInfo.InvariantCulture, "{0}|#|{1}|#|{2}", farm, project, tag);
        }

        ProjectTagI FindTask(string task) {
            return this.projectTagTable.FirstOrDefault(x => x.name == task);
        }
        ServerI FindServer(string farm, string project) {
            ServerI server;
            if (serverDict.TryGetValue(new ProjectKey() {Farm = farm, Project = project}, out server))
                return server;
            return this.serverTable.FirstOrDefault(x => string.Compare(x.HyperName, project, StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        #region inner farm shit
        class ProjectKeyComparer : IEqualityComparer<ProjectKey> {
            public bool Equals(ProjectKey x, ProjectKey y) {
                return x.Farm == y.Farm && x.Project == y.Project;
            }
            public int GetHashCode(ProjectKey obj) {
                return obj.Farm.GetHashCode() + obj.Project.GetHashCode();
            }
            public static readonly ProjectKeyComparer Default = new ProjectKeyComparer();
        }
        struct ProjectKey {
            public string Farm;
            public string Project;
            public ProjectKey(string farm, string project) {
                Project = project;
                Farm = farm;
            }
        }
        readonly Dictionary<ProjectKey, ProjectI> projectDict = new Dictionary<ProjectKey, ProjectI>(ProjectKeyComparer.Default);
        readonly DXCCTrayIntegratorList integratorList = new DXCCTrayIntegratorList();
        public DXCCTrayIntegratorList IntegratorList {
            get { return integratorList; }
        }
        readonly List<ProjectI> projectTable = new List<ProjectI>();
        class SkipUpdateController {
            const int stopUpdateTimeoutMilliseconds = 3000;
            readonly Stopwatch stopUpdateTimeoutTimer = new Stopwatch();
            public bool SkipUpdate() {
                if (stopUpdateTimeoutTimer.ElapsedMilliseconds > 0 && stopUpdateTimeoutTimer.ElapsedMilliseconds < stopUpdateTimeoutMilliseconds) {
                    return true;
                }
                stopUpdateTimeoutTimer.Stop();
                return false;
            }
            public void PullBySkipUpdate() {
                stopUpdateTimeoutTimer.Restart();
            }
        }

        readonly SkipUpdateController skipUpdateControllerProjects = new SkipUpdateController();
        readonly SkipUpdateController skipUpdateControllerServers = new SkipUpdateController();
        readonly BindingList<BuildNotificationViewInfo> buildNotifications = new BindingList<BuildNotificationViewInfo>();
        readonly Dictionary<ProjectKey, ServerI> serverDict = new Dictionary<ProjectKey, ServerI>(ProjectKeyComparer.Default);
        readonly Dictionary<TagKey, ProjectTagI> projectTagsDict = new Dictionary<TagKey, ProjectTagI>(TagKeyComparer.Default);
        readonly List<ProjectTagI> projectTagTable = new List<ProjectTagI>();
        readonly List<ServerI> serverTable = new List<ServerI>();

        readonly object syncLocker = new object();

        static FarmHelper() {
            DXCCTrayConfiguration.LoadConfiguration();
        }

        public FarmHelper() {
            foreach (string url in DXCCTrayConfiguration.FarmList) {
                integratorList.Add(url);
            }
        }
        public void StartIntegrator() {
            if (IsRunning)
                return;
            foreach (DXCCTrayIntegrator integrator in IntegratorList) {
                integrator.Start();
                integrator.OnChanged += Integrator_OnChanged;
            }
            IsRunning = true;
        }
        delegate void UpdateDelegate();

        private void Integrator_OnChanged(DXCCTrayIntegrator sender, bool servers, bool projects, bool queue, bool notification) {
            UpdateDelegate update = delegate () {
                //ToolTipControlInfo tip = toolTipController.ActiveObjectInfo;
                if (servers)
                    integrator_OnServersChanged(sender);
                if (projects)
                    integrator_OnProjectListChanged(sender);
                if (queue) { }
                if (notification)
                    integrator_OnNotificationListChanged(sender);
                integrator_OnProjectDetailsAndForcerUpdated(sender);

                if (projects || notification)
                    RaiseRefreshed();
                //if (tip != null) {
                //ToolTipControllerGetActiveObjectInfoEventArgs newArgs = new ToolTipControllerGetActiveObjectInfoEventArgs((Control)toolTipController.ActiveControlClient,
                //    null, null, ((Control)toolTipController.ActiveControlClient).PointToClient(Cursor.Position));
                //toolTipController_GetActiveObjectInfo(null, newArgs);
                //if (newArgs.Info != null && newArgs.Info.Object == tip.Object) {
                //    toolTipController.ShowHint((ToolTipControlInfo)null);
                //    toolTipController.ShowHint(newArgs.Info);
                //}
                //}
            };
            try {
                lock (this.syncLocker) {
                    update();
                }
            }
            catch {
            }
        }
        void integrator_OnProjectDetailsAndForcerUpdated(DXCCTrayIntegrator integrator) {
            try {
                UpdateProjectsDetails(integrator);
                UpdateForcers(integrator);
                UpdateCurrentSubProjects(integrator);
                SinchronizeProjectTagsTable();
                //gridControlProjects.RefreshDataSource();
                //projectTable.FireChanged();
                //projectTagTable.FireChanged();
            }
            catch (Exception ex) {
                Log.Error("error", ex);
                throw;
            }
        }
        struct TagKey {
            public string Farm;
            public string Project;
            public string Tag;
            public TagKey(string farm, string project, string tag) {
                Tag = tag;
                Project = project;
                Farm = farm;
            }
        }
        class TagKeyComparer : IEqualityComparer<TagKey> {
            public bool Equals(TagKey x, TagKey y) {
                return x.Farm == y.Farm && x.Project == y.Project && x.Tag == y.Tag;
            }
            public int GetHashCode(TagKey obj) {
                return obj.Farm.GetHashCode() + obj.Project.GetHashCode() + obj.Tag.GetHashCode();
            }
            public static TagKeyComparer Default = new TagKeyComparer();
        }
        static void CopyProjectRowInfo(ProjectTagI row, ProjectI sourceRow) {
            row.server = sourceRow.server;
            row.activity = sourceRow.activity;
            row.queuename = sourceRow.queuename;
            row.weburl = sourceRow.weburl;
            row.buildstage = sourceRow.buildstage;
            row.buildstatus = sourceRow.buildstatus;
            row.status = sourceRow.status;
            row.lastbuilddate = sourceRow.lastbuilddate;
            row.lastbuildlabel = sourceRow.lastbuildlabel;
            row.nextbuildtime = sourceRow.nextbuildtime;
            row.details = sourceRow.details;
            row.icon = sourceRow.icon;
            row.types = sourceRow.types;
            row.forcer = sourceRow.forcer;
            row.currentProjectChilds = sourceRow.currentProjectChilds;
            row.projectChilds = sourceRow.projectChilds;
        }

        ProjectTagI UpdateTag(TagKey fullName, ProjectI sourceRow) {
            ProjectTagI row;
            if (projectTagsDict.TryGetValue(fullName, out row)) {
                CopyProjectRowInfo(row, sourceRow);
            }
            else {
                row = new ProjectTagI();
                row.farm = sourceRow.farm;
                row.name = sourceRow.name;
                row.tag = fullName.Tag;
                CopyProjectRowInfo(row, sourceRow);
                projectTagTable.Add(row);
                projectTagsDict.Add(fullName, row);
            }
            return row;
        }


        void SinchronizeProjectTagsTable() {
            object update = new object();
            foreach (ProjectI sourceRow in projectTable) {
                if (sourceRow.tags.Tags.Length == 0) {
                    TagKey fullName = new TagKey(sourceRow.farm, sourceRow.name, string.Empty);
                    UpdateTag(fullName, sourceRow).Update = update;
                }
                else {
                    foreach (string tag in sourceRow.tags.Tags) {
                        TagKey fullName = new TagKey(sourceRow.farm, sourceRow.name, tag);
                        UpdateTag(fullName, sourceRow).Update = update;
                    }
                }
            }
            for (int i = 0; i < projectTagTable.Count; i++) {
                ProjectTagI row = projectTagTable[i];
                if (row.Update != update) {
                    projectTagsDict.Remove(new TagKey(row.farm, row.name, row.tag));
                    projectTagTable.RemoveAt(i);
                    i--;
                }
            }
        }

        private void UpdateCurrentSubProjects(DXCCTrayIntegrator integrator) {
            if (skipUpdateControllerProjects.SkipUpdate()) {
                return;
            }
            Dictionary<ProjectTagI, List<ProjectI>> currentChilds = new Dictionary<ProjectTagI, List<ProjectI>>();
            foreach (ProjectInfo info in integrator.ProjectList) {
                ProjectI row;
                if (projectDict.TryGetValue(new ProjectKey(integrator.Name, info.Name), out row)) {
                    ProjectI parentRow;
                    if (!string.IsNullOrEmpty(row.forcer) && projectDict.TryGetValue(new ProjectKey(integrator.Name, row.forcer), out parentRow) && parentRow.projectChilds.Count == 0) {
                        if (!currentChilds.ContainsKey(parentRow)) {
                            currentChilds[parentRow] = new List<ProjectI>();
                        }
                        currentChilds[parentRow].Add(row);
                    }
                }
            }
            foreach (ProjectInfo info in integrator.ProjectList) {
                ProjectI parentRow;
                if (projectDict.TryGetValue(new ProjectKey(integrator.Name, info.Name), out parentRow)) {
                    if (!currentChilds.ContainsKey(parentRow)) {
                        if (parentRow.currentProjectChilds.Count > 0) {
                            parentRow.currentProjectChilds.Clear();
                            CollapsDetail(parentRow.farm, parentRow.name);
                        }
                    }
                    else {
                        for (int i = 0; i < parentRow.currentProjectChilds.Count; i++) {
                            if (!currentChilds[parentRow].Contains(parentRow.currentProjectChilds[i])) {
                                parentRow.currentProjectChilds.RemoveAt(i--);
                            }
                        }
                        for (int i = 0; i < currentChilds[parentRow].Count; i++) {
                            if (!parentRow.currentProjectChilds.Contains(currentChilds[parentRow][i])) {
                                parentRow.currentProjectChilds.Insert(0, currentChilds[parentRow][i]);
                                currentChilds[parentRow][i].Parent = parentRow;
                                i++;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateForcers(DXCCTrayIntegrator integrator) {
            if (skipUpdateControllerProjects.SkipUpdate()) {
                return;
            }
            foreach (ProjectInfo info in integrator.ProjectList) {
                ProjectI row;
                if (projectDict.TryGetValue(new ProjectKey(integrator.Name, info.Name), out row)) {
                    if (integrator.CCServerAdditionalInfo.ProjectForcer.ContainsKey(row.name)) {
                        row.forcer = integrator.CCServerAdditionalInfo.ProjectForcer[row.name];
                    }
                    else {
                        row.forcer = string.Empty;
                    }
                }
            }
        }

        private void UpdateProjectsDetails(DXCCTrayIntegrator integrator) {
            object updated = new object();
            foreach (ProjectInfo info in integrator.ProjectList) {
                ProjectI row;
                if (projectDict.TryGetValue(new ProjectKey(integrator.Name, info.Name), out row)) {
                    row.details = info.Details;
                    ServerI serverRow;
                    if (serverDict.TryGetValue(new ProjectKey(row.farm, row.server), out serverRow)) {
                        if (string.IsNullOrEmpty(row.details))
                            serverRow.Details = row.activity;
                        else
                            serverRow.Details = row.details;
                        serverRow.Update = updated;
                    }
                }
            }
            lock (integrator.ServerList) {
                foreach (ServerI serverRow in serverTable) {
                    foreach (ServerInfo newInfo in integrator.ServerList) {
                        if (newInfo.Name == serverRow.Server) {
                            if (UpdateRemainedServerTime(newInfo, serverRow, integrator)) {
                                serverRow.Update = updated;
                                serverRow.Remained = newInfo.RemainedTime;
                            }
                        }
                    }
                    if (serverRow.Farm == integrator.Name && serverRow.Update != updated) {
                        serverRow.Details = string.Empty;
                    }
                }
            }
        }

        protected void integrator_OnNotificationListChanged(DXCCTrayIntegrator integrator) {
            List<string> balloonMessages = new List<string>();
            bool balloonFailState = false;
            BuildNotification lastNotification = null;
            lock (integrator.Notifications) {
                foreach (BuildNotification bn in integrator.Notifications) {
                    bool bnExists = false;
                    lock (buildNotifications) {
                        foreach (BuildNotificationViewInfo bnOld in buildNotifications) {
                            if (bnOld.Notification.BuildUrl == bn.BuildUrl && bnOld.Notification.CreateTime == bn.CreateTime) {
                                bnExists = true;
                                break;
                            }
                        }
                    }
                    if (bnExists) {
                        continue;
                    }
                    string projectName;
                    string buildName;
                    DXCCTrayHelper.ParseBuildUrl(bn.BuildUrl, out projectName, out buildName);
                    balloonMessages.Add(String.Format("{0} - {1}", projectName, (bn.BuildChangeStatus == BuildChangeStatus.None ? bn.BuildStatus.ToString() : bn.BuildChangeStatus.ToString())));
                    if (bn.BuildStatus != BuildIntegrationStatus.Success) {
                        balloonFailState = true;
                    }
                    lastNotification = bn;
                    buildNotifications.Insert(0, new BuildNotificationViewInfo(bn));
                }
                integrator.Notifications.Clear();
                if (balloonMessages.Count > 0) {
                    ShowBalloonTip(string.Join(Environment.NewLine, balloonMessages), balloonFailState, balloonMessages.Count > 1 ? null : lastNotification);
                }
            }
            lock (buildNotifications) {
                while (buildNotifications.Count > notificationsMaxCount) {
                    buildNotifications.RemoveAt(notificationsMaxCount);
                }
            }
            //if (needSetTopRowIndex) {
            //    gridViewNotifications.TopRowIndex = 0;
            //}
        }
        const int notificationsMaxCount = 100;
        void integrator_OnServersChanged(DXCCTrayIntegrator integrator) {
            try {
                if (skipUpdateControllerServers.SkipUpdate()) {
                    return;
                }
                object update = new object();

                //ServerI focusedRow = gridViewServers.GetFocusedRow() as ServerI;
                //bool focusedGroupped = gridViewServers.IsGroupRow(gridViewServers.FocusedRowHandle);
                //int[] selectedRowHandles = gridViewServers.GetSelectedRows();
                //List<ServerI> selectedRows = new List<ServerI>();
                //foreach (int srh in selectedRowHandles) {
                //    if (!gridViewServers.IsGroupRow(srh)) {
                //        selectedRows.Add((ServerI)gridViewServers.GetRow(srh));
                //    }
                //}
                lock (integrator.ServerList) {
                    foreach (ServerInfo newInfo in integrator.ServerList) {
                        ServerI row;
                        if (!serverDict.TryGetValue(new ProjectKey(integrator.Name, newInfo.Name), out row)) {
                            row = new ServerI();
                            row.Farm = integrator.Name;
                            row.Server = newInfo.Name;
                            serverTable.Add(row);
                            serverDict.Add(new ProjectKey(row.Farm, row.Server), row);
                        }
                        row.Farm = integrator.Name;
                        row.Types = new List<string>(newInfo.Types);
                        row.Running = newInfo.IsRunning;
                        row.Host = newInfo.Host;
                        row.Vmid = newInfo.VMID;
                        row.Status = newInfo.Status;
                        //row.Project = (string.IsNullOrEmpty(newInfo.ProjectName)) ? newInfo.Creator == null ? "Free" : GetPersonalServerTitle(newInfo.Creator) : newInfo.ProjectName;
                        UpdateRemainedServerTime(newInfo, row, integrator);
                        row.Remained = newInfo.RemainedTime;
                        row.Update = update;
                        //if (IsServerForUser(row)) {
                        //    CustomServerProcess(newInfo, integrator, newServer);
                        //}
                        row.Remained = newInfo.RemainedTime;
                    }
                }
                for (int i = 0; i < serverTable.Count; i++) {
                    ServerI row = serverTable[i];
                    if (row.Farm == integrator.Name && row.Update != update) {
                        serverDict.Remove(new ProjectKey(row.Farm, row.Server));
                        serverTable.RemoveAt(i);
                        i--;
                    }
                }
                //if (!focusedGroupped && serverTable.Contains(focusedRow)) {
                //    int focusedRowHandle = gridViewServers.GetRowHandle(serverTable.IndexOf(focusedRow));
                //    gridViewServers.FocusedRowHandle = focusedRowHandle;
                //}
                //gridViewServers.ClearSelection();
                //foreach (ServerI selected in selectedRows) {
                //    if (serverTable.Contains(selected)) {
                //        gridViewServers.SelectRow(gridViewServers.GetRowHandle(serverTable.IndexOf(selected)));
                //    }
                //}
            }
            catch (Exception ex) {
                Log.Error("error", ex);
                throw;
            }
        }
        public void StopIntegrator() {
            if (!IsRunning)
                return;

            foreach (DXCCTrayIntegrator integrator in IntegratorList) {
                integrator.OnChanged -= Integrator_OnChanged;
                integrator.Stop();
            }
            IsRunning = false;
        }
        void integrator_OnProjectListChanged(DXCCTrayIntegrator integrator) {
            try {
                object update = new object();
                List<ProjectI> projectsWithChildsToReload = new List<ProjectI>();
                foreach (ProjectInfo info in integrator.ProjectList) {
                    ProjectI row;
                    if (!projectDict.TryGetValue(new ProjectKey(integrator.Name, info.Name), out row)) {
                        row = new ProjectI();
                        row.name = info.Name;
                        ProjectTagCollection ptc = new ProjectTagCollection(info.Tags.ToArray());
                        row.tags = ptc;
                        row.tag = info.TagsString;
                        projectTable.Add(row);
                        projectDict.Add(new ProjectKey(integrator.Name, info.Name), row);
                    }
                    else {
                        if (row.tag != info.TagsString) {
                            ProjectTagCollection ptc = new ProjectTagCollection(info.Tags.ToArray());
                            row.tags = ptc;
                            row.tag = info.TagsString;
                        }
                    }
                    row.farm = integrator.Name;
                    row.server = info.Server;
                    row.activity = info.Activity.ToString();
                    row.queuename = info.QueueName;
                    row.weburl = info.WebURL;
                    row.buildstage = info.BuildStage;
                    row.buildstatus = info.BuildStatus;
                    row.status = GetStatus(info.Status);
                    row.lastbuilddate = info.LastBuildDate;
                    row.lastbuildlabel = info.LastBuildLabel;
                    row.nextbuildtime = info.NextBuildTime;
                    row.types = info.TypesString;
                    row.IsBuildIfModification = info.IsBuildIfModification;
                    if (row.projectChilds.Count == info.ProjectsChilds.Count) {
                        foreach (ProjectI child in row.projectChilds) {
                            if (!info.ProjectsChilds.Contains(child.name)) {
                                projectsWithChildsToReload.Add(row);
                                break;
                            }
                        }
                    }
                    else {
                        projectsWithChildsToReload.Add(row);
                    }
                    row.Update = update;
                }
                ReloadProjectChilds(integrator, projectsWithChildsToReload);
                for (int i = 0; i < projectTable.Count; i++) {
                    ProjectI row = projectTable[i];
                    if (row.farm == integrator.Name && row.Update != update) {
                        projectDict.Remove(new ProjectKey(row.farm, row.name));
                        projectTable.RemoveAt(i);
                        i--;
                    }
                }
            }
            catch (Exception ex) {
                Log.Error("error", ex);
                throw;
            }
        }
        private void ReloadProjectChilds(DXCCTrayIntegrator integrator, List<ProjectI> projectsWithChildsToReload) {
            foreach (ProjectI parentProject in projectsWithChildsToReload) {
                ProjectInfo parentProjectInfo = null;
                foreach (ProjectInfo info in integrator.ProjectList) {
                    if (info.Name == parentProject.name) {
                        parentProjectInfo = info;
                        break;
                    }
                }
                List<ProjectI> projectsToAddToParentList = new List<ProjectI>(parentProject.projectChilds);
                parentProject.projectChilds.Clear();
                foreach (string childName in parentProjectInfo.ProjectsChilds) {
                    ProjectI child;
                    projectDict.TryGetValue(new ProjectKey(integrator.Name, childName), out child);
                    if (projectsToAddToParentList.Contains(child)) {
                        projectsToAddToParentList.Remove(child);
                    }
                    parentProject.projectChilds.Add(child);
                    child.Parent = parentProject;
                    projectTable.Remove(child);
                }
                foreach (ProjectI parentProjectToAdd in projectsToAddToParentList) {
                    parentProjectToAdd.Parent = null;
                    projectTable.Add(parentProjectToAdd);
                }
                if (parentProject.projectChilds.Count == 0) {
                    CollapsDetail(parentProject.farm, parentProject.name);
                }
            }
        }
        void CollapsDetail(ProjectTagI row) {
        }
        void CollapsDetail(string farm, string projectName) {
        }
        string GetStatus(ProjectIntegratorState projectIntegratorState) {
            switch (projectIntegratorState) {
                case ProjectIntegratorState.Running:
                    return "Running";
                case ProjectIntegratorState.Stopped:
                    return "Stopped";
                case ProjectIntegratorState.Stopping:
                    return "Stopping";
                default:
                    return projectIntegratorState.ToString();
            }
        }
        private bool UpdateRemainedServerTime(ServerInfo newInfo, ServerI row, DXCCTrayIntegrator integrator) {
            //if (IsServerForUser(row) && newInfo.RemainedTime > 0) {
            //    row.Details = String.Format("Remained {0} min", newInfo.RemainedTime);
            //    if (newInfo.Creator.Equals(DXCCTrayConfiguration.WorkUserName, StringComparison.InvariantCultureIgnoreCase) && newInfo.RemainedTime < minimumRemainedTime && row.Remained > 0 && row.Remained == minimumRemainedTime) {
            //        row.Remained = newInfo.RemainedTime;
            //        if (XtraMessageBox.Show(String.Format("Do you want to prolong server \"{0}\" for 1 hour?", row.Server), "DXCCTray", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes) {
            //            integrator.UpdateServerTime(row.Server, 60);
            //        }
            //    }
            //    return true;
            //}
            return false;
        }
        void ShowBalloonTip(string message, bool failState, BuildNotification singleNotification) {
            //notifyIcon.Tag = singleNotification;
            //notifyIcon.ShowBalloonTip(balloonShowTimeout, string.Empty, message, failState ? ToolTipIcon.Error : ToolTipIcon.Info);
            //notifyIcon.Visible = true;
            //if (!Active) {
            //    StartTrayNotificationAnimation();
            //}
        }
        #endregion

    }
}
