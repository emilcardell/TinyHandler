using System;
using StructureMap;

namespace TinyHandler.Samples
{
    public class Sample
    {
        public Sample()
        {
            //Run once
            Setup();

            HandlerCentral.Process(new TestObject());
        }

        public void Setup()
        {
            ObjectFactory.Configure(x => x.Scan(y => y.AssemblyContainingType<FakeLogger>()));
            ObjectFactory.Configure(x => x.For<HandlerModule<TestObject>>().Use<TestObjectHandlerModule>());
            HandlerCentral.AddProcessBehaviors<TimerProcessBehvior>();
        }
    }

    public class TestObject{}

    public class TestObjectHandlerModule : HandlerModule<TestObject>
    {
        
        public TestObjectHandlerModule(FakeRepo repo, FakeBus bus, FakeLogger logger)
        {
            Process = testObjectToHandle => 
            { 
                repo.Save(testObjectToHandle);
                return testObjectToHandle;
            };

            Dispatch = testObjectToDispatch => { bus.Publish(testObjectToDispatch); };

            OnProcessError = (testObjectThatFailed, exception) => logger.LogSpecialCase(exception);
        }
    }


    public class TimerProcessBehvior : BasicProcessBehaviour
    {
        private readonly FakeLogger _fakeLogger;

        public TimerProcessBehvior(FakeLogger fakeLogger)
        {
            _fakeLogger = fakeLogger;
        }

        public override object Invoke(object handledObject)
        {
            _fakeLogger.StartLog();
            var result = InvokeNext(handledObject);
            _fakeLogger.EndLog();
            return result;
        }
    }
    
    public class FakeLogger
    {
        public void StartLog(){}
        public void EndLog(){}
        public void LogSpecialCase(Exception exception) { }
    }

    public class FakeRepo
    {
        public void Save(object objectToSave) { }
    }

    public class FakeBus
    {
        public void Publish(object objectToPublish) { }
    }

}
