using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.UI.AtomFeed {
    [DataContract(IsReference =false, Name = Vsix.ExtensionName, Namespace =Vsix.ExtensionNamespace)]
    public class Vsix {
        public const string ExtensionNamespace = "http://schemas.microsoft.com/developer/vsx-syndication-schema/2010";
        public const string ExtensionName = "Vsix";

        string versionString;

        [DataMember( IsRequired = true, Name = "Id")]
        public string Id { get; set; }
        [DataMember(IsRequired = true, Name = "Version")]
        public string VersionString {
            get { return versionString; }
            set {
                versionString = value;
                Version = Version.Parse(versionString);
            }
        }
        public Version Version { get; set; }

    }
}
