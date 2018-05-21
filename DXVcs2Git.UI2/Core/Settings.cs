namespace DXVcs2Git.UI2.Core {
    public interface ISettings {
        void Initialize();
    }
    
    public class Settings : ISettings {
        public readonly string ConfigsPath = "Configs";
        public readonly string WorkPath = @"c:\Work";
        public void Initialize() {
            
        }
    }
}