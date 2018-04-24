using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Executor.UnitTest
{
    [TestClass]
    public class ExecutorTest
    {
        private TimeSpan _timeout = TimeSpan.FromSeconds(2);
         
        [TestMethod]
        public void Should_RunOneTask()
        {
            // Arrange
            var cut = new Executor();
            var done = new AutoResetEvent(false);

            // Act
            cut.AddForExecution(() => done.Set());
            var res = done.WaitOne(_timeout);

            // Assert
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Should_RunSeveralTasksInRightOrder()
        {
            // Arrange
            var cut = new Executor();
            var done = new AutoResetEvent(false);
            var x = string.Empty;

            // Act
            cut.AddForExecution(() => { x += 'A'; });
            cut.AddForExecution(() => { x += 'B'; });
            cut.AddForExecution(() => { x += 'C'; done.Set(); });
            done.WaitOne(_timeout);

            // Assert
            Assert.AreEqual("ABC", x);
        }

        [TestMethod]
        public void Should_AllowSeveralClientsToAddTasks()
        {
            // Arrange
            var cut = new Executor();
            var x = 0;
            var done = new AutoResetEvent(false);

            // Act
            Parallel.For(0, 100, (i) => { cut.AddForExecution(() => x++); });
            cut.AddForExecution(() => { done.Set(); });
            done.WaitOne();

            // Assert
            Assert.AreEqual(100, x);
        }

        [TestMethod]
        public void Should_RunNextTask_When_ExceptionIsThrown()
        {
            // Arrange
            var cut = new Executor();
            var done = new AutoResetEvent(false);

            // Act
            cut.AddForExecution(() => { throw new Exception(); });
            cut.AddForExecution(() => { done.Set(); });

            var res = done.WaitOne(_timeout);

            // Assert
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Should_AllowToDisposeTwice()
        {
            // Arrange
            var cut = new Executor();

            // Act
            cut.Dispose();
            cut.Dispose();

            // Assert: no exception is expected
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Should_NotAllowToAddTaskAfterDisposal()
        {
            // Arrange
            var cut = new Executor();
            cut.Dispose();

            // Act
            cut.AddForExecution(() => { });

            // Assert: exception is expected
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowToAddNullTask()
        {
            // Arrange
            var cut = new Executor();
            // Act
            cut.AddForExecution(null);

            // Assert: exception is expected
        }
    }
}