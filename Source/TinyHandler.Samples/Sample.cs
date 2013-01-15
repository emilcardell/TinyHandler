﻿using System;
using StructureMap;

namespace TinyHandler.Samples
{
    public class Sample
    {
        public static string SubscriptionOne;
        public static bool SubscriptionTwo;

        public Sample()
        {
            //Run once
            Setup();

            HandlerCentral.Process(new TestObject());
        }

        public void Setup()
        {
            ObjectFactory.Configure(x => x.Scan(y => y.AssemblyContainingType<FakeLogger>()));
            HandlerCentral.AddProcessBehaviors<TimerProcessBehavior>();
        }
    }

    public class TestObject{}

    public class TestObjectProcessModule : Processor<TestObject>
    {
        
        public TestObjectProcessModule(FakeRepo repo, FakeBus bus, FakeLogger logger)
        {
            Process = testObjectToHandle => 
            { 
                repo.Save(testObjectToHandle);
                return testObjectToHandle;
            };          

            OnProcessError = (testObjectThatFailed, exception) => logger.LogSpecialCase(exception);
        }
    }

    public class TestSubscription : Subscription<TestObject>
    {
        public TestSubscription(FakeRepo repo, FakeBus bus, FakeLogger logger)
        {
            OnProcessed = testEvent =>
            {
                Sample.SubscriptionOne = "Test";
            };
        }
    }


    public class TimerProcessBehavior : BasicProcessBehaviour
    {
        private readonly FakeLogger _fakeLogger;

        public TimerProcessBehavior(FakeLogger fakeLogger)
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
