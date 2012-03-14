using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using FloatingQueue.Common;
using FloatingQueue.Common.Proxy;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Replication;

namespace FloatingQueue.Server.Service
{
    public class QueueService : IQueueService
    {
        public void Push(string aggregateId, int version, object e)
        {
            Core.Server.Log.Info("Command: push {0} {1} {2}", aggregateId, version, e);
            // todo: ensure that slaves do not allow writes directly from client
            var aggregate = GetEventAggregate(aggregateId);
            try
            {
                aggregate.Push(version, e);
                if (Core.Server.Configuration.IsMaster)
                {
                    var replicated = Core.Server.Resolve<IConnectionManager>().TryReplicate(aggregateId, version, e);
                    if (!replicated)
                    {
                        throw new ApplicationException("Cannot replicate the data.");
                    }
                }
                else
                {
                    // todo: find a better place to open connections from slaves
                    Core.Server.Resolve<IConnectionManager>().OpenOutcomingConnections();
                }
                aggregate.Commit();
            }
            catch
            {
                aggregate.Rollback();
                throw;
            }
        }

        private static IEventAggregate GetEventAggregate(string aggregateId)
        {
            IEventAggregate aggregate;
            if (!AggregateRepository.Instance.TryGetEventAggregate(aggregateId, out aggregate))
            {
                aggregate = AggregateRepository.Instance.CreateAggregate(aggregateId);
            }
            return aggregate;
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.TryGetNext(version, out next);
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.GetAllNext(version);
        }

        public PingResult Ping(PingParams pingParams)
        {
            if (pingParams.NodeInfo == null)
                throw new ArgumentException("NodeInfo can't be null");

            switch (pingParams.Reason)
            {
                case PingReason.ConnectionCheck:
                    return new PingResult();

                case PingReason.IntroductionOfNewNode:
                    AddNewNode(pingParams.NodeInfo);
                    return new PingResult();

                case PingReason.RequestForSyncronization:
                    // validate request
                    // create task to share all data with this node
                    return new PingResult();

                case PingReason.NotificationOfSlaveReadiness:
                    Core.Server.Configuration.Nodes.Siblings
                        .Single(n => n.ServerId == pingParams.NodeInfo.ServerId)
                        .DeclareAsSyncedNode();
                    return new PingResult();

                default:
                    throw new NotImplementedException(string.Format("Reason {0} is not supported yet", pingParams.Reason));
            }
        }

        private static void AddNewNode(NodeInfo newNode)
        {
            Core.Server.Configuration.Nodes.AddNewNode(new NodeConfiguration
            {
                Address = newNode.Address,
                Proxy = new ManualQueueServiceProxy(newNode.Address),
                ServerId = newNode.ServerId,
                IsSynced = false,
                IsMaster = false,
                IsReadonly = false
            });
        }

    }
}
