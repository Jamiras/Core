using System.Windows;
using System.Windows.Controls;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for OkCancelView.xaml
    /// </summary>
    public partial class OkCancelView : ContentControl
    {
        /// <summary>
        /// Constructs a new <see cref="OkCancelView"/>.
        /// </summary>
        public OkCancelView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructs a new <see cref="OkCancelView"/> with the provided content.
        /// </summary>
        /// <param name="content">Content to display in the <see cref="OkCancelView"/></param>
        public OkCancelView(FrameworkElement content)
            : this()
        {
            Content = content;
        }
    }
}
