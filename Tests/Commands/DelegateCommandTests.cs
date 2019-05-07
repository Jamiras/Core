using System;
using System.Windows.Input;
using Jamiras.Commands;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Commands
{
    [TestFixture]
    class DelegateCommandTests
    {
        [Test]
        public void TestInheritance()
        {
            var command = new DelegateCommand(() => { });
            Assert.That(command, Is.InstanceOf<ICommand>());
            Assert.That(command, Is.InstanceOf<CommandBase>());
        }

        [Test]
        public void TestNoDelegate()
        {
            Assert.That(() => new DelegateCommand(null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void TestCanExecute()
        {
            var executed = false;
            var command = new DelegateCommand(() => executed = true);
            Assert.That(command.CanExecute, Is.True);
            Assert.That(((ICommand)command).CanExecute(null), Is.True);
            Assert.That(executed, Is.False);
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void TestExecute()
        {
            var executed = false;
            var command = new DelegateCommand(() => executed = true);
            Assert.That(executed, Is.False);
            command.Execute();
            Assert.That(executed, Is.True);
        }

        [Test]
        public void TestExecuteICommand()
        {
            var executed = false;
            var command = new DelegateCommand(() => executed = true);
            Assert.That(executed, Is.False);
            ((ICommand)command).Execute(null);
            Assert.That(executed, Is.True);
        }

        [Test]
        public void TestParameterizedInheritance()
        {
            var executeParameter = 0;
            var command = new DelegateCommand<int>(i => executeParameter = i);
            Assert.That(command, Is.InstanceOf<ICommand>());
            Assert.That(command, Is.InstanceOf<CommandBase<int>>());
        }

        [Test]
        public void TestParameterizedNoDelegate()
        {
            Assert.That(() => new DelegateCommand<int>(null), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new DelegateCommand<int>(null, i => true), Throws.InstanceOf<ArgumentNullException>());
            
            var executeParameter = 0;
            var command = new DelegateCommand<int>(i => executeParameter = i, null);
            Assert.That(command, Is.Not.Null);
        }

        [Test]
        public void TestParameterizedCanExecute()
        {
            var executeParameter = 0;
            var command = new DelegateCommand<int>(i => executeParameter = i, i => (i % 2) == 0);
            Assert.That(command.CanExecute(4), Is.True);
            Assert.That(command.CanExecute(7), Is.False);
            Assert.That(((ICommand)command).CanExecute(4), Is.True);
            Assert.That(((ICommand)command).CanExecute(7), Is.False);
            Assert.That(executeParameter, Is.EqualTo(0));
        }

        [Test]
        public void TestParameterizedCanExecuteInvalidParameter()
        {
            var executeParameter = 0;
            var command = new DelegateCommand<int>(i => executeParameter = i, i => (i % 2) == 0);
            Assert.That(((ICommand)command).CanExecute(null), Is.False);
            Assert.That(((ICommand)command).CanExecute(1), Is.False);
        }

        [Test]
        public void TestParameterizedCanExecuteNullString()
        {
            var executeParameter = "";
            var command = new DelegateCommand<string>(s => executeParameter = s);
            Assert.That(((ICommand)command).CanExecute(null), Is.True);
        }

        [Test]
        public void TestParameterizedExecute()
        {
            var executeParameter = 0;
            var command = new DelegateCommand<int>(i => executeParameter = i, i => (i % 2) == 0);
            Assert.That(executeParameter, Is.EqualTo(0));
            command.Execute(4);
            Assert.That(executeParameter, Is.EqualTo(4));
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void TestParameterizedExecuteICommand()
        {
            var executeParameter = 0;
            var command = new DelegateCommand<int>(i => executeParameter = i, i => (i % 2) == 0);
            Assert.That(executeParameter, Is.EqualTo(0));
            ((ICommand)command).Execute(4);
            Assert.That(executeParameter, Is.EqualTo(4));
        }
    }
}
