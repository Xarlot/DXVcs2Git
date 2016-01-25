using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ThoughtWorks.CruiseControl.Remote;
using System.Xml;
using DevExpress.CCNetSmart.Lib;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;
using DXVcs2Git.Core;

namespace DevExpress.DXCCTray {
    public class ServerInfo {
        public string Name;
        public string Host;
        public string VMID;
        public string Status;
        public string ProjectName;
        public bool IsRunning = true;
        public string Guid;
        public string Creator;
        public int RemainedTime;
        List<string> types = new List<string>();

        public List<string> Types {
            get { return types; }
        }

        public bool Equals(ServerInfo info) {
            bool equalsTypes = true;

            if(Types.Count != info.Types.Count)
                equalsTypes = false;
            else {
                for(int i = 0; i < Types.Count; i++) {
                    if(Types[i] != info.Types[i]) {
                        equalsTypes = false;
                        break;
                    }
                }
            }

            return Name == info.Name && Host == info.Host
                && Status == info.Status && ProjectName == info.ProjectName && IsRunning == info.IsRunning && RemainedTime == info.RemainedTime && equalsTypes;
        }
        public override string ToString() {
            return string.Format("Name = {0}; Status = {1}; Project = {2}; {3}", Name, Status, ProjectName ?? string.Empty, IsRunning ? "Running" : "Stopped");
        }
    }

    public class ProjectInfo {
        public string Name;
        public string Server;
        public ProjectActivity Activity;
        public string QueueName;
        public string WebURL;
        public string BuildStage;
        public IntegrationStatus BuildStatus;
        public ProjectIntegratorState Status;
        public DateTime LastBuildDate;
        public string LastBuildLabel;
        public DateTime NextBuildTime;
        public string CurrentMessage;
        public string TypesString;
        public string TagsString;
        public bool IsBuildIfModification;
        public List<string> ProjectsChilds = new List<string>();
        List<string> types = new List<string>();

        static readonly Dictionary<string, TimeSpan> lastBuildDurationDictionary = new Dictionary<string, TimeSpan>();

        public List<string> Types {
            get { return types; }
        }

        List<string> tags = new List<string>();
        public List<string> Tags {
            get { return tags; }
        }

        bool isBuildInProgress;
        bool isLoadedFromDictionary;
        TimeSpan lastBuildDuration = TimeSpan.MaxValue;
        DateTime lastBuildStart = DateTime.MaxValue;
        DateTime lastBuildStop = DateTime.MaxValue;

        bool monitorBuildFailureNotFromStart = false;

        DateTime firstBuildFailure = DateTime.MaxValue;

        bool monitorPendingNotFromStart = false;
        DateTime lastPendingStart = DateTime.MaxValue;

