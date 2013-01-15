using System;
using StructureMap;
using System.Threading;

namespace TinyHandler.ConsoleSample
{
    public class Program
    {
        static void Main(string[] args)
        {
            //Run once
            Setup();

            HandlerCentral.Process(new TestObject());

            Thread.Sleep(500);

            Console.Write(ConsoleOut);

            Thread.Sleep(2000);
        }

        public static string ConsoleOut;
       
       

        public static void Setup()
        {
            ObjectFactory.Configure(x => x.Scan(y =>
                {
                    y.AssemblyContainingType<TestObjectProcessModule>();
                    y.IncludeNamespaceContainingType<TestObjectProcessModule>();
                    y.AddAllTypesOf<IProcessor>();
                    y.AddAllTypesOf<ISubscription>();
                    y.WithDefaultConventions();
                }
                ));
            HandlerCentral.AddProcessBehaviors<TimerProcessBehavior>();
        }
    }

    

    public class TestObject { }

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
                Program.ConsoleOut = "TestSubscription ran";
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
        public void StartLog() { }
        public void EndLog() { }
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
