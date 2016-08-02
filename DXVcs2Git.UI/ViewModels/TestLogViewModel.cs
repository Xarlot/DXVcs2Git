using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DevExpress.CCNetSmart.Lib;
using DevExpress.Mvvm;
using DXVcs2Git.Core;

namespace DXVcs2Git.UI.ViewModels {
    public class TestLogViewModel : BindableBase {
        ArtifactsViewModel model;

        public IEnumerable<TestCaseViewModel> Tests {
            get { return GetProperty(() => Tests); }
            private set { SetProperty(() => Tests, value); }
        }
        public TestLogViewModel(ArtifactsViewModel model) {
            this.model = model;

            Tests = GetTests(model.TestLog);
        }
        List<TestCaseViewModel> GetTests(string buildLog) {
            if (string.IsNullOrEmpty(buildLog))
                return new List<TestCaseViewModel>();
            List<TestCaseViewModel> result = new List<TestCaseViewModel>();
            try {
                XmlReader reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(buildLog)));
                while (true) {
                    if (!reader.ReadToFollowing("test-suite")) {
                        break;
                    }
                    string name = reader.GetAttribute("name");
                    if (!string.IsNullOrEmpty(name) && (name.EndsWith(".dll") || name.EndsWith(".xap") || name.EndsWith(".exe"))) {
                        XmlReader subReader = reader.ReadSubtree();
                        result.AddRange(GetTests(subReader));
                        subReader.Close();
                        reader.Skip();
                    }
                    reader.Close();
                }
            }
            catch (Exception ex) {
                Log.Error("Error during parsing build log", ex);
            }
            return result;
        }
        List<TestCaseViewModel> GetTests(XmlReader projectReader) {
            List<TestCaseViewModel> result = new List<TestCaseViewModel>();
            if (!projectReader.Read()) {
                return result;
            }
            string project = projectReader.GetAttribute("name");
            project = GetProjectFromFullAssembly(project);
            while (projectReader.ReadToFollowing("test-case")) {
                string fullName = projectReader.GetAttribute("name");
                bool executed = projectReader.GetAttribute("executed") == "True";
                bool passed = projectReader.GetAttribute("success") != "False";
                double time = 0;
                string timeStr = projectReader.GetAttribute("time");
                if (!string.IsNullOrEmpty(timeStr)) {
                    time = double.Parse(timeStr, System.Globalization.CultureInfo.InvariantCulture);
                }
                result.Add(new TestCaseViewModel(fullName, time, executed, passed, project));
            }
            return result;
        }
        private static string GetProjectFromFullAssembly(string project) {
            string[] parts = project.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) {
                project = parts[parts.Length - 1];
            }
            project = BuildLogAnalyzer.GetProjectName(project);
            return project;
        }

    }
}