        public static byte[] GetLastBuildDurationDictSaveData() {
            lock(lastBuildDurationDictionary) {
                using(MemoryStream ms = new MemoryStream()) {
                    using(DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true)) {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(ds, lastBuildDurationDictionary);
                    }
                    return ms.ToArray();
                }
            }
        }
        public static void LoadLastBuildDurationDict(byte[] data) {
            if(data == null) {
                lock(lastBuildDurationDictionary) {
                    lastBuildDurationDictionary.Clear();
                }
                return;
            }
            Dictionary<string, TimeSpan> tempDict;
            using(MemoryStream ms = new MemoryStream(data)) {
                using(DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress)) {
                    BinaryFormatter bf = new BinaryFormatter();
                    tempDict = (Dictionary<string, TimeSpan>)bf.Deserialize(ds);
                }
            }
            lock(lastBuildDurationDictionary) {
                lastBuildDurationDictionary.Clear();
                foreach(KeyValuePair<string, TimeSpan> pair in tempDict) {
                    lastBuildDurationDictionary[pair.Key] = pair.Value;
                }
            }
        }

        public void TryLoadLastBuildDurationFromDict() {
            if(isLoadedFromDictionary)
                return;
            lock(lastBuildDurationDictionary) {
                if(isLoadedFromDictionary)
                    return;
                TimeSpan ts;
                if(lastBuildDurationDictionary.TryGetValue(WebURL, out ts)) {
                    lastBuildDuration = ts;
                }
                isLoadedFromDictionary = true;
            }
        }

        void SaveLastBuldsDurationToDict() {
            lock(lastBuildDurationDictionary) {
                lastBuildDurationDictionary[WebURL] = lastBuildDuration;
            }
        }

        bool IsBuildingOrPrepareRemoteProject(ProjectActivity activity) {
            return activity.IsBuilding() || activity.ToString() == SmartProjectActivity.PreparingRemoteDir.ToString();
        }

        public void SetBuildStatus(ProjectActivity activity, IntegrationStatus buildStatus) {
            masterDetails = null;
            if(Activity != null) {
                if(!IsBuildingOrPrepareRemoteProject(Activity) && IsBuildingOrPrepareRemoteProject(activity)) {
                    isBuildInProgress = true;
                    lastBuildStart = DateTime.Now;
                }
                if(isBuildInProgress && IsBuildingOrPrepareRemoteProject(Activity) && !IsBuildingOrPrepareRemoteProject(activity) && buildStatus == IntegrationStatus.Success) {
                    isBuildInProgress = false;
                    lastBuildStop = DateTime.Now;
                    lastBuildDuration = lastBuildStop - lastBuildStart;
                    SaveLastBuldsDurationToDict();
                }
            } else {
                if(IsBuildingOrPrepareRemoteProject(activity)) {
                    isBuildInProgress = true;
                    lastBuildStart = DateTime.Now;
                    lastBuildStop = DateTime.MaxValue;
                    lastBuildDuration = TimeSpan.MaxValue;
                }
            }

            if(Activity != SmartProjectActivity.TypePending && activity == SmartProjectActivity.TypePending) {
                if(Activity == null)
                    monitorPendingNotFromStart = true;
                else
                    monitorPendingNotFromStart = false;

                lastPendingStart = DateTime.Now;
            }
            if(Activity == SmartProjectActivity.TypePending && activity != SmartProjectActivity.TypePending) {
                monitorPendingNotFromStart = false;
                lastPendingStart = DateTime.MaxValue;
            }
            if(buildStatus != IntegrationStatus.Success && (BuildStatus == IntegrationStatus.Success || Activity == null)) {
                monitorBuildFailureNotFromStart = (Activity == null);
                firstBuildFailure = DateTime.Now;
            }

            if(Activity != activity)
                Activity = activity;
            BuildStatus = buildStatus;
        }

        public string Details {
            get {
                if(BuildStatus == IntegrationStatus.Failure || BuildStatus == IntegrationStatus.Exception) {
                    DateTime now = DateTime.Now;
                    if(now > firstBuildFailure) {
                        TimeSpan failureTime = now - firstBuildFailure;
                        string additional = string.Empty;
                        if(monitorBuildFailureNotFromStart)
                            additional = " more than";
                        string masterDetails = GetMasterDetails();
                        return string.Format("Failure{0} {1} {2}{3}", additional, new CCTimeFormatter(failureTime), string.IsNullOrEmpty(masterDetails) ? string.Empty : "- ", masterDetails);
                    }
                }
                return GetMasterDetails();
            }
        }
        string masterDetails;
        string GetMasterDetails() {
            if(masterDetails != null)
                return masterDetails;
            string message = string.Empty;

            if(CurrentMessage.Length > 0) {
                message = " - " + CurrentMessage;
            }

            if(Activity.IsSleeping()) {
                if(NextBuildTime == DateTime.MaxValue)
                    message = "Not auto-triggered" + message;
                else
                    message = string.Format("Next: {0:T}", NextBuildTime) + message;
            } else {
                if(Activity == SmartProjectActivity.TypePending && lastPendingStart != DateTime.MaxValue) {
                    TimeSpan pendingTime = DateTime.Now - lastPendingStart;
                    string additional = string.Empty;
                    if(monitorPendingNotFromStart)
                        additional = " more than";
                    return string.Format("Pending{0} {1}", additional, new CCTimeFormatter(pendingTime)) + message;
                }

                if(isBuildInProgress && (lastBuildStart != DateTime.MaxValue) && IsBuildingOrPrepareRemoteProject(Activity)) {
                    TimeSpan durationRemaining = DurationRemaining;
                    if(durationRemaining != TimeSpan.MaxValue) {
                        if(durationRemaining <= TimeSpan.Zero)
                            return string.Format("Taking {0} longer", new CCTimeFormatter(durationRemaining.Negate())) + message;
                        return string.Format("Remains {0}", new CCTimeFormatter(durationRemaining)) + message;
                    } else {
                        TimeSpan buildTime = DateTime.Now - lastBuildStart;
                        return string.Format("Building {0}", new CCTimeFormatter(buildTime)) + message;
                    }
                }
            }
            masterDetails = message;
            return message;
        }

        TimeSpan DurationRemaining {
            get {
                if(!(isBuildInProgress && !(lastBuildDuration == TimeSpan.MaxValue))) {
                    return TimeSpan.MaxValue;
                }
                return (TimeSpan)((lastBuildStart + lastBuildDuration) - DateTime.Now);
            }
        }
        public override string ToString() {
            return string.Format("Name = {0}; Activity = {1}; QueueName = {2}; WebURL = {3}", Name, Activity, QueueName, WebURL);
        }

        class CCTimeFormatter {
            readonly TimeSpan timeSpan;

            public CCTimeFormatter(TimeSpan timeSpan) {
                this.timeSpan = timeSpan;
            }

            bool AddIfNeeded(StringBuilder sb, int value, string type) {
                if(value != 0) {
                    sb.AppendFormat("{0} {1} ", value, type);
                    return true;
                }
                return false;
            }

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                if(!AddIfNeeded(sb, this.timeSpan.Days, "d"))
                    if(!AddIfNeeded(sb, this.timeSpan.Hours, "h"))
                        if(!AddIfNeeded(sb, this.timeSpan.Minutes, "m"))
                            AddIfNeeded(sb, this.timeSpan.Seconds, "sec");
                return sb.ToString().Trim();
            }
        }
    }

    public class QueueInfo {
        public string Name;
        public QueueInfoType Type;
        public string Queue;
        public bool Exists = false;

        public override bool Equals(object obj) {
            QueueInfo info = obj as QueueInfo;
            if(info == null)
                return false;
            return Name == info.Name && Type == info.Type && Queue == info.Queue;
        }

        public override int GetHashCode() {
            StringBuilder resultBuilder = new StringBuilder();
            resultBuilder.Append(Name);
            resultBuilder.Append(Type);
            resultBuilder.Append(Queue);
            String result = resultBuilder.ToString();
            return result.GetHashCode();
        }

    }

    public enum QueueInfoType {
        Queue,
        Project,
        Farm
    }

    public class ProjectQueueInfo {
        public string Project;
        public string Queue;
    }

    public class DXCCTrayIntegratorList : IEnumerable<DXCCTrayIntegrator> {
        List<DXCCTrayIntegrator> list = new List<DXCCTrayIntegrator>();


        public DXCCTrayIntegrator this[int index] {
            get {
                return list[index];
            }
        }


        public void SetActiveMode(bool state) {
            foreach(DXCCTrayIntegrator integrator in list) {
                integrator.ActiveMode = state;
            }
        }

        public int Count {
            get { return list.Count; }
        }

        public void Clear() {
            list.Clear();
        }

        public bool Contains(DXCCTrayIntegrator integrator) {
            return list.Contains(integrator);
        }

        public bool ContainsName(string url) {
            foreach(DXCCTrayIntegrator integrator in list) {
                if(integrator.Url == url)
                    return true;
            }
            return false;
        }

        public void Add(string url) {
            if(ContainsName(url))
                return;
            list.Add(new DXCCTrayIntegrator(url));
        }

        public void RemoveAt(int index) {
            list.RemoveAt(index);
        }

        public IEnumerator<DXCCTrayIntegrator> GetEnumerator() {
            return new DXCCTrayIntegratorListEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return new DXCCTrayIntegratorListEnumerator(this);
        }
    }

    public class DXCCTrayIntegratorListEnumerator : IEnumerator<DXCCTrayIntegrator> {
        int counter = -1;
        DXCCTrayIntegratorList list;

        public DXCCTrayIntegratorListEnumerator(DXCCTrayIntegratorList list) {
            this.list = list;
        }

        public DXCCTrayIntegrator Current {
            get {
                try {
                    return list[counter];
                } catch(Exception) {
                    throw new InvalidOperationException();
                }
            }
        }

        public void Dispose() { }

        object System.Collections.IEnumerator.Current {
            get {
                try {
                    return (Object)list[counter];
                } catch(Exception) {
                    throw new InvalidOperationException();
                }
            }
        }

        public bool MoveNext() {
            counter++;
            return counter < list.Count;
        }

        public void Reset() {
            counter = -1;
        }
    }
    public delegate void GetMachineEventHandler(SmartServerInfo server);
    public class DXCCTrayIntegrator {
        bool isRunning = false;
        bool internalIsAlive = false;
        int waitTimeForWCFCallSeconds = 3;
        int waitTime = 5000;
        int[] waitTimes;
        int waitTimeIndex = 0;
        string name = string.Empty;

        bool activeMode = true;

        public bool IsAlive {
            get { return internalIsAlive; }
            set {
                internalIsAlive = value;
                if(!internalIsAlive)
                    prevDiffRequest = DateTime.MinValue;
            }
        }

        public bool ActiveMode {
            get { return activeMode; }
            set { activeMode = value; }
        }
        Thread thread;
        System.Timers.Timer watchDogTimer = new System.Timers.Timer();

        public delegate void TrayIntegratorEventHandler(DXCCTrayIntegrator sender, bool servers, bool projects, bool queue, bool notifications);

        public event TrayIntegratorEventHandler OnChanged;

        List<ServerInfo> serverList = new List<ServerInfo>();
        List<ProjectInfo> projectList = new List<ProjectInfo>();
        Dictionary<string, ProjectInfo> projectDict = new Dictionary<string, ProjectInfo>();
        List<QueueInfo> queueList = new List<QueueInfo>();

        public List<ServerInfo> ServerList {
            get { return serverList; }
        }

        public List<ProjectInfo> ProjectList {
            get { return projectList; }
        }

        public List<QueueInfo> QueueList {
            get { return queueList; }
        }

        public string Name {
            get { return name; }
        }

        public int WaitTime {
            get { return waitTime; }
            set {
                waitTime = value;
                waitTimes = new int[] { waitTime, waitTime, waitTime, waitTime * 6, waitTime * 6, waitTime * 12, waitTime * 12, waitTime * 48 };
                watchDogTimer.Interval = waitTimes[waitTimeIndex] * 3;
            }
        }

        string url;
        public string Url { get { return url; } }
        string urlWCF = string.Empty;
        string nearestUrlWCF = string.Empty;
        public string NearestUrlWCF { get { return nearestUrlWCF; } }

        System.Timers.Timer sendServerIAmHereTimer;
        const int sendServerIAmHereIntervalMinutes = 10;
        List<string> allUserNames = new List<string>();
        public DXCCTrayIntegrator(string url) {
            this.url = url;
            waitTimeForWCFCallSeconds = 3;
            waitTime = 5000;
            waitTimes = new int[] { waitTime, waitTime, waitTime, waitTime * 6, waitTime * 6, waitTime * 12, waitTime * 12, waitTime * 48 };
            watchDogTimer.Interval = waitTime * 3;
            waitTimeIndex = 0;
            wcfConnectionTimer.Elapsed += WcfConnectionTimer_Elapsed;            
            watchDogTimer.Elapsed += new System.Timers.ElapsedEventHandler(watchDogTimer_Elapsed);
            checkMulticastTimer.Elapsed += new System.Timers.ElapsedEventHandler(checkMulticastTimer_Elapsed);
            InitUserNames();

            sendServerIAmHereTimer = new System.Timers.Timer(TimeSpan.FromMinutes(sendServerIAmHereIntervalMinutes).TotalMilliseconds);
            sendServerIAmHereTimer.Elapsed += new System.Timers.ElapsedEventHandler(sendServerIAmHereTimer_Elapsed);
            sendServerIAmHereTimer.Start();
        }
        private void InitUserNames() {
            allUserNames.Add(Environment.UserName);
            if(!allUserNames.Contains(DXCCTrayConfiguration.WorkUserName)) {
                allUserNames.Add(DXCCTrayConfiguration.WorkUserName);
            }
            string[] hgUserNames = FindMercurialLogins();
            foreach(string hgUserName in hgUserNames) {
                if(!string.IsNullOrEmpty(hgUserName)) {
                    if(!allUserNames.Contains(hgUserName)) {
                        allUserNames.Add(hgUserName);
                    }
                    if(hgUserName.Contains(".")) {
                        string userNameWithoutDots = hgUserName.Replace(".", " ");
                        if(!allUserNames.Contains(userNameWithoutDots)) {
                            allUserNames.Add(userNameWithoutDots);
                        }
                    }
                }
            }
            Log.Message("Found names: " + string.Join(";", allUserNames));
        }
        bool CheckUserName(string userName) {
            foreach(string user in allUserNames) {
                if(user.Equals(userName, StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }
        void sendServerIAmHereTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            try {
                foreach(string user in allUserNames) {
                    SmartCruiseManager.IAMHere(user);
                }
            } catch(Exception exc) {
                Log.Message(exc.ToString());
            }
        }
        string[] FindMercurialLogins() {
            string mercurialConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mercurial.ini");
            List<string> names = new List<string>();
            if(File.Exists(mercurialConfigFile)) {
                try {
                    foreach(string line in File.ReadAllLines(mercurialConfigFile)) {
                        int startIndex = line.IndexOf("username", StringComparison.InvariantCultureIgnoreCase);
                        if(startIndex < 0) {
                            continue;
                        }
                        startIndex = line.IndexOf("=", startIndex + "username".Length);
                        if(startIndex < 0) {
                            break;
                        }
                        startIndex++;
                        int endIndex = line.IndexOf("<", startIndex);
                        if(endIndex < 0) {
                            endIndex = line.Length - 1;
                        } else {
                            endIndex--;
                        }
                        string newName = line.Substring(startIndex, endIndex - startIndex + 1).TrimEnd().TrimStart();
                        if(!names.Contains(newName))
                            names.Add(newName);
                    }
                } catch(Exception exc) {
                    Log.Message(exc.ToString());
                }
            }
            return names.ToArray();
        }
        public void SaveProjectMessages() {
            CruiseServerSnapshot snapshot = SmartCruiseManager.GetCruiseServerSnapshot();
            List<string> projectMessages = new List<string>();
            foreach(ProjectStatus status in snapshot.ProjectStatuses) {
                if(status.BuildStatus != IntegrationStatus.Success && !string.IsNullOrEmpty(status.CurrentMessage)) {
                    projectMessages.Add(string.Format("{0}{1}{2}", status.Name, SmartConstants.Delimiter, status.CurrentMessage));
                }
            }
            if(projectMessages.Count > 0) {
                File.WriteAllLines("ProjectMessages.txt", projectMessages);
            }
        }
        void watchDogTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            try {
                Log.Message("Watch dog has rose.");
                watchDogTimer.Stop();

                if(isRunning) {
                    StopAbort();
                    thread.Join(3000);
                    Start();
                }
            } catch(Exception ex) {
                Log.Message(ex.ToString());
            }
        }

        public void Start() {
            if(!isRunning) {
                isRunning = true;
                thread = new Thread(new ThreadStart(Run));
                thread.IsBackground = true;
                thread.Start();
            } else
                throw new InvalidOperationException("Integrator already started");
        }

        public void Stop() {
            if(udpProcessor != null) {
                StopMulticastMode();
            }
            isRunning = false;
        }
        public void StopMulticastMode() {
            if(udpProcessor.IsReceiving) {
                udpProcessor.StopReceiveMessages();
            }
            udpProcessor.reseiveMessageCompleted -= new UDPMulticastingPacketProcessor.ReseiveMessageEventHandler(udpProcessor_reseiveMessageCompleted);
            udpProcessor.Close();
            udpProcessor = null;
            checkMulticastTimer.Stop();
        }

        public void StopAbort() {
            Stop();
            thread.Abort();
        }
        System.Timers.Timer checkMulticastTimer = new System.Timers.Timer();

        const double checkMulticastPeriod = 20000;
        double[] multicastRestartPeriods = new double[] { 1, 30000, 180000, 300000, 600000, 600000, 1800000 };

        UDPMulticastingPacketProcessor udpProcessor;
        bool requestMode = false;
        public bool MulticastMode { get { return !requestMode; } }
        void Run() {
            while(SmartCruiseManager == null) {
                ClearLists();
                IsAlive = false;
                Thread.Sleep(waitTime);
            }
            while(true) {
                try {
                    if(string.IsNullOrEmpty(this.name))
                        this.name = SmartCruiseManager.GetFarmName();
                    break;
                } catch(Exception exc) {
                    Log.Error("exception", exc);
                    Thread.Sleep(waitTime);
                }
            }
            IsAlive = true;
            if(!requestMode) {
                try {
                    RestartMulticastTimer();
                    string multicastAddress = SmartCruiseManager.GetMulticastAddress();
                    RunMulticastMode(multicastAddress);
                    return;
                } catch(Exception exc) {
                    Log.Error("exception", exc);
                    if(udpProcessor != null) {
                        StopMulticastMode();
                    }
                }
                checkMulticastTimer.Stop();
            }
            requestMode = true;
            RunRequestMode();
        }
        ISmartCruiseManager smartCruiseManager = null;
        System.Timers.Timer wcfConnectionTimer = new System.Timers.Timer(20000);
        bool CheckCruiseManagerWorking(ISmartCruiseManager cruiseManager) {
            if(cruiseManager == null || !WCFHelper.IsClientAvailable<ISmartCruiseManager>(cruiseManager)) {
                return false;
            }
            try {
                cruiseManager.IAMHere(DXCCTrayConfiguration.WorkUserName);
                return true;
            } catch {
                return false;
            }
        }
        DateTime lastAccessToSmartCruiseManager = DateTime.MinValue;
        const int reconnectProxyIntervalMinutes = 1;
        bool needProxy = false;
        bool connectedToProxy = false;
        string lastConnectedCCPath = string.Empty;
        string lastConnectedCCProxyPath = string.Empty;
        System.Diagnostics.Stopwatch smartCruiseNotWorkingTimer = System.Diagnostics.Stopwatch.StartNew();
        public ISmartCruiseManager SmartCruiseManager {
            get {
                wcfConnectionTimer.Stop();
                try {
                    if((!needProxy || connectedToProxy) && CheckCruiseManagerWorking(smartCruiseManager))
                        return smartCruiseManager;
                    connectedToProxy = false;
                    this.nearestUrlWCF = string.Empty;
                    if(string.IsNullOrEmpty(urlWCF)) {
                        urlWCF = GetSmartCruiseManagerWCFUrl();
                    }
                    if(string.IsNullOrEmpty(urlWCF)) {
                        return null;
                    }
                    WCFHelper.PrepareWCFClient<ISmartCruiseManager>(ref smartCruiseManager, urlWCF, waitTimeForWCFCallSeconds, true);
                    if(!CheckCruiseManagerWorking(smartCruiseManager)) {
                        Log.Message("Smart Cruise Manager not working: " + urlWCF);
                        if(smartCruiseNotWorkingTimer.Elapsed.TotalSeconds == 1) 
                            Log.Message(Environment.StackTrace);
                        smartCruiseNotWorkingTimer.Restart();
                        lastConnectedCCPath = string.Empty;
                        return null;
                    } else if(lastConnectedCCPath != urlWCF) {
                        Log.Message("Connected to Cruise Control: " + urlWCF);
                        lastConnectedCCPath = urlWCF;
                    }
                    needProxy = !IsTCPAddressInLAN(urlWCF);
                    if(needProxy && (DateTime.Now - lastAccessToSmartCruiseManager).TotalMinutes > reconnectProxyIntervalMinutes) {
                        lastAccessToSmartCruiseManager = DateTime.Now;
                    } else {
                        this.nearestUrlWCF = urlWCF;
                        return smartCruiseManager;
                    }
                    string proxy = smartCruiseManager.GetProxy();

                    if(string.IsNullOrEmpty(proxy)) {
                        Log.Message("Proxy not found");
                        lastConnectedCCProxyPath = string.Empty;
                        return smartCruiseManager;
                    }
                    ISmartCruiseManager smartCruiseManagerProxy = null;
                    WCFHelper.PrepareWCFClient<ISmartCruiseManager>(ref smartCruiseManagerProxy, proxy, waitTimeForWCFCallSeconds, true);
                    if(lastConnectedCCProxyPath != proxy) {
                        Log.Message("Connected to proxy: " + proxy);
                        lastConnectedCCProxyPath = proxy;
                    }
                    this.smartCruiseManager = smartCruiseManagerProxy;
                    this.nearestUrlWCF = proxy;
                    connectedToProxy = true;
                    return smartCruiseManager;
                } catch(Exception exc) {
                    Log.Error("exception", exc);
                    return null;
                } finally {
                    wcfConnectionTimer.Start();
                }
            }
        }
        void CloseWCFConnection() {
            wcfConnectionTimer.Stop();
            if(smartCruiseManager == null)
                return;
            try {
                WCFHelper.Close<ISmartCruiseManager>(smartCruiseManager);
            } catch(Exception exc) {
                Log.Error("exception", exc);
            }
        }
        void WcfConnectionTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            CloseWCFConnection();
        }
        private string GetSmartCruiseManagerWCFUrl() {
            try {
                ICruiseManager cruiseManager = new RemoteCruiseManagerFactory().GetCruiseManager(Url);
                urlWCF = cruiseManager.GetStatisticsDocument(SmartConstants.GetWCFManagerUrl);
                if(string.IsNullOrEmpty(urlWCF))
                    Log.Message("Created WCF url is empty: " + Url);
                return urlWCF;
            } catch(SocketException) {
                Log.Message("Error: Can't connect to " + Url);
            } catch(Exception exc) {
                Log.Error("exception", exc);
            }
            return string.Empty;
        }

        bool IsTCPAddressInLAN(string address) {
            string proxyHost = NetworkHelper.GetHostFromTcpSource(address);
            if(proxyHost == null) {
                Log.Message("Incorrect tcp address: " + address);
                return false;
            }
            return NetworkHelper.IsHostInLAN(proxyHost);
        }
        void RunMulticastMode(string multicastAddress) {
            IsAlive = true;
            string[] parts = multicastAddress.Split(':');
            string source = new Uri(string.IsNullOrEmpty(nearestUrlWCF) ? Url : nearestUrlWCF).Host;
            udpProcessor = new UDPMulticastingPacketProcessor(parts[0], int.Parse(parts[1]), source);
            udpProcessor.reseiveMessageCompleted += new UDPMulticastingPacketProcessor.ReseiveMessageEventHandler(udpProcessor_reseiveMessageCompleted);
            
            if(!udpProcessor.IsReceiving) {
                udpProcessor.StartReceiveMessagesAsync();
            }
            Log.Message(string.Format("Activated Multicast Mode for: {0} ({1}) source: {2}", Name, multicastAddress, source));
            isRunning = true;
        }
        void RunRequestMode() {
            Log.Message("Activated Request Mode for: " + Name);
            while(isRunning) {
                try {
#if !DEBUG
                    watchDogTimer.Start();
#endif
                    try {
                        IsAlive = SmartCruiseManager != null;
                    } catch(Exception exc) {
                        Log.Error("exception", exc);
                        IsAlive = false;
                    }

                    bool hasServersChanges = false;
                    bool hasProjectsChanges = false;
                    bool hasQueuesChanges = false;
                    bool notificationsChanged = false;

                    if(isRunning && IsAlive) {
                        FillListsSmart(out hasProjectsChanges, out hasQueuesChanges, out notificationsChanged, out hasServersChanges);
                    }

                    watchDogTimer.Stop();

                    if(IsAlive) {
                        waitTimeIndex = 0;
                        if(!checkMulticastTimer.Enabled) {
                            Log.Message(string.Format("RunRequestMode - RestartMulticastTimer.  multicastTryCounter:{0}", multicastTryCounter));
                            RestartMulticastTimer();
                        }
                        if(OnChanged != null) {
                            OnChanged(this, hasServersChanges, hasProjectsChanges, hasQueuesChanges, notificationsChanged);
                        }
                    } else {
                        checkMulticastTimer.Stop();
                        multicastTryCounter = 0;
                        ClearLists();
                        if(waitTimeIndex != waitTimes.Length - 1) {
                            waitTimeIndex++;
                            Log.Message(string.Format("Server \"{0}\" error. Set wait time to {1} s", Name, (waitTimes[waitTimeIndex] / 1000)));
                        } else {
                            Log.Message(string.Format("Server \"{0}\" error. Wait time is max ({1} s)", Name, (waitTimes[waitTimeIndex] / 1000)));
                        }
                    }
                    Thread.Sleep(waitTimes[waitTimeIndex]);
                } catch(ThreadAbortException) {
#if !DEBUG
                    watchDogTimer.Stop();
#endif
                    return;
                } catch {
#if !DEBUG
                    watchDogTimer.Stop();
#endif
                    checkMulticastTimer.Stop();
                    multicastTryCounter = 0;
                    isRunning = false;
                    Thread.Sleep(waitTime);
                }
            }
        }
        void checkMulticastTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            checkMulticastTimer.Stop();
            RestartAnotherMode();
        }
        private void RestartAnotherMode() {
            StopAbort();
            if(!requestMode) {
                Log.Message("Multicast deactivated for: " + Name);
            } else {
                Log.Message(string.Format("{0} try multicast: {1}", multicastTryCounter, Name));
            }
            if(requestMode || (!requestMode && multicastTryCounter > 0)) {
                requestMode = !requestMode;
            } else {
                multicastTryCounter++;
            }
            thread.Join(3000);
            Start();
        }
        int multicastTryCounter = 0;
        private void RestartMulticastTimer() {
            if(!requestMode) {
                checkMulticastTimer.Interval = checkMulticastPeriod;
            } else {
                checkMulticastTimer.Interval = multicastRestartPeriods[multicastTryCounter];
                if(multicastTryCounter < multicastRestartPeriods.Length - 1) {
                    multicastTryCounter++;
                }
            }
            checkMulticastTimer.Stop();
            checkMulticastTimer.Start();
        }
        void udpProcessor_reseiveMessageCompleted(byte[] reseivedData, string messageName) {
            requestMode = false;
            multicastTryCounter = 0;
            RestartMulticastTimer();
            StreamReader servers = null;
            CruiseServerSnapshot snapshot = null;
            BuildNotification[] notifications = null;
            CCServerAdditionalInfo ccServerAddInfo = null;

            try {
                if(messageName == SmartConstants.SnapshotMessageName) {
                    snapshot = (CruiseServerSnapshot)SmartCCNetHelper.DeserializeObject(reseivedData);
                }
            } catch(Exception exc) {
                Log.Error("exception", exc);
            }
            try {
                if(messageName == SmartConstants.ServersMessageName) {
                    servers = SmartCCNetHelper.DecompressStringData(reseivedData);
                }
            } catch(Exception exc) {
                Log.Error("exception", exc);
            }
            try {
                if(messageName == SmartConstants.AdditionalInformationMessageName) {
                    ccServerAddInfo = (CCServerAdditionalInfo)SmartCCNetHelper.DeserializeObject(reseivedData);
                }
            } catch(Exception exc) {
                Log.Error("exception", exc);
            }
            try {
                if(messageName == SmartConstants.BuildNotificationsformationMessageName) {
                    notifications = (BuildNotification[])SmartCCNetHelper.DeserializeObject(reseivedData);
                }
            } catch(Exception exc) {
                Log.Error("exception", exc);
            }
            bool hasServersChanges = false;
            bool hasProjectsChanges = false;
            bool hasQueuesChanges = false;
            bool notificationsChanged = false;

            if(servers != null) {
                hasServersChanges = FillServerList(servers);
            } else {
                hasServersChanges = false;
            }
            if(snapshot != null) {
                hasProjectsChanges = UpdateProjectList(snapshot.ProjectStatuses);
                if(IsAlive) {
                    hasQueuesChanges = FillQueueList(snapshot.QueueSetSnapshot.Queues);
                }
            }
            if(ccServerAddInfo != null) {
                FillAdditionalInfo(ccServerAddInfo);
            }
            if(notifications != null) {
                notificationsChanged = FillNotifications(notifications);
            }
            if(IsAlive) {
                if(OnChanged != null) {
                    OnChanged(this, hasServersChanges, hasProjectsChanges, hasQueuesChanges, notificationsChanged);
                }
            } else {
                ClearLists();
            }
        }

        CCServerAdditionalInfo ccServerAdditionalInfo = new CCServerAdditionalInfo();
        public CCServerAdditionalInfo CCServerAdditionalInfo {
            get { return ccServerAdditionalInfo; }
        }
        public const string UsersGroupQuieue = "Users";
        private void FillAdditionalInfo(CCServerAdditionalInfo ccServerAdditionalInfo) {
            this.ccServerAdditionalInfo = ccServerAdditionalInfo;
        }
        public List<BuildNotification> Notifications = new List<BuildNotification>();
        bool FillNotifications(BuildNotification[] notifications) {
            bool result = false;
            lock(Notifications) {
                foreach(BuildNotification bn in notifications) {
                    if(CheckUserName(bn.Recipient)) {
                        try {
                            SmartCruiseManager.BuildNotificationRecieved(bn.Guid);
                        } catch(Exception ex) {
                            Log.Error("exception", ex);
                            break;
                        }
                        bn.Farm = Name;
                        Notifications.Add(bn);
                        result = true;
                    }
                }
            }
            return result;
        }
        public void WaitForThread() {
            thread.Join(5000);
        }

        public void ClearLists() {
            serverList.Clear();
            projectList.Clear();
            projectDict.Clear();
            queueList.Clear();
            prevDiffRequest = DateTime.MinValue;
            additionalInformationUpdateDict.Clear();

            if(OnChanged != null) {
                OnChanged(this, true, true, true, false);
            }
        }
        bool FillServerList() {
            string serversInfo = string.Empty;
            try {
                serversInfo = SmartCruiseManager.GetServerList();
            } catch(Exception ex) {
                Log.Error("exception", ex);
                IsAlive = false;
                return true;
            }
            try {
                return FillServerList(SmartCCNetHelper.FromBase64FlateAndToStream(serversInfo));
            } catch(Exception ex) {
                IsAlive = true;
                Log.Error("exception", ex);
                return true;
            }
        }
        bool FillServerList(StreamReader serversInfoXml) {
            //if(!activeMode) {
            //    isAlive = true;
            //    return false;
            //}
            if(serversInfoXml == null) {
                throw new ArgumentNullException();
            }
            IsAlive = true;

            try {
                XmlTextReader doc = new XmlTextReader(serversInfoXml);
                List<ServerInfo> newServerList = new List<ServerInfo>();
                Dictionary<string, ServerInfo> newServerDict = new Dictionary<string, ServerInfo>();

                doc.MoveToContent();
                while(doc.Read()) {
                    if(doc.NodeType == XmlNodeType.Element && doc.Name == "server") {
                        ServerInfo info = new ServerInfo();
                        while(doc.Read()) {
                            if(doc.Name == "server")
                                break;
                            if(doc.NodeType != XmlNodeType.Element || doc.IsEmptyElement)
                                continue;
                            string name = doc.Name;
                            switch(name) {
                                case "name":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.Name = doc.Value;
                                    break;
                                case "host":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.Host = doc.Value;
                                    break;
                                case "vmid":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.VMID = doc.Value;
                                    break;
                                case "status":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.Status = doc.Value;
                                    break;
                                case "projectName":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.ProjectName = doc.Value;
                                    break;
                                case "isRunning":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.IsRunning = doc.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase) ? true : false;
                                    break;
                                case "guid":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.Guid = doc.Value;
                                    break;
                                case "creator":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.Creator = doc.Value;
                                    break;
                                case "remainedTime":
                                    doc.Read();
                                    if(doc.NodeType == XmlNodeType.Text)
                                        info.RemainedTime = int.Parse(doc.Value);
                                    break;

                                case "types":
                                    while(doc.Read()) {
                                        if(doc.Name == "types")
                                            break;
                                        if(doc.NodeType != XmlNodeType.Element)
                                            continue;
                                        if(doc.Name == "type") {
                                            doc.Read();
                                            info.Types.Add(doc.Value);
                                        }
                                    }
                                    break;

                            }
                        }
                        newServerList.Add(info);
                        newServerDict.Add(info.Name, info);
                    }
                }

                bool hasChanges = false;
                if(newServerList.Count == serverList.Count) {
                    foreach(ServerInfo info in serverList) {
                        ServerInfo newInfo;
                        if(!newServerDict.TryGetValue(info.Name, out newInfo) || !info.Equals(newInfo)) {
                            hasChanges = true;
                            break;
                        }
                    }
                } else
                    hasChanges = true;

                if(hasChanges) {
                    lock(ServerList) {
                        serverList = newServerList;
                    }
                }

                return hasChanges;
            } catch(Exception) {
                return true;//throw;                
            }
        }

        DateTime prevDiffRequest = DateTime.MinValue;
        void FillListsSmart(out bool projectsHasChanges, out bool queueHasChanges, out bool notificationsChanged, out bool serversChanged) {
            string diffs;
            bool resultSuccess = false;
            DateTime resultDiffRequest = prevDiffRequest;
            projectsHasChanges = false;
            queueHasChanges = false;
            notificationsChanged = false;
            BuildNotification[] notifications;
            SmartJournalItem[] diffList;
            serversChanged = FillServerList();
            try {
                lock(this) {
                    try {
                        notifications = (BuildNotification[])SmartCCNetHelper.GetSerializedObject(SmartCruiseManager.GetBuildNotifications(DXCCTrayConfiguration.WorkUserName));
                        if(notifications != null && notifications.Length > 0) {
                            FillNotifications(notifications);
                            notificationsChanged = true;
                        }
                    } catch(Exception ex) {
                        Log.Error("exception", ex);
                    }
                    try {
                        CCServerAdditionalInfo additionalInformation = (CCServerAdditionalInfo)SmartCCNetHelper.GetSerializedObject(SmartCruiseManager.GetAdditionalInformation());
                        if(additionalInformation != null) {
                            FillAdditionalInfo(additionalInformation);
                        }
                    } catch(Exception ex) {
                        Log.Error("exception", ex);
                    }
                    try {
                        diffs = SmartCruiseManager.GetDiffsRequest(SmartJournal.CreateRequest(prevDiffRequest));
                    } catch(Exception ex) {
                        Log.Error("exception", ex);
                        IsAlive = false;
                        return;
                    }
                    IsAlive = true;
                    diffList = (SmartJournalItem[])SmartCCNetHelper.GetSerializedObject(diffs);
                    if(diffList.Length == 0)
                        return;
                    if(diffList[0].Action == SmartJournalAction.Sync) {
                        resultDiffRequest = (DateTime)diffList[0].Info;
                    }
                }
                if(diffList.Length > 1) {
                    FillListsSmartProjectsAndQueues(out projectsHasChanges, out queueHasChanges, diffList);
                }
                resultSuccess = true;
            } finally {
                if(resultSuccess) {
                    prevDiffRequest = resultDiffRequest;
                } else {
                    prevDiffRequest = DateTime.MinValue;
                }
            }
        }
        void FillListsSmartProjectsAndQueues(out bool projectsHasChanges, out bool queueHasChanges, SmartJournalItem[] diffList) {
            projectsHasChanges = false;
            queueHasChanges = false;
            if(diffList[1].Action == SmartJournalAction.Reset) {
                List<ProjectStatus> pList = new List<ProjectStatus>();
                List<QueueSnapshot> qList = new List<QueueSnapshot>();
                for(int i = 2; i < diffList.Length; i++) {
                    if(diffList[i].Type == SmartJournalItemType.Project) {
                        pList.Add((ProjectStatus)diffList[i].Info);
                    } else {
                        qList.Add((QueueSnapshot)diffList[i].Info);
                    }
                }
                projectsHasChanges = UpdateProjectList(pList.ToArray());
                queueHasChanges = FillQueueList(qList);
            } else {
                foreach(SmartJournalItem item in diffList) {
                    if(item.Type == SmartJournalItemType.Project) {
                        if(item.Action == SmartJournalAction.Add || item.Action == SmartJournalAction.Update) {
                            AddOrUpdateProject((ProjectStatus)item.Info);
                        }
                        if(item.Action == SmartJournalAction.Delete) {
                            DeleteProject((string)item.Info);
                        }
                        projectsHasChanges = true;
                    } else if(item.Type == SmartJournalItemType.Queue) {
                        if(item.Action == SmartJournalAction.Add || item.Action == SmartJournalAction.Update) {
                            AddOrUpdateQueue((QueueSnapshot)item.Info);
                        }
                        if(item.Action == SmartJournalAction.Delete) {
                            DeleteQueue((string)item.Info);
                        }
                        queueHasChanges = true;
                    }
                }
            }
        }
        void AddOrUpdateQueue(QueueSnapshot qSnapshot) {
            lock(queueList) {
                string queueName = qSnapshot.QueueName;
                QueueInfo[] newQueues = QueueSnapShotToArray(qSnapshot);
                UpdateQueueList(newQueues, queueName);
            }
        }

        void DeleteQueue(string name) {
            lock(queueList) {
                for(int i = 0; i < queueList.Count; i++) {
                    if(queueList[i].Type == QueueInfoType.Queue) {
                        if(queueList[i].Name == name) {
                            queueList.RemoveAt(i);
                            i--;
                        }
                    } else if(queueList[i].Type == QueueInfoType.Project) {
                        if(queueList[i].Queue == name) {
                            queueList.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }

        ProjectInfo AddOrUpdateProject(ProjectStatus ps) {
            lock(projectList) {
                ProjectInfo foundProjectInfo;
                if(projectDict.TryGetValue(ps.Name, out foundProjectInfo)) {
                    UpdateProjectInfo(foundProjectInfo, ps);
                    if(foundProjectInfo.Server != "Empty") {
                        for(int i = 0; i < projectList.Count; i++) {
                            ProjectInfo info = projectList[i];
                            if(info == foundProjectInfo)
                                continue;
                            if(info.Server == foundProjectInfo.Server) {
                                info.Server = "Empty";
                            }
                        }
                    }
                } else {
                    ProjectInfo nI = new ProjectInfo();
                    nI.Name = ps.Name;
                    foundProjectInfo = nI;
                    UpdateProjectInfo(foundProjectInfo, ps);
                    projectList.Add(nI);
                    projectDict.Add(nI.Name, nI);
                }
                return foundProjectInfo;
            }
        }

        void DeleteProject(string name) {
            lock(projectList) {
                for(int i = 0; i < projectList.Count; i++) {
                    if(projectList[i].Name == name) {
                        projectDict.Remove(name);
                        additionalInformationUpdateDict.Remove(name);
                        projectList.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        public void UpdateProjectInfo(string projectName) {
            if(additionalInformationUpdateDict.ContainsKey(projectName)) {
                additionalInformationUpdateDict.Remove(projectName);
            }
        }

        Dictionary<string, DateTime> additionalInformationUpdateDict = new Dictionary<string, DateTime>();
        Random additionalInformationUpdateRandomizer = new Random(DateTime.Now.GetHashCode());
        void CheckAndUpdateAdditionalInfo(ProjectInfo info) {
            DateTime lastAdditionalInformationUpdate;
            if(!additionalInformationUpdateDict.TryGetValue(info.Name, out lastAdditionalInformationUpdate)
                || DateTime.Now.Subtract(lastAdditionalInformationUpdate).TotalMinutes > (240.0 + additionalInformationUpdateRandomizer.NextDouble() * 3.0)) {
                UpdateProjectAdditionalInfo(info);
            }
        }

        bool inUpdateProjectList = false;
        bool InUpdateProjectList() {
            return inUpdateProjectList;
        }
        void BeginUpdateProjectList() {
            inUpdateProjectList = true;
        }
        void EndUpdateProjectList() {
            inUpdateProjectList = false;
            projectStringCached.Clear();
        }
        bool UpdateProjectList(ProjectStatus[] psList) {
            bool hasChanges = false;
            lock(projectList) {
                if(projectList.Count == 0) {
                    BeginUpdateProjectList();
                    if(!LoadProjectString()) {
                        EndUpdateProjectList();
                    }
                }
                Dictionary<string, bool> toRemove = new Dictionary<string, bool>();
                foreach(ProjectStatus newInfo in psList) {
                    hasChanges = true;
                    ProjectInfo info = AddOrUpdateProject(newInfo);
                    toRemove.Add(info.Name, false);
                }
                for(int i = 0; i < projectList.Count; i++) {
                    ProjectInfo info = projectList[i];
                    if(!toRemove.ContainsKey(info.Name)) {
                        hasChanges = true;
                        projectDict.Remove(info.Name);
                        additionalInformationUpdateDict.Remove(info.Name);
                        projectList.RemoveAt(i);
                        i--;
                    }
                }
                EndUpdateProjectList();
            }
            return hasChanges;
        }

        void UpdateProjectInfo(ProjectInfo info, ProjectStatus status) {
            UpdateString(ref info.BuildStage, status.BuildStage);
            info.Status = status.Status;
            info.LastBuildDate = status.LastBuildDate;
            UpdateString(ref info.LastBuildLabel, status.LastBuildLabel);
            info.NextBuildTime = status.NextBuildTime;
            info.CurrentMessage = status.CurrentMessage;
            UpdateString(ref info.WebURL, status.WebURL);
            string newServer = "Empty";

            info.SetBuildStatus(status.Activity, status.BuildStatus);
            info.TryLoadLastBuildDurationFromDict();

            lock(ServerList) {
                foreach(ServerInfo serverInfo in serverList) {
                    if(serverInfo.ProjectName == info.Name) {
                        newServer = serverInfo.Name;
                        break;
                    }
                }
            }
            UpdateString(ref info.Server, newServer);
            CheckAndUpdateAdditionalInfo(info);
        }
        void UpdateString(ref string oldValue, string value) {
            if(oldValue != value)
                oldValue = value;
        }
        Dictionary<string, string> projectStringCached = new Dictionary<string, string>();
        bool LoadProjectString() {
            if(!InUpdateProjectList()) {
                throw new InvalidOperationException("For update project list only!");
            }
            string projectsStringCompressed = string.Empty;
            try {
                projectsStringCompressed = SmartCruiseManager.GetProjectsConfigurations();
            } catch(Exception exc) {
                Log.Error("exception", exc);
                return false;
            }
            string projectsString = (string)SmartCCNetHelper.GetSerializedObject(projectsStringCompressed);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(projectsString);
            XmlNodeList projectsNodes = doc.GetElementsByTagName("project");
            foreach(XmlNode projectNode in projectsNodes) {
                string projectName = string.Empty;
                foreach(XmlNode child in projectNode.ChildNodes) {
                    if(child.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase)) {
                        projectName = child.InnerText;
                        break;
                    }
                }
                projectStringCached[projectName] = projectNode.OuterXml;
            }
            return true;
        }
        void UpdateProjectAdditionalInfo(ProjectInfo info) {
            try {
                string projectString;
                if(InUpdateProjectList() && projectStringCached.ContainsKey(info.Name)) {
                    projectString = projectStringCached[info.Name];
                } else {
                    projectString = SmartCruiseManager.GetProject(info.Name);
                }
                UpdateProjectAdditionalInfo(info, projectString);
                additionalInformationUpdateDict[info.Name] = DateTime.Now;
            } catch(Exception exc) {
                Log.Error("exception", exc);
                info.QueueName = "none";
            }
        }
        private void UpdateProjectAdditionalInfo(ProjectInfo info, string projectString) {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(projectString);

            XmlNodeList nodeList = doc.GetElementsByTagName("triggers");
            if(nodeList.Count == 1) {
                foreach(XmlNode trigerNode in nodeList[0].ChildNodes) {
                    if(trigerNode.Name == "intervalTrigger") {
                        foreach(XmlNode condNode in trigerNode.ChildNodes) {
                            if(condNode.Name == "buildCondition") {
                                info.IsBuildIfModification = condNode.InnerText == "IfModificationExists";
                            }
                        }
                    }
                }
            }

            foreach(XmlNode xmlNode in doc.GetElementsByTagName("queue")) {
                if(xmlNode.ParentNode.Name == "project") {
                    UpdateString(ref info.QueueName, xmlNode.InnerXml);
                }
            }
            XmlNodeList subProjectsNode = doc.GetElementsByTagName("subProjects");
            info.ProjectsChilds.Clear();
            if(subProjectsNode.Count > 0) {
                foreach(XmlNode spNode in subProjectsNode[0].ChildNodes) {
                    info.ProjectsChilds.Add(spNode.InnerText);
                }
            }
            info.Types.Clear();
            info.Tags.Clear();
            StringBuilder tags = new StringBuilder(), types = new StringBuilder();
            foreach(XmlNode xmlNode in doc.GetElementsByTagName("string")) {
                switch(xmlNode.ParentNode.Name) {
                    case "projectTypes":
                        string type = xmlNode.InnerXml;
                        if(types.Length > 0)
                            types.Append(", ");
                        types.Append(type);
                        info.Types.Add(type);
                        break;
                    case "projectTags":
                        string tag = xmlNode.InnerXml;
                        if(tags.Length > 0)
                            tags.Append(", ");
                        tags.Append(tag);
                        info.Tags.Add(tag);
                        break;
                }
            }
            info.TypesString = types.ToString();
            info.TagsString = tags.ToString();
        }
        bool FillQueueList(IEnumerable queues) {
            if(queues == null)
                throw new NullReferenceException();
            List<QueueInfo> newQueueList = new List<QueueInfo>();

            foreach(QueueSnapshot queue in queues) {
                newQueueList.AddRange(QueueSnapShotToArray(queue));
            }
            return UpdateQueueList(newQueueList.ToArray(), null);
        }

        bool UpdateQueueList(QueueInfo[] newQueueList, string queueName) {
            bool hasChanges = false;
            for(int i = 0; i < queueList.Count; i++) {
                QueueInfo info = queueList[i];
                bool oldExists = false;
                foreach(QueueInfo newInfo in newQueueList) {
                    if(info == newInfo) {
                        hasChanges = true;
                        oldExists = true;
                        newInfo.Exists = true;
                    }
                }
                if(!oldExists) {
                    if(queueName != null) {
                        if(info.Type == QueueInfoType.Queue) {
                            if(info.Name != queueName)
                                continue;
                        } else if(info.Type == QueueInfoType.Project) {
                            if(info.Queue != queueName)
                                continue;
                        }
                    }
                    hasChanges = true;
                    queueList.RemoveAt(i);
                    i--;
                }
            }

            foreach(QueueInfo info in newQueueList) {
                if(!info.Exists) {
                    hasChanges = true;
                    if(!queueList.Contains(info))
                        queueList.Add(info);
                }
            }
            return hasChanges;
        }

        static QueueInfo[] QueueSnapShotToArray(QueueSnapshot queue) {
            List<QueueInfo> newQueueList = new List<QueueInfo>();
            QueueInfo info = new QueueInfo();
            info.Name = queue.QueueName;
            info.Type = QueueInfoType.Queue;
            newQueueList.Add(info);

            foreach(QueuedRequestSnapshot request in queue.Requests) {
                info = new QueueInfo();
                info.Name = request.ProjectName;
                info.Type = QueueInfoType.Project;
                info.Queue = queue.QueueName;
                newQueueList.Add(info);
            }
            return newQueueList.ToArray();
        }
        public string[] ForceBuild(string[] projectNames) {
            List<string> stoppedList = new List<string>();
            try {
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            if(info.Status != ProjectIntegratorState.Running) {
                                stoppedList.Add(projectName);
                                break;
                            }
                            SmartCruiseManager.ForceBuild(projectName, DXCCTrayConfiguration.WorkUserName);
                            break;
                        }
                    }
                }
            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to force build because of the following error:\n{0}", ex.Message));
            }
            return stoppedList.ToArray();
        }
        public void UpdateServerTime(string server, int newRemainsMinutes) {
            try {
                SmartCruiseManager.UpdateServerTime(server, TimeSpan.FromMinutes(newRemainsMinutes));
            } catch(Exception exc) {
                Log.Error("exception", exc);
            }
        }
        public bool ForceBuildOn(string projectName, string serverName) {
            try {
                foreach(ProjectInfo info in projectList) {
                    if(info.Name == projectName) {
                        if(info.Status != ProjectIntegratorState.Running)
                            return false;
                        SmartCruiseManager.ForceBuild(projectName,
                            string.Format("{0}{1}#{2}", SmartServerList.forceOnServerMark, DXCCTrayConfiguration.WorkUserName, serverName));
                    }
                }
            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to force build because of the following error:\n{0}", ex.Message));
            }
            return true;
        }
        public string GetServerAsync(VmInfo vmInfo, string psScript) {
            try {
                Guid guid = Guid.NewGuid();
                SmartCruiseManager.GetServer(vmInfo, guid, DXCCTrayConfiguration.WorkUserName, psScript);
                return guid.ToString();
            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to GetServerAsync because of the following error:\n{0}", ex.Message));
                return string.Empty;
            }
        }
        public bool SendNotification(string project, string recepient, string sender) {
            try {
                SmartCruiseManager.SendNotification(project, recepient, sender);
                return true;
            } catch {
                //DXMessageBox.Show("Send notification not supported");
                return false;
            }
        }
        public string[] GetBreakers(string projectName) {
            try {
                return SmartCruiseManager.GetProjectBreakers(projectName);
            } catch {
                return new string[0];
            }
        }
        public string[] GetImageNames() {
            return SmartCruiseManager.GetImageNames();
        }

        public void AbortBuild(string[] projectNames) {
            try {
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            SmartCruiseManager.AbortBuild(projectName, DXCCTrayConfiguration.WorkUserName);
                            break;
                        }
                    }
                }

            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to abort build because of the following error:\n{0}", ex.Message));
            }
        }

        public void StartProject(string[] projectNames) {
            try {
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            SmartCruiseManager.StartProject(projectName);
                            break;
                        }
                    }
                }

            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to start project because of the following error:\n{0}", ex.Message));
            }
        }

        public void StopProject(string[] projectNames) {
            try {
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            SmartCruiseManager.StopProject(projectName);
                            break;
                        }
                    }
                }

            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to stop project because of the following error:\n{0}", ex.Message));
            }
        }
        public void CancelPending(string[] projectNames) {
            try {
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            SmartCruiseManager.CancelPendingRequest(projectName);
                            break;
                        }
                    }
                }

            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to cancel pending project because of the following error:\n{0}", ex.Message));
            }
        }
        public void FixBuild(string[] projectNames) {
            try {
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            SmartCruiseManager.SendMessage(projectName,
                                new ThoughtWorks.CruiseControl.Remote.Message(string.Format("{0}{1}", DXCCTrayConfiguration.WorkUserName, SmartConstants.FixingBuildMessage)));
                            break;
                        }
                    }
                }

            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to fix build because of the following error:\n{0}", ex.Message));
            }
        }

        public void FinishFixBuild(string[] projectNames) {
            try {
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            if(!info.Details.Contains(DXCCTrayConfiguration.WorkUserName))
                                throw new InvalidOperationException(string.Format("You are not fixing the build '{0}'.", projectName));
                        }
                    }
                }
                foreach(string projectName in projectNames) {
                    foreach(ProjectInfo info in projectList) {
                        if(info.Name == projectName) {
                            SmartCruiseManager.SendMessage(projectName,
                                new ThoughtWorks.CruiseControl.Remote.Message(string.Format("{0}{1}", DXCCTrayConfiguration.WorkUserName, SmartConstants.FinishFixingBuildMessage)));
                            break;
                        }
                    }
                }

            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to fix build because of the following error:\n{0}", ex.Message));
            }
        }

        public void StartServer(string serverName) {
            try {
                SmartCruiseManager.StartServer(serverName, DXCCTrayConfiguration.WorkUserName);
            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to start server because of the following error:\n{0}", ex.Message));
            }
        }
        public void StopServer(string serverName) {
            try {
                SmartCruiseManager.StopServer(serverName, DXCCTrayConfiguration.WorkUserName);
            } catch(Exception ex) {
                DXCCTrayConfiguration.ShowError(string.Format("Unable to stop server because of the following error:\n{0}", ex.Message));
            }
        }

        public override bool Equals(object obj) {
            DXCCTrayIntegrator integrator = obj as DXCCTrayIntegrator;
            if(integrator == null)
                return false;
            return name == integrator.name && Url == integrator.Url;
        }

        public override int GetHashCode() {
            string result = name + Url;
            return result.GetHashCode();
        }
        const string powerShellScriptsDXVCSPath = "$/CCNetConfig/DXCCTray/PowerShellScripts.xml";
        //public PSScript[] GetDefaultPowerShellScripts() {
        //    List<PSScript> result = new List<PSScript>();
        //    try {
        //        string dxvcsServicePath = SmartCruiseManager.GetDXVCSServicePath();
        //        DXVCSDriver vcs = new DXVCSDriver();
        //        vcs.Open(dxvcsServicePath, null, null);

        //        DXBuild.Core.VSSLocation[] loc = new DXBuild.Core.VSSLocation[1];
        //        loc[0] = new DXBuild.Core.VSSLocation(powerShellScriptsDXVCSPath, string.Empty, true);
        //        byte[][] data = vcs.GetFilesFromSS(loc, string.Empty);
        //        if(data.Length != 1 || data[0] == null || data[0].Length == 0)
        //            return null;
        //        string fileText = Encoding.UTF8.GetString(data[0]);
        //        XmlDocument doc = new XmlDocument();
        //        doc.LoadXml(fileText);
        //        foreach(XmlNode psNode in doc.GetElementsByTagName("PSScript")) {
        //            string name = psNode.Attributes["Name"].Value;
        //            string script = SmartCCNetHelper.UnescapeXml(psNode.InnerText);
        //            PSScript psScript = new PSScript(name, script, true);
        //            result.Add(psScript);
        //        }
        //    } catch(Exception exc) {
        //        Log.Error("exception", exc);
        //    }
        //    return result.ToArray();
        //}
    }
    public class FarmProjectList {
        string name;
        List<string> projects = new List<string>();

        public List<string> Projects {
            get { return projects; }
        }

        public string Name {
            get { return name; }
        }

        public FarmProjectList(string name) {
            this.name = name;
        }
    }
}
