using System;
using System.Windows.Markup;
using Microsoft.Practices.ServiceLocation;

namespace DXVcs2Git.UI.Extensions {
    public class IoCExtension : MarkupExtension {
        public Type TargetType { get; set; }

        public IoCExtension() {
            
        }
        public IoCExtension(Type targetType) {
            TargetType = targetType;
        }
        public override object ProvideValue(IServiceProvider serviceProvider) {
            return ServiceLocator.Current.GetInstance(TargetType);
        }
    }
}
