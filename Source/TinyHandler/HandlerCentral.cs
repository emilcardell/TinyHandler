using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using StructureMap;

namespace TinyHandler
{
    public class HandlerCentral
    {

        private static readonly List<Type> ProcessBehaviors = new List<Type>();
        private static readonly List<Type> OnProcessErrorBehaviors = new List<Type>();
        private static readonly List<Type> SubscriptionBehaviors = new List<Type>();

        public static IContainer Container = ObjectFactory.Container;

        public static void AddProcessBehaviors<TProcessBehavior>() where TProcessBehavior : IProcessBehavior
        {
            ProcessBehaviors.Add(typeof(TProcessBehavior));
        }

        public static void AddOnProcessErrorBehaviors<TOnProcessErrorBehavior>() where TOnProcessErrorBehavior : IOnProcessErrorBehavior
        {
            ProcessBehaviors.Add(typeof(TOnProcessErrorBehavior));
        }

        public static void AddSubscriptionBehaviorss<TSubscriptionBehavior>() where TSubscriptionBehavior : ISubscriptionBehavior
        {
            ProcessBehaviors.Add(typeof(TSubscriptionBehavior));
        }

        public static void Process<T>(T objectToHandle)
        {
            Process<T, object>(objectToHandle);
        }

        public static TOut Process<T, TOut>(T objectToHandle)
        {
            var processors = Container.GetAllInstances<IProcessor>().OfType<Processor<T>>();

            if (processors.Count() == 0)
            {
                ThreadPool.QueueUserWorkItem(x => PublishSubscriptions(objectToHandle));
                return default(TOut);
            }

            if (processors.Count() > 1)
                throw new ApplicationException("There can only be one process module for each type.");

            try
            {
                IProcessBehavior startOfBehaviorChain = new ProcessBehaviorExecuter<T>(processors.FirstOrDefault().Process);

                var reveresedBehaviorChain = ProcessBehaviors;
                reveresedBehaviorChain.Reverse();

                foreach (var behviorType in reveresedBehaviorChain)
                {
                    var newBehaviour = Container.GetInstance(behviorType) as IProcessBehavior;

                    if (newBehaviour == null)
                        continue;
                        
                    newBehaviour.NextBehavior = startOfBehaviorChain;
                    startOfBehaviorChain = newBehaviour;

                }

                var result = (TOut)startOfBehaviorChain.Invoke(objectToHandle);
                ThreadPool.QueueUserWorkItem(x => PublishSubscriptions(objectToHandle));
                return result;

            }
            catch (Exception exception)
            {
                IOnProcessErrorBehavior startOfOnProcessBehaviorChain =
                    new OnProcessErrorBehaviorExecuter<T>(processors.FirstOrDefault().OnProcessError);

                var reveresedBehaviorChain = OnProcessErrorBehaviors;
                reveresedBehaviorChain.Reverse();

                foreach (var behviorType in reveresedBehaviorChain)
                {
                    var newBehaviour = Container.GetInstance(behviorType) as IOnProcessErrorBehavior;
                        
                    if(newBehaviour == null)
                        continue;
                        
                    newBehaviour.NextBehavior = startOfOnProcessBehaviorChain;
                    startOfOnProcessBehaviorChain = newBehaviour;

                }

                startOfOnProcessBehaviorChain.Invoke(objectToHandle, exception);
                throw;
            }

            
        }

