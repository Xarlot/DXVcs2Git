using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace DXVCS {
    public abstract class ServiceBase<T> : IDisposable {
        public bool IsAdmin;
        public abstract T Service { get; }
        public virtual void Dispose() {
        }
    }
    class WCFService<T> : ServiceBase<T> {
        List<DataBase<T>> channels;
        ChannelFactory<T> factory;
        Timer closeTimer;
        CreateServiceImplementor<T> createServiceImplementor;
        public WCFService(ChannelFactory<T> factory, CreateServiceImplementor<T> createServiceImplementor = null) {
            this.factory = factory;
            this.createServiceImplementor = createServiceImplementor;
            channels = new List<DataBase<T>>();
            factory.Endpoint.Behaviors.Add(new AutoPoolBehavior<T>(this));
            closeTimer = new Timer(Close, null, 0, 5000);
        }
        void Close(object state) {
            List<IClientChannel> removed = new List<IClientChannel>();
            lock(this) {
                for(int i = channels.Count - 1; i >= 0; i--) {
                    DataBase<T> d = channels[i];
                    try {
                        if(d.Used && d.Channel.State == CommunicationState.Opened)
                            continue;
                        if(d.Channel.State == CommunicationState.Opened && d.LastAccess.Elapsed.TotalSeconds < 20)
                            continue;
                        removed.Add(d.Channel);
                        channels.RemoveAt(i);
                    } catch {
                    }
                }
            }
            foreach(IClientChannel channel in removed) {
                try {
                    if(channel.State == CommunicationState.Opened) {
                        channel.Close();
                    } else
                        if(channel.State == CommunicationState.Faulted)
                            channel.Abort();
                } catch {
                }
            }
        }
        T Create() {
            T newService = factory.CreateChannel();
            IContextChannel newChannel = (IContextChannel)newService;
            newChannel.OperationTimeout = new TimeSpan(0, 10, 0);
            newChannel.Open();
            return newService;
        }
        public void ReturnService(bool fault) {
            IClientChannel faulted = null;
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock(this) {
                for(int i = 0; i < channels.Count; i++) {
                    DataBase<T> d = channels[i];
                    if(d.ThreadId == threadId) {
                        if(fault) {
                            faulted = d.Channel;
                            channels.RemoveAt(i);
                        } else
                            d.Used = false;
                        break;
                    }
                }
            }
            if(faulted != null) {
                try {
                    if(faulted.State == CommunicationState.Faulted)
                        faulted.Abort();
                    else
                        if(faulted.State == CommunicationState.Opened)
                            faulted.Close();
                } catch {
                }
            }
        }
        public override T Service {
            get {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                lock(this) {
                    for(int i = 0; i < channels.Count; i++) {
                        DataBase<T> d = channels[i];
                        if(!d.Used) {
                            d.Used = true;
                            d.ThreadId = threadId;
                            return d.Service;
                        }
                    }
                }
                T service = Create();
                DataBase<T> dd = new DataBase<T>(service, threadId, createServiceImplementor);
                lock(this)
                    channels.Add(dd);
                return dd.Service;
            }
        }
    }
    class DataBase<T> {
        T service;
        IClientChannel channel;
        int threadId;
        Stopwatch lastAccess = Stopwatch.StartNew();
        bool used;
        public T Service { get { return service; } }
        public IClientChannel Channel {
            get { return channel; }
        }
        public int ThreadId { get { return threadId; } set { threadId = value; } }
        public bool Used {
            get {
                return used;
            }
            set {
                used = value;
                if(!used)
                    lastAccess.Restart();
            }
        }
        public Stopwatch LastAccess { get { return lastAccess; } }
        public DataBase(T service, int threadId, CreateServiceImplementor<T> createServiceImplementor) {
            this.channel = (IClientChannel)service;
            if(createServiceImplementor != null)
                this.service = (T)createServiceImplementor(service);
            else
                this.service = service;
            this.threadId = threadId;
            Used = true;
        }
    }
    class AutoPoolBehavior<T> : IEndpointBehavior {
        WCFService<T> client;
        public AutoPoolBehavior(WCFService<T> client) {
            this.client = client;
        }
        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters) {
        }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime) {
            AutoPoolInspector<T> inspector = new AutoPoolInspector<T>(client);
            clientRuntime.MessageInspectors.Add(inspector);
        }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher) {
        }

        public void Validate(ServiceEndpoint endpoint) {
        }
    }
    class AutoPoolInspector<T> : IClientMessageInspector {
        WCFService<T> client;
        public AutoPoolInspector(WCFService<T> client) {
            this.client = client;
        }
        void IClientMessageInspector.AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState) {
            client.ReturnService(reply.IsFault);
        }

        object IClientMessageInspector.BeforeSendRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel) {
            return null;
        }
    }
    class Factory<T> : ChannelFactory<T> {
        public Factory(ServiceEndpoint endpoint) : base(endpoint) { }
        protected override void ApplyConfiguration(string configurationName) {
        }
    }
    delegate object CreateServiceImplementor<T>(T service);
    public static class ConnectToServiceWCFHelper {
        static Dictionary<string, WCFService<IDXVCSService>> services = new Dictionary<string, WCFService<IDXVCSService>>();
        public static ServiceBase<IDXVCSService> ConnectWCF(string auxPath, string user, string password) {
            lock(services) {
                WCFService<IDXVCSService> service;
                string serviceName = string.Format("{0}#$%%$#{1}#@%%@#{2}", auxPath, string.IsNullOrEmpty(user) ? string.Empty : user, string.IsNullOrEmpty(password));
                if(!services.TryGetValue(serviceName, out service)) {
                    EndpointAddress myEndpointAddress = new EndpointAddress(new Uri(auxPath), new SpnEndpointIdentity(String.Empty));
                    ServiceEndpoint point = GZipMessageEncodingBindingElement.CreateEndpoint(myEndpointAddress, typeof(IDXVCSService));
                    ChannelFactory<IDXVCSService> factory = new Factory<IDXVCSService>(point);
                    factory.Credentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Identification;
                    if(!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password)) {
                        factory.Credentials.Windows.ClientCredential = new System.Net.NetworkCredential(user, password);
                    }
                    service = new WCFService<IDXVCSService>(factory, delegate(IDXVCSService s) { return new ServiceLogger(s); });
                    services.Add(serviceName, service);
                }
                return service;
            }
        }
    }
}
