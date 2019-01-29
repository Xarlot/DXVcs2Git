using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Security;
using System.Runtime.InteropServices;
using System.Reflection;
using DevExpress.CCNetSmart.Lib;
using System.Text.RegularExpressions;

using DXVcs2Git.Core;
using DXVcs2Git.UI.Farm;

namespace DevExpress.DXCCTray {
    class DXCCTrayConfiguration {
        static public void ShowError(string text) {
//            DXMessageBox.Show(text);
        }
        const bool defaultAlwaysOnTop = false;
        const bool defaultMinimized = false;
        const bool defaultBuildLogMaximized = false;
        const bool defaultUseSkin = true;
        const bool defaultAlreadyReloadGridTestsLayout = false;

        const int defaultRefreshTime = 5;
        const int defaultFormWidth = 1000;
        const int defaultFormHeight = 700;
        const int defaultFormTop = 0;
        const int defaultFormLeft = 0;
        const string defaultSkinName = "Lilian";
        const int defaultPopupHideTimeout = 0;
        
        static string defaultUserName;

        public static string GetLocalStoragePath() {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\DXCCTray";
        }

        static string updateUrl = string.Empty;
        public static string UpdateUrl {
            get { return updateUrl; }
            set { if(CanSetConfig)updateUrl = value; }
        }
        static string workUserName = string.Empty;
        public static string WorkUserName {
            get { return workUserName; }
            set { workUserName = value; }
        }
        static string layoutXml = string.Empty;
        public static string LayoutXml {
            get { return layoutXml; }
            set { if(CanSetConfig)layoutXml = value; }
        }

        static string gridProjectXml = string.Empty;
        public static string GridProjectsXml {
            get { return gridProjectXml; }
            set { if(CanSetConfig)gridProjectXml = value; }
        }
        static string gridTestsXml = string.Empty;
        public static string GridTestsXml {
            get { return gridTestsXml; }
            set { gridTestsXml = value; }
        }
        static string gridServersXml = string.Empty;
        public static string GridServersXml {
            get { return gridServersXml; }
            set { if(CanSetConfig)gridServersXml = value; }
        }
        static string gridNotificationsXml = string.Empty;
        public static string GridNotificationsXml {
            get { return gridNotificationsXml; }
            set { if(CanSetConfig)gridNotificationsXml = value; }
        }
        static bool useSkin = defaultUseSkin;
        public static bool UseSkin {
            get { return useSkin; }
            set { if(CanSetConfig)useSkin = value; }
        }
        static bool needAskForStutup = true;
        public static bool NeedAskForStutup {
            get { return needAskForStutup; }
            set { if(CanSetConfig)needAskForStutup = value; }
        }
        static bool alreadyReloadGridTestsLayout = false;
        public static bool AlreadyReloadGridTestsLayout {
            get { return alreadyReloadGridTestsLayout; }
            set { if(CanSetConfig)alreadyReloadGridTestsLayout = value; }
        }
        static string skinName = defaultSkinName;
        public static string SkinName {
            get { return skinName; }
            set { if(CanSetConfig)skinName = value; }
        }

        static bool canSetConfig = true;
        public static bool CanSetConfig {
            get { return canSetConfig; }
            set { canSetConfig = value; }
        }

        static int popupHideTimeout = 0;
        public static int PopupHideTimeout {
            get { return DXCCTrayConfiguration.popupHideTimeout; }
            set { DXCCTrayConfiguration.popupHideTimeout = value; }
        }

        static List<string> farmList = new List<string>();
        public static List<string> FarmList {
            get { return farmList; }
        }
        static List<BuildNotificationViewInfo> buildNotifications = new List<BuildNotificationViewInfo>();
        public static List<BuildNotificationViewInfo> BuildNotifications {
            get {
                return buildNotifications;
            }
        }
        static List<PSScript> powerShellScripts = new List<PSScript>();
        public static List<PSScript> PowerShellScripts {
            get {
                return powerShellScripts;
            }
        }
        public static bool UsePowerShellScript { get; internal set; }
        static int formWidth = defaultFormWidth;
        public static int FormWidth {
            get { return formWidth; }
            set { if(CanSetConfig)formWidth = value; }
        }

