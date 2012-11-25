using System;
using System.Windows.Input;
using Jamiras.Commands;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Commands
{
    [TestFixture]
    class CommandBaseTests
    {
        private class TestCommand : CommandBase
        {
            public bool IsExecuted { get; set; }

            public override void Execute()
            {
                IsExecuted = true;
            }

            public void SetCanExecute(bool newValue)
            {
                CanExecute = newValue;
            }
        }

        [Test]
        public void TestInheritance()
        {
            var command = new TestCommand();
            Assert.That(command, Is.InstanceOf<ICommand>());
        }

        [Test]
        public void TestCanExecute()
        {
            var command = new TestCommand();
            Assert.That(command.CanExecute, Is.True);
            Assert.That(((ICommand)command).CanExecute(null), Is.True);
            Assert.That(command.IsExecuted, Is.False);
        }

        [Test]
        public void TestExecute()
        {
            var command = new TestCommand();
            Assert.That(command.IsExecuted, Is.False);
            command.Execute();
            Assert.That(command.IsExecuted, Is.True);
        }

        [Test]
        [RequiresSTA]
        public void TestExecuteICommand()
        {
            var command = new TestCommand();
            Assert.That(command.IsExecuted, Is.False);
            ((ICommand)command).Execute(null);
            Assert.That(command.IsExecuted, Is.True);
        }

        [Test]
        public void TestCanExecuteChanged()
        {
            var eventRaised = false;
            var command = new TestCommand();
            command.CanExecuteChanged += (o, e) => eventRaised = true;
            
            command.SetCanExecute(true);
            Assert.That(eventRaised, Is.False);

            command.SetCanExecute(false);
            Assert.That(eventRaised, Is.True);
        }

        private class TestParameterizedCommand : CommandBase<int>
        {
            public int ExecuteParameter { get; private set; }

            public override bool CanExecute(int parameter)
            {
                return (parameter % 2) == 0;
            }

            public override void Execute(int parameter)
            {
                ExecuteParameter = parameter;
            }

            public void RaiseCanExecuteChanged()
            {
                OnCanExecuteChanged(EventArgs.Empty);
            }
        }

        [Test]
        public void TestParameterizedInheritance()
        {
            var command = new TestParameterizedCommand();
            Assert.That(command, Is.InstanceOf<ICommand>());
        }

        [Test]
        public void TestParameterizedCanExecute()
        {
            var command = new TestParameterizedCommand();
            Assert.That(command.CanExecute(4), Is.True);
            Assert.That(command.CanExecute(7), Is.False);
            Assert.That(((ICommand)command).CanExecute(4), Is.True);
            Assert.That(((ICommand)command).CanExecute(7), Is.False);
            Assert.That(command.ExecuteParameter, Is.EqualTo(0));
        }

        [Test]
        public void TestParameterizedCanExecuteInvalidParameter()
        {
            var command = new TestParameterizedCommand();
            Assert.That(((ICommand)command).CanExecute(null), Is.False);
            Assert.That(((ICommand)command).CanExecute("happy"), Is.False);
        }

        [Test]
        public void TestParameterizedExecute()
        {
            var command = new TestParameterizedCommand();
            Assert.That(command.ExecuteParameter, Is.EqualTo(0));
            command.Execute(4);
            Assert.That(command.ExecuteParameter, Is.EqualTo(4));
        }

        [Test]
        [RequiresSTA]
        public void TestParameterizedExecuteICommand()
        {
            var command = new TestParameterizedCommand();
            Assert.That(command.ExecuteParameter, Is.EqualTo(0));
            ((ICommand)command).Execute(4);
            Assert.That(command.ExecuteParameter, Is.EqualTo(4));
        }

        [Test]
        public void TestParameterizedCanExecuteChanged()
        {
            var eventRaised = false;
            var command = new TestParameterizedCommand();
            command.CanExecuteChanged += (o, e) => eventRaised = true;

            command.RaiseCanExecuteChanged();
            Assert.That(eventRaised, Is.True);
        }
    }
}
