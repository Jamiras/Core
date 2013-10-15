using System.ComponentModel;
using Jamiras.Components;

namespace Jamiras.Core.Tests.Components
{
    internal class TestPropertyChangedObject : PropertyChangedObject
    {
        public int ClassicProperty
        {
            get { return _intValue; }
            set
            {
                if (_intValue != value)
                {
                    _intValue = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ClassicProperty"));
                }
            }
        }
        private int _intValue;

        public string NewProperty
        {
            get { return _stringValue; }
            set
            {
                if (_stringValue != value)
                {
                    _stringValue = value;
                    OnPropertyChanged(() => NewProperty);
                }
            }
        }
        private string _stringValue;
    }
}
