using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm;

namespace DXVcs2Git.UI.ViewModels {
    public class ModificationsViewModel : BindableBase {
        ArtifactsViewModel model;
        public string Text { get; }

        public ModificationsViewModel(ArtifactsViewModel model) {
            this.model = model;
            Text = model.Modifications;
        }
    }
}
