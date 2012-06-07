using System;
using System.Collections.Generic;
using System.Threading;
using StructureMap;

namespace TinyHandler
{
    public class HandlerCentral
    {

        private static readonly List<Type> ProcessBehaviors = new List<Type>();
        private static readonly List<Type> OnProcessErrorBehaviors = new List<Type>();
        private static readonly List<Type> DispatchBehaviors = new List<Type>();

        public static IContainer Container = ObjectFactory.Container;

        public static void AddProcessBehaviors<TProcessBehavior>() where TProcessBehavior : IProcessBehavior
        {
            ProcessBehaviors.Add(typeof(TProcessBehavior));
        }

        public static void AddOnProcessErrorBehaviors<TOnProcessErrorBehavior>() where TOnProcessErrorBehavior : IOnProcessErrorBehavior
        {
            ProcessBehaviors.Add(typeof(TOnProcessErrorBehavior));
        }

        public static void AddDispatchBehaviorss<TDispatchBehavior>() where TDispatchBehavior : IDispatchBehavior
        {
            ProcessBehaviors.Add(typeof(TDispatchBehavior));
        }

        public static void Process<T>(T objectToHandle)
        {
            var handlerModule = Container.GetInstance<HandlerModule<T>>();

            try
            {
                IProcessBehavior startOfBehaviorChain = new ProcessBehaviorExecuter<T>(handlerModule.Process);

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

                startOfBehaviorChain.Invoke(objectToHandle);

            }
            catch (Exception exception)
            {
                IOnProcessErrorBehavior startOfOnProcessBehaviorChain =
                    new OnProcessErrorBehaviorExecuter<T>(handlerModule.OnProcessError);

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

            ThreadPool.QueueUserWorkItem(x => Dispatch(objectToHandle));
        }

        private static void Dispatch<T>(T objectToHandle)
        {
            var handlerModule = Container.GetInstance<HandlerModule<T>>();
            IDispatchBehavior startOfDispatchBehaviorChain = new DispatchBehaviorExecuter<T>(handlerModule.Dispatch);

            var reveresedDispatchBehaviorChain = DispatchBehaviors;
            reveresedDispatchBehaviorChain.Reverse();

            foreach (var behviorType in reveresedDispatchBehaviorChain)
            {
                var newBehaviour = Container.GetInstance(behviorType) as IDispatchBehavior;

                if (newBehaviour == null)
                    continue;

                newBehaviour.NextBehavior = startOfDispatchBehaviorChain;
                startOfDispatchBehaviorChain = newBehaviour;

            }

            startOfDispatchBehaviorChain.Invoke(objectToHandle);
        }
    }

    public class ProcessBehaviorExecuter<T> : IProcessBehavior
    {
        public ProcessBehaviorExecuter(Action<T> processAction)
        {
            ProcessAction = processAction;
        }
        public IProcessBehavior NextBehavior { get; set; }
        public Action<T> ProcessAction { get; set; }

        public void Invoke(object handledObject)
        {
            if(ProcessAction != null)
                ProcessAction.Invoke((T)handledObject);
        }
    }

    public class DispatchBehaviorExecuter<T> : IDispatchBehavior
    {
        public DispatchBehaviorExecuter(Action<T> processAction)
        {
            DispatchAction = processAction;
        }
        public IDispatchBehavior NextBehavior { get; set; }
        public Action<T> DispatchAction { get; set; }

        public void Invoke(object handledObject)
        {
            if(DispatchAction != null)
                DispatchAction.Invoke((T)handledObject);
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

    public abstract class HandlerModule<T>
    {
        public Action<T> Process { get; set; }
        public Action<T> Dispatch { get; set; }
        public Action<T, Exception> OnProcessError { get; set; }
    }

    public abstract class BasicProcessBehaviour : IProcessBehavior
    {
        public IProcessBehavior NextBehavior { get; set; }

        public abstract void Invoke(object handledObject);

        public void InvokeNext(object handledObject)
        {
            if (NextBehavior != null)
                NextBehavior.Invoke(handledObject);
        }
    }

    public interface IProcessBehavior
    {
        IProcessBehavior NextBehavior { get; set; }
        void Invoke(object handledObject);
    }


    public abstract class BasicDispatchBehaviour : IDispatchBehavior
    {
        public IDispatchBehavior NextBehavior { get; set; }

        public abstract void Invoke(object handledObject);

        public void InvokeNext(object handledObject)
        {
            if (NextBehavior != null)
                NextBehavior.Invoke(handledObject);
        }
    }

    public interface IDispatchBehavior
    {
        IDispatchBehavior NextBehavior { get; set; }
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