        static int formHeight = defaultFormHeight;
        public static int FormHeight {
            get { return formHeight; }
            set { if(CanSetConfig)formHeight = value; }
        }

        static int formLeft = defaultFormLeft;
        public static int FormLeft {
            get { return formLeft; }
            set { if(CanSetConfig)formLeft = value; }
        }

        static int formTop = defaultFormTop;
        public static int FormTop {
            get { return formTop; }
            set { if(CanSetConfig)formTop = value; }
        }

        static bool minimized = defaultMinimized;
        public static bool Minimized {
            get { return minimized; }
            set { if(CanSetConfig)minimized = value; }
        }
        static bool buildLogmaximized = defaultBuildLogMaximized;
        public static bool BuildLogMaximized {
            get { return buildLogmaximized; }
            set { if(CanSetConfig)buildLogmaximized = value; }
        }
        static bool alwaysOnTop = defaultAlwaysOnTop;
        public static bool AlwaysOnTop {
            get { return alwaysOnTop; }
            set { if(CanSetConfig)alwaysOnTop = value; }
        }

        static int refreshTime = defaultRefreshTime;
        public static int RefreshTime {
            get { return refreshTime; }
            set { if(CanSetConfig)refreshTime = value; }
        }

        static List<ProjectShortInfo> trackedProjects = new List<ProjectShortInfo>();
        public static List<ProjectShortInfo> TrackedProjects {
            get { return trackedProjects; }
        }

        //static Color needVolunteerColorLight;
        //static Color needVolunteerColorDark;
        //static Color needNextVolunteerColorLight;
        //static Color needNextVolunteerColorDark;
        //static Color fixingColorLight;
        //static Color fixingColorDark;


        //public static Color NeedVolunteerColorLight {
        //    get {
        //        return needVolunteerColorLight;
        //    }
        //}

        //public static Color NeedVolunteerColorDark {
        //    get {
        //        return needVolunteerColorDark;
        //    }
        //}
        //public static Color NeedNextVolunteerColorLight {
        //    get {
        //        return needNextVolunteerColorLight;
        //    }
        //}
        //public static Color NeedNextVolunteerColorDark {
        //    get {
        //        return needNextVolunteerColorDark;
        //    }
        //}
        //public static Color FixingColorLight {
        //    get {
        //        return fixingColorLight;
        //    }
        //}
        //public static Color FixingColorDark {
        //    get {
        //        return fixingColorDark;
        //    }
        //}

        static string fileName = "dxcctray.config";

        //public readonly static Color DefaultNeedVolunteerColorLight;
        //public readonly static Color DefaultNeedVolunteerColorDark;
        //public readonly static Color DefaultNeedNextVolunteerColorLight;
        //public readonly static Color DefaultNeedNextVolunteerColorDark;
        //public readonly static Color DefaultFixingColorLight;
        //public readonly static Color DefaultFixingColorDark;

        //public static string VolunteerColorString {
        //    get {
        //        return GetVolunteerColorString(needVolunteerColorLight, needVolunteerColorDark,
        //            needNextVolunteerColorLight, needNextVolunteerColorDark,
        //            fixingColorLight, fixingColorDark);
        //    }
        //    set {
        //        GetVolunteerColorsFromString(value, ref needVolunteerColorLight, ref needVolunteerColorDark,
        //            ref needNextVolunteerColorLight, ref needNextVolunteerColorDark,
        //            ref fixingColorLight, ref fixingColorDark);
        //    }
        //}

