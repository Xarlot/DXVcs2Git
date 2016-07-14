using System;
using System.ServiceModel;
using DXVCS;

namespace DXVcs2Git.DXVcs {
    [ServiceContract]
    class DXVcsServiceProvider : MarshalByRefObject {
        public IDXVCSService GetService(string serviceUrl, string user = "", string password = "") {
            var service = ConnectToServiceWCFHelper.ConnectWCF(serviceUrl, user, password);
            return service.Service;
            //EndpointAddress myEndpointAddress = new EndpointAddress(new Uri(serviceUrl), new SpnEndpointIdentity(String.Empty));
            //ServiceEndpoint point = GZipMessageEncodingBindingElement.CreateEndpoint(myEndpointAddress);
            //ChannelFactory<IDXVCSService> factory = new Factory(point);
            //factory.Credentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Identification;
            //if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
            //    factory.Credentials.Windows.ClientCredential = new NetworkCredential(user, password);
            //IDXVCSService newService = factory.CreateChannel();
            //IContextChannel newChannel = (IContextChannel)newService;
            //newChannel.OperationTimeout = TimeSpan.FromSeconds(10);
            //newChannel.Open();
            //return new ServiceWrapper(newService);
        }

        public override object InitializeLifetimeService() {
            return null; // lease never expires
        }
    }
}