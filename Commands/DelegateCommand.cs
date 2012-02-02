using System;

namespace Jamiras.Commands
{
    public class DelegateCommand : CommandBase
    {
        public DelegateCommand(Action executeMethod)
        {
            this.executeMethod = executeMethod;
        }

        private readonly Action executeMethod;

        public override void Execute()
        {
            executeMethod();
        }
    }

    public class DelegateCommand<T> : CommandBase<T>
    {
        public DelegateCommand(Action<T> executeMethod)
        {
            this.executeMethod = executeMethod;
        }

        public DelegateCommand(Action<T> executeMethod, Predicate<T> canExecuteFunction)
        {
            this.executeMethod = executeMethod;
            this.canExecuteFunction = canExecuteFunction;
        }

        private readonly Action<T> executeMethod;
        private readonly Predicate<T> canExecuteFunction;

        public override void Execute(T parameter)
        {
            executeMethod(parameter);
        }

        public override bool CanExecute(T parameter)
        {
            if (this.canExecuteFunction != null)
                return this.canExecuteFunction(parameter);

            return base.CanExecute(parameter);
        }
    }
}