        //public static string GetVolunteerColorString(Color needVolunteerColorLight,
        //    Color needVolunteerColorDark,
        //    Color needNextVolunteerColorLight,
        //    Color needNextVolunteerColorDark,
        //    Color fixingColorLight,
        //    Color fixingColorDark) {
        //    return string.Join("#", new string[] {
        //               		                    ((uint)needVolunteerColorLight.ToArgb()).ToString(),
        //               		                    ((uint)needVolunteerColorDark.ToArgb()).ToString(),
        //               		                    ((uint)needNextVolunteerColorLight.ToArgb()).ToString(),
        //               		                    ((uint)needNextVolunteerColorDark.ToArgb()).ToString(),
        //               		                    ((uint)fixingColorLight.ToArgb()).ToString(),
        //               		                    ((uint)fixingColorDark.ToArgb()).ToString()
        //               		                });
        //}
        //public static void GetVolunteerColorsFromString(string value, ref Color needVolunteerColorLight,
        //    ref Color needVolunteerColorDark,
        //    ref Color needNextVolunteerColorLight,
        //    ref Color needNextVolunteerColorDark,
        //    ref Color fixingColorLight,
        //    ref Color fixingColorDark) {
        //    string[] colors = value.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
        //    if(colors.Length != 6) throw new ArgumentException();
        //    needVolunteerColorLight = Color.FromArgb((int)uint.Parse(colors[0]));
        //    needVolunteerColorDark = Color.FromArgb((int)uint.Parse(colors[1]));
        //    needNextVolunteerColorLight = Color.FromArgb((int)uint.Parse(colors[2]));
        //    needNextVolunteerColorDark = Color.FromArgb((int)uint.Parse(colors[3]));
        //    fixingColorLight = Color.FromArgb((int)uint.Parse(colors[4]));
        //    fixingColorDark = Color.FromArgb((int)uint.Parse(colors[5]));
        //}

