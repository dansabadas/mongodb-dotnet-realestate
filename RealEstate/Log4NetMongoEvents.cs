using System;
using log4net;
using MongoDB.Driver.Core.Events;

namespace RealEstate
{
    public class Log4NetMongoEvents : IEventSubscriber
    {
        public static ILog CommandStarted = LogManager.GetLogger("CommandStarted");

        private ReflectionEventSubscriber _subscriber;

        public Log4NetMongoEvents()
        {
            _subscriber = new ReflectionEventSubscriber(this);
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            //if (typeof(TEvent) != typeof(CommandStartedEvent))
            //{
            //    handler = null;
            //    return false;
            //}

            //handler = e => { };
            //return true;

            return _subscriber.TryGetEventHandler(out handler);
        }

        public void Handle(CommandStartedEvent started)
        {
            CommandStarted.Info(new
            {
                started.Command,
                started.CommandName,
                started.ConnectionId,
                started.DatabaseNamespace,
                started.OperationId,
                started.RequestId
            });
        }

        public void Handle(CommandSucceededEvent succeeded)
        {

        }
    }
}