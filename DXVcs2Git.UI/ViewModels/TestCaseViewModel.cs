using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class TestCaseViewModel : BindableBase {
        public string FullName { get; }
        public string Project { get; }
        public double Time { get; }
        public bool Passed { get; }
        public bool? Executed { get; }
        public int Failed => Passed ? 0 : 1;

        public string Name { get; }
        public string ClassName { get; }
        public string NamespaceName { get; }
        public TestCaseViewModel(string fullName, double time, bool? executed, bool passed, string assembly) {
            this.FullName = fullName;
            this.Time = time;
            this.Project = assembly;
            this.Passed = passed;
            this.Executed = executed;

            string[] parts = this.FullName.Split('.');
            this.Name = parts[parts.Length - 1];
            if (parts.Length > 1) {
                this.ClassName = parts[parts.Length - 2];
            }
            else {
                this.ClassName = string.Empty;
            }
            if (parts.Length > 2) {
                this.NamespaceName = this.FullName.Remove(this.FullName.Length - this.Name.Length - this.ClassName.Length - 2);
            }
            else {
                this.NamespaceName = string.Empty;
            }
        }
        public TestCaseViewModel(string fullName, string name, double time, bool? executed, bool passed, string assembly)
            : this(fullName, time, executed, passed, assembly) {
            this.Name = name;
        }
    }
}