        static DXCCTrayConfiguration() {
            defaultUserName = Environment.UserName;
            //DefaultFixingColorLight = Color.FromArgb(160, 255, 160);
            //DefaultFixingColorDark = Color.FromArgb(0, 160, 0);
            //DefaultNeedNextVolunteerColorLight = Color.FromArgb(255, 255, 160);
            //DefaultNeedNextVolunteerColorDark = Color.FromArgb(0xB2, 0x69, 0x00);
            //DefaultNeedVolunteerColorLight = Color.FromArgb(0xFF, 0xA3, 0xA3);
            //DefaultNeedVolunteerColorDark = Color.FromArgb(160, 0, 0);
            LoadDefaultVolunteerColors();
        }
        static void LoadDefaultVolunteerColors() {
            //fixingColorLight = DefaultFixingColorLight;
            //fixingColorDark = DefaultFixingColorDark;
            //needNextVolunteerColorLight = DefaultNeedNextVolunteerColorLight;
            //needNextVolunteerColorDark = DefaultNeedNextVolunteerColorDark;
            //needVolunteerColorLight = DefaultNeedVolunteerColorLight;
            //needVolunteerColorDark = DefaultNeedVolunteerColorDark;
        }
        public static bool LocalConfig = false;
        static string defaultConfigPath = "DXVcs2Git.Core.dxcctray.config";
        public static void LoadConfiguration() {
            string fullFileName = Path.Combine(Assembly.GetExecutingAssembly().Location, fileName);
            if(!LocalConfig){
                fullFileName = Path.Combine(GetLocalStoragePath(), fileName);
            }
            if(File.Exists(fullFileName)) {
                try {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(fullFileName);
                    LoadFarmList(doc);
                    buildNotifications.Clear();
                    foreach(XmlNode node in doc.GetElementsByTagName("buildNotifications")) {
                        if(node != null) {
                            foreach(XmlNode bnNode in node.ChildNodes) {
                                BuildNotification bn = (BuildNotification)SmartCCNetHelper.GetSerializedObject(bnNode.InnerText);
                                BuildNotificationViewInfo bnvi = new BuildNotificationViewInfo(bn);
                                bnvi.Read = true;
                                buildNotifications.Add(bnvi);
                            }
                        }
                    }
                    trackedProjects.Clear();
                    foreach(XmlNode node in doc.GetElementsByTagName("trackedProject")) {
                        if(node.ParentNode != null && node.ParentNode.Name == "trackedProjects") {
                            ProjectShortInfo info = new ProjectShortInfo();
                            foreach(XmlAttribute attr in node.Attributes) {
                                switch(attr.Name){
                                    case "name":
                                        info.Name = attr.Value;
                                        break;
                                    case "status":
                                        info.BuildStatus = attr.Value;
                                        break;
                                }                                    
                            }
                            trackedProjects.Add(info);
                        }
                    }
                    powerShellScripts.Clear();
                    foreach(XmlNode node in doc.GetElementsByTagName("PowerShellScript")) {
                        if(node != null) {
                            foreach(XmlNode scriptNode in node.ChildNodes) {
                                PSScript script = (PSScript)SmartCCNetHelper.GetSerializedObject(scriptNode.InnerText);
                                powerShellScripts.Add(script);
                            }
                        }
                    }
                    UsePowerShellScript = LoadBoolean(doc, "UsePowerShellScript", false);
                    LoadLayoutAndGrid(doc);
                    gridNotificationsXml = LoadString(doc, "gridNotificationsXml", string.Empty);
                    alwaysOnTop = LoadBoolean(doc, "alwaysOnTop", defaultAlwaysOnTop);
                    minimized = LoadBoolean(doc, "minimized", defaultMinimized);
                    formWidth = LoadInt(doc, "formWidth", defaultFormWidth);
                    formHeight = LoadInt(doc, "formHeight", defaultFormHeight);
                    formLeft = LoadInt(doc, "formLeft", defaultFormLeft);
                    formTop = LoadInt(doc, "formTop", defaultFormTop);
                    refreshTime = LoadInt(doc, "refreshTime", defaultRefreshTime);
                    LoadUpdateUrl(doc);
                    workUserName = LoadString(doc, "workUserName", string.Empty);
                    if(string.IsNullOrEmpty(workUserName)) {
                        workUserName = defaultUserName;
                    }
                    useSkin = LoadBoolean(doc, "useSkin", defaultUseSkin);
                    needAskForStutup = LoadBoolean(doc, "needAskForStutup", true);
                    alreadyReloadGridTestsLayout = LoadBoolean(doc, "alreadyReloadGridTestsLayout", defaultAlreadyReloadGridTestsLayout);
                    skinName = LoadString(doc, "skinName", defaultSkinName);
                    popupHideTimeout = LoadInt(doc, "popupHideTimeout", defaultPopupHideTimeout);
                    
                    //if(TryLoadString(doc, "volunteerColorString", out tempString)) {
                    //    VolunteerColorString = tempString;
                    //} else {
                    //    LoadDefaultVolunteerColors();
                    //}
                    byte[] lastProjectDurationDictionaryData = LoadBase64(doc, "lastProjectDurationDictionary", null);
                    ProjectInfo.LoadLastBuildDurationDict(lastProjectDurationDictionaryData);
                } catch(Exception exc) {
                    Log.Error("exception", exc);
                    LoadDefaultConfiguration();
                }
            } else {
                LoadDefaultConfiguration();
            }
            DXCCTrayConfiguration.CanSetConfig = false;
        }
        private static void LoadUpdateUrl(XmlDocument doc) {
            updateUrl = LoadString(doc, "updateUrl", string.Empty);
        }
        static void LoadDefaultFarmListAndUpdateUrl() {
            string defaultConfig = GetResource(defaultConfigPath);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(defaultConfig);
            LoadFarmList(doc);
            LoadUpdateUrl(doc);
        }
        private static void LoadFarmList(XmlDocument doc) {
            farmList.Clear();
            foreach(XmlNode node in doc.GetElementsByTagName("integrator")) {
                if(node.ParentNode != null && node.ParentNode.Name == "integrators") {
                    foreach(XmlAttribute attr in node.Attributes) {
                        switch(attr.Name) {
                            case "url":
                                if(!attr.Value.ToLower().Contains("coderush"))//TODO remove
                                    farmList.Add(attr.Value);
                                break;
                        }
                    }
                }
            }
        }
        static int LoadInt(XmlDocument doc, string name, int defaultValue) {
            foreach(XmlNode node in doc.GetElementsByTagName(name)) {
                if(node.ParentNode != null && node.ParentNode.Name == "config") {
                    try {
                        return Convert.ToInt32(node.InnerXml);
                    } catch(Exception) {
                        return defaultValue;
                    }
                }
            }

            return defaultValue;
        }
        static bool LoadBoolean(XmlDocument doc, string name, bool defaultValue) {
            foreach(XmlNode node in doc.GetElementsByTagName(name)) {
                if(node.ParentNode != null && node.ParentNode.Name == "config") {
                    try {
                        return Convert.ToBoolean(node.InnerXml);
                    } catch(Exception) {
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }
        static string LoadString(XmlDocument doc, string name, string defaultValue) {
            foreach(XmlNode node in doc.GetElementsByTagName(name)) {
                if(node.ParentNode != null && node.ParentNode.Name == "config") {
                    try {
                        return node.InnerXml;
                    } catch(Exception) {
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }
        static bool TryLoadString(XmlDocument doc, string name, out string value) {
            value = null;
            foreach(XmlNode node in doc.GetElementsByTagName(name)) {
                if(node.ParentNode != null && node.ParentNode.Name == "config") {
                    try {
                        value = node.InnerXml;
                        return true;
                    } catch(Exception) {
                        return false;
                    }
                }
            }
            return false;
        }

        static byte[] LoadBase64(XmlDocument doc, string name, byte[] defaultValue) {
            foreach(XmlNode node in doc.GetElementsByTagName(name)) {
                if(node.ParentNode != null && node.ParentNode.Name == "config") {
                    try {
                        return Convert.FromBase64String(node.InnerXml);
                    } catch(Exception) {
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }
        public static void LoadDefaultConfiguration() {
            trackedProjects.Clear();
            alwaysOnTop = defaultAlwaysOnTop;
            refreshTime = defaultRefreshTime;
            minimized = defaultMinimized;
            formWidth = defaultFormWidth;
            formHeight = defaultFormHeight;
            formLeft = defaultFormLeft;
            formTop = defaultFormTop;
            LoadDefaultFarmListAndUpdateUrl();
            LoadDefaultLayoutAndGrid();
            gridTestsXml = string.Empty;
            workUserName = defaultUserName;
            skinName = defaultSkinName;
            useSkin = defaultUseSkin;
            alreadyReloadGridTestsLayout = defaultAlreadyReloadGridTestsLayout;
            LoadDefaultVolunteerColors();
            popupHideTimeout = defaultPopupHideTimeout;
            ProjectInfo.LoadLastBuildDurationDict(null);
        }
        static void LoadLayoutAndGrid(XmlDocument doc) {
            layoutXml = LoadString(doc, "layoutXml", string.Empty);
            gridProjectXml = LoadString(doc, "gridProjectXml", string.Empty);
            gridTestsXml = LoadString(doc, "gridTestsXml", string.Empty);
            gridServersXml = LoadString(doc, "gridServersXml", string.Empty);
        }
        public static void LoadDefaultLayoutAndGrid() {            
            string defaultConfig = GetResource(defaultConfigPath);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(defaultConfig);
            LoadLayoutAndGrid(doc);
        }
        public static void LoadDefaultLayout() {
            string defaultConfig = GetResource(defaultConfigPath);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(defaultConfig);
            layoutXml = LoadString(doc, "layoutXml", string.Empty);
        }
        public static string GetResource(string resourceName) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            TextReader textReader = new StreamReader(assembly.GetManifestResourceStream(resourceName));
            string result = textReader.ReadToEnd();
            textReader.Close();
            return result;
        }
    }
    public class ProjectShortInfo {
        string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }
        string buildStatus;
        public string BuildStatus {
            get { return buildStatus; }
            set { buildStatus = value; }
        }
        public ProjectShortInfo() { }
        public ProjectShortInfo(string name) {
            Name = name;
        }
        public override string ToString() {
            return string.Format("{0} - {1}", Name, BuildStatus.ToString());
        }
        public override bool Equals(object obj) {
            ProjectShortInfo psi = obj as ProjectShortInfo;
            if(psi == null) { return false; }
            return psi.Name == Name;
        }
        public override int GetHashCode() {
            return Name.GetHashCode() + BuildStatus.GetHashCode();
        }
    }
    public static class DXCCTrayHelper {
        static string startupPath = string.Empty;
        static string StartupPath {
            get {
                if(string.IsNullOrEmpty(startupPath)) {
                    startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "dxcctray.lnk");
                }
                return startupPath;
            }
        }
        public static bool IsStartupDXCCTray() {
            if(File.Exists(StartupPath)) {
                return true;
            }
            return false;
        }
        //public static void AddDXCCTrayToStartup(IWin32Window owner) {
        //}
        //public static void RemoveDXCCTrayFromStartup(IWin32Window owner) {

        //}
        public static void ParseBuildUrl(string buildUrl, out string project, out string build) {
            string fake;
            ParseBuildUrl(buildUrl, out fake, out project, out build);
        }
        public static void ParseBuildUrl(string buildUrl, out string server, out string project, out string build) {
            string[] urlParts = buildUrl.Split('/');
            server = string.Empty;
            build = string.Empty;
            project = string.Empty;
            for(int i = 0; i < urlParts.Length; i++) {
                if(string.IsNullOrEmpty(server)) {
                    server = FindTagValue("server", urlParts, i);
                }
                if(string.IsNullOrEmpty(project)) {
                    string tagValue = FindTagValue("project", urlParts, i);
                    tagValue = tagValue.Replace("+", " ");
                    project = Regex.Replace(tagValue, "%..", new MatchEvaluator(delegate (Match target) {
                        return Convert.ToChar(Convert.ToUInt32(target.Value.Remove(0, 1), 16)).ToString();
                    }));
                }
                if(string.IsNullOrEmpty(build)) {
                    build = FindTagValue("build", urlParts, i);
                }
            }
        }
        public static string CreateBuildUrl(string anyBuildUrl, string projectName, string buildName) {
            string[] urlParts = anyBuildUrl.Split('/');
            string foundProjectName = null;
            string foundBuildName = null;
            for(int i = 0; i < urlParts.Length; i++) {
                if(string.IsNullOrEmpty(foundProjectName)) {
                    foundProjectName = FindTagValue("project", urlParts, i);
                    if(!string.IsNullOrEmpty(foundProjectName)) {
                        anyBuildUrl = anyBuildUrl.Replace(String.Format("/{0}/", foundProjectName), String.Format("/{0}/", projectName.Replace(" ", "+").Replace(",", "%2c")));
                    }
                }
                if(string.IsNullOrEmpty(foundBuildName)) {
                    foundBuildName = FindTagValue("build", urlParts, i);
                    if(!string.IsNullOrEmpty(foundBuildName)) {
                        anyBuildUrl = anyBuildUrl.Replace(String.Format("/{0}/", foundBuildName), String.Format("/{0}/", buildName));
                    }
                }
            }
            return anyBuildUrl;
        }

        static string FindTagValue(string prevTagName, string[] urlParts, int index) {
            if(urlParts[index].Equals(prevTagName, StringComparison.InvariantCultureIgnoreCase)) {
                if(urlParts.Length > index + 1) {
                    return urlParts[++index];
                }
            }
            return string.Empty;
        }
    }
    [SuppressUnmanagedCodeSecurity]
    public static class DXWin32Native {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GwConsts wCmd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);


        public enum GwConsts {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST,
            GW_HWNDNEXT,
            GW_HWNDPREV,
            GW_OWNER,
            GW_CHILD
        }
    }

}
