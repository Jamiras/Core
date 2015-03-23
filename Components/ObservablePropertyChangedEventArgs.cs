using System.ComponentModel;

namespace Jamiras.Components
{
    public class ObservablePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public ObservablePropertyChangedEventArgs(string propertyName, object newValue, object oldValue)
            : base(propertyName)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }

        public object NewValue { get; private set; }

        public object OldValue { get; private set; }
    }
}
