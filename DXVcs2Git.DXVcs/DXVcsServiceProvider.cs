using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using DXVCS;

namespace DXVcsTools.DXVcsClient {
    [ServiceContract]
    class DXVcsServiceProvider : MarshalByRefObject {
        class Factory : ChannelFactory<IDXVCSService> {
            public Factory(ServiceEndpoint endpoint) : base(endpoint) { }
            protected override void ApplyConfiguration(string configurationName) {
            }
        }
        public IDXVCSService GetService(string serviceUrl) {
            EndpointAddress myEndpointAddress = new EndpointAddress(new Uri(serviceUrl), new SpnEndpointIdentity(String.Empty));
            ServiceEndpoint point = GZipMessageEncodingBindingElement.CreateEndpoint(myEndpointAddress);
            ChannelFactory<IDXVCSService> factory = new Factory(point);
            factory.Credentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Identification;
            IDXVCSService newService = factory.CreateChannel();
            IContextChannel newChannel = (IContextChannel)newService;
            newChannel.OperationTimeout = TimeSpan.FromSeconds(10);
            newChannel.Open();
            return new ServiceWrapper(newService);
        }

        public override object InitializeLifetimeService() {
            return null; // lease never expires
        }
    }
}