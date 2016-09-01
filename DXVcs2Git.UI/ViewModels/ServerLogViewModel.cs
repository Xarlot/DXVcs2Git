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
    public class ServerLogViewModel : BindableBase {
        ArtifactsViewModel model;
        public ServerLogViewModel(ArtifactsViewModel model) {
            this.model = model;
            Text = model.HasTrace ? model.Trace : "Text";
        }

        public string Text {
            get { return GetProperty(() => Text); }
            private set { SetProperty(() => Text, value); }
        }
    }
}

