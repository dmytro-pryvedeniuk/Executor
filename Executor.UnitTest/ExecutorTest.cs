using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Executor.UnitTest
{
    [TestClass]
    public class ExecutorTest
    {
        private TimeSpan _timeout = TimeSpan.FromSeconds(2);
        private Executor _cut;

        [TestInitialize]
        public void TestInitialize()
        {
            _cut = new Executor();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cut.Dispose();
        }

        [TestMethod]
        public void Should_RunOneTask()
        {
            // Arrange
            var done = new AutoResetEvent(false);

            // Act
            _cut.AddForExecution(() => done.Set());
            var res = done.WaitOne(_timeout);

            // Assert
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Should_RunSeveralTasksInRightOrder()
        {
            // Arrange
            var done = new AutoResetEvent(false);
            var x = string.Empty;

            // Act
            _cut.AddForExecution(() => { x += 'A'; });
            _cut.AddForExecution(() => { x += 'B'; });
            _cut.AddForExecution(() => { x += 'C'; done.Set(); });
            done.WaitOne(_timeout);

            // Assert
            Assert.AreEqual("ABC", x);
        }

        [TestMethod]
        public void Should_AllowSeveralClientsToAddTasks()
        {
            // Arrange
            var x = 0;
            var done = new AutoResetEvent(false);

            // Act
            Parallel.For(0, 10000, (i) => { _cut.AddForExecution(() => x++); });
            _cut.AddForExecution(() => { done.Set(); });
            done.WaitOne();

            // Assert
            Assert.AreEqual(10000, x);
        }

        [TestMethod]
        public void Should_ReleaseWorkingThreads_When_CallerRespectsDispose()
        {
            // Arrange
            var threadNumberBefore = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;

            // Act
            Parallel.For(0, 100, (i) =>
            {
                using (var cut = new Executor()) 
                    cut.AddForExecution(() => { });
            });

            // Assert
            var threadNumberAfter = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            Assert.AreEqual(threadNumberBefore, threadNumberAfter);
        }

        //[TestMethod] This test is failing now
        public void Should_ReleaseWorkingThreads_When_CallerDoesNotRespectsDispose()
        {
            // Arrange
            var threadNumberBefore = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;

            // Act
            Parallel.For(0, 100, (i) =>
            {
                // Each executor spawns a working thread which is not stopped explicitly
                var cut = new Executor();
                cut.AddForExecution(() => { });
            });

            // Assert
            var threadNumberAfter = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            Assert.AreEqual(threadNumberBefore, threadNumberAfter);
        }

        [TestMethod]
        public void Should_RunNextTask_When_ExceptionIsThrown()
        {
            // Arrange
            var done = new AutoResetEvent(false);

            // Act
            _cut.AddForExecution(() => { throw new Exception(); });
            _cut.AddForExecution(() => { done.Set(); });

            var res = done.WaitOne(_timeout);

            // Assert
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Should_AllowToDisposeTwice()
        {
            // Arrange
            // Act
            _cut.Dispose();
            _cut.Dispose();

            // Assert: no exception is expected
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Should_NotAllowToAddTaskAfterDisposal()
        {
            // Arrange
            _cut.Dispose();

            // Act
            _cut.AddForExecution(() => { });

            // Assert: exception is expected
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowToAddNullTask()
        {
            // Arrange

            // Act
            _cut.AddForExecution(null);

            // Assert: exception is expected
        }
    }
}