        private static void PublishSubscriptions<T>(T objectToHandle)
        {
            var subscriptions = Container.GetAllInstances<ISubscription>().OfType<Subscription<T>>();

            foreach (var subscriptionModule in subscriptions)
            {
                try
                {
                    ISubscriptionBehavior startOfSubscriptionBehaviorChain = new SubscriptionBehaviorExecuter<T>(subscriptionModule.OnProcessed);

                    var reveresedSubscriptionBehaviorChain = SubscriptionBehaviors;
                    reveresedSubscriptionBehaviorChain.Reverse();

                    foreach (var behviorType in reveresedSubscriptionBehaviorChain)
                    {
                        var newBehaviour = Container.GetInstance(behviorType) as ISubscriptionBehavior;

                        if (newBehaviour == null)
                            continue;

                        newBehaviour.NextBehavior = startOfSubscriptionBehaviorChain;
                        startOfSubscriptionBehaviorChain = newBehaviour;

                    }

                    startOfSubscriptionBehaviorChain.Invoke(objectToHandle);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }
    }

    public class ProcessBehaviorExecuter<T> : IProcessBehavior
    {
        public ProcessBehaviorExecuter(Func<T, object> processAction)
        {
            ProcessAction = processAction;
        }
        public IProcessBehavior NextBehavior { get; set; }
        public Func<T, object> ProcessAction { get; set; }

        public object Invoke(object handledObject)
        {
            if(ProcessAction != null)
                return ProcessAction.Invoke((T)handledObject);

            return ReturnValue.Empty;
        }
    }

    public class SubscriptionBehaviorExecuter<T> : ISubscriptionBehavior
    {
        public SubscriptionBehaviorExecuter(Action<T> processAction)
        {
            SubscriptionAction = processAction;
        }
        public ISubscriptionBehavior NextBehavior { get; set; }
        public Action<T> SubscriptionAction { get; set; }

        public void Invoke(object handledObject)
        {
            if(SubscriptionAction != null)
                SubscriptionAction.Invoke((T)handledObject);
        }
    }

    public class OnProcessErrorBehaviorExecuter<T> : IOnProcessErrorBehavior
    {
        public OnProcessErrorBehaviorExecuter(Action<T, Exception> onProcessErrorAction)
        {
            OnProcessErrorAction = onProcessErrorAction;
        }
        public IOnProcessErrorBehavior NextBehavior { get; set; }
        Action<T, Exception> OnProcessErrorAction { get; set; }

        public void Invoke(object handledObject, Exception exception)
        {
            if(OnProcessErrorAction != null)
                OnProcessErrorAction.Invoke((T)handledObject, exception);

            throw exception;
        }
    }

    public abstract class Processor<T> : IProcessor
    {
        public Func<T, object> Process { get; set; }
        public Action<T, Exception> OnProcessError { get; set; }
    }

    public interface IProcessor
    {
    }

    public abstract class BasicProcessBehaviour : IProcessBehavior
    {
        public IProcessBehavior NextBehavior { get; set; }

        public abstract object Invoke(object handledObject);

        public object InvokeNext(object handledObject)
        {
            if (NextBehavior != null)
                return NextBehavior.Invoke(handledObject);

            return null;
        }
    }

    public interface IProcessBehavior
    {
        IProcessBehavior NextBehavior { get; set; }
        object Invoke(object handledObject);
    }


    public abstract class BasicSubscriptionBehaviour : ISubscriptionBehavior
    {
        public ISubscriptionBehavior NextBehavior { get; set; }

        public abstract void Invoke(object handledObject);

        public void InvokeNext(object handledObject)
        {
            if (NextBehavior != null)
                NextBehavior.Invoke(handledObject);
        }
    }

    public interface ISubscriptionBehavior
    {
        ISubscriptionBehavior NextBehavior { get; set; }
        void Invoke(object handledObject);
    }

    public abstract class BasicOnProcessErrorBehaviour : IOnProcessErrorBehavior
    {
        public IOnProcessErrorBehavior NextBehavior { get; set; }

        public abstract void Invoke(object handledObject, Exception thrownException);

        public void InvokeNext(object handledObject, Exception thrownException)
        {
            if (NextBehavior != null)
                NextBehavior.Invoke(handledObject, thrownException);
        }
    }

    public interface IOnProcessErrorBehavior
    {
        IOnProcessErrorBehavior NextBehavior { get; set; }
        void Invoke(object handledObject, Exception thrownException);
    }
}
