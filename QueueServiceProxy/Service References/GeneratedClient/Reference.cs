﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.261
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FloatingQueue.ServiceProxy.GeneratedClient {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="PingResult", Namespace="http://schemas.datacontract.org/2004/07/FloatingQueue.Server.Service")]
    [System.SerializableAttribute()]
    public partial class PingResult : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private int ResultCodeField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int ResultCode {
            get {
                return this.ResultCodeField;
            }
            set {
                if ((this.ResultCodeField.Equals(value) != true)) {
                    this.ResultCodeField = value;
                    this.RaisePropertyChanged("ResultCode");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="GeneratedClient.IQueueService")]
    public interface IQueueService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IQueueService/Push", ReplyAction="http://tempuri.org/IQueueService/PushResponse")]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(object[]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(FloatingQueue.ServiceProxy.GeneratedClient.PingResult))]
        void Push(string aggregateId, int version, object e);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IQueueService/TryGetNext", ReplyAction="http://tempuri.org/IQueueService/TryGetNextResponse")]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(object[]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(FloatingQueue.ServiceProxy.GeneratedClient.PingResult))]
        bool TryGetNext(out object next, string aggregateId, int version);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IQueueService/GetAllNext", ReplyAction="http://tempuri.org/IQueueService/GetAllNextResponse")]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(object[]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(FloatingQueue.ServiceProxy.GeneratedClient.PingResult))]
        object[] GetAllNext(string aggregateId, int version);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IQueueService/Ping", ReplyAction="http://tempuri.org/IQueueService/PingResponse")]
        FloatingQueue.ServiceProxy.GeneratedClient.PingResult Ping();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IQueueServiceChannel : FloatingQueue.ServiceProxy.GeneratedClient.IQueueService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class QueueServiceClient : System.ServiceModel.ClientBase<FloatingQueue.ServiceProxy.GeneratedClient.IQueueService>, FloatingQueue.ServiceProxy.GeneratedClient.IQueueService {
        
        public QueueServiceClient() {
        }
        
        public QueueServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public QueueServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public QueueServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public QueueServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void Push(string aggregateId, int version, object e) {
            base.Channel.Push(aggregateId, version, e);
        }
        
        public bool TryGetNext(out object next, string aggregateId, int version) {
            return base.Channel.TryGetNext(out next, aggregateId, version);
        }
        
        public object[] GetAllNext(string aggregateId, int version) {
            return base.Channel.GetAllNext(aggregateId, version);
        }
        
        public FloatingQueue.ServiceProxy.GeneratedClient.PingResult Ping() {
            return base.Channel.Ping();
        }
    }
}
