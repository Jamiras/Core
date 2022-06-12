﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Defines a column for the <see cref="GridViewModel"/>.
    /// </summary>
    [DebuggerDisplay("{Header}")]
    public abstract class GridColumnDefinition : ModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridColumnDefinition"/> class.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="sourceProperty">The property bound to the column.</param>
        protected GridColumnDefinition(string header, ModelProperty sourceProperty)
        {
            Header = header;
            SourceProperty = sourceProperty;
        }

        /// <summary>
        /// Gets the property bound to the column
        /// </summary>
        public ModelProperty SourceProperty { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Header"/>
        /// </summary>
        public static readonly ModelProperty HeaderProperty =
            ModelProperty.Register(typeof(GridColumnDefinition), "Header", typeof(string), null);

        /// <summary>
        /// Gets or sets the text to display in the column header.
        /// </summary>
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="FooterText"/>
        /// </summary>
        public static readonly ModelProperty FooterTextProperty =
            ModelProperty.Register(typeof(GridColumnDefinition), "FooterText", typeof(string), null);

        /// <summary>
        /// Gets or sets the text to display in the column footer.
        /// </summary>
        public string FooterText
        {
            get { return (string)GetValue(FooterTextProperty); }
            set { SetValue(FooterTextProperty, value); }
        }

        internal static readonly ModelProperty SummarizeFunctionProperty =
            ModelProperty.Register(typeof(GridColumnDefinition), null, typeof(Func<IEnumerable, string>), null);

        /// <summary>
        /// Gets or sets the summarize function.
        /// </summary>
        protected Func<IEnumerable, string> SummarizeFunction
        {
            get { return (Func<IEnumerable, string>)GetValue(SummarizeFunctionProperty); }
            set { SetValue(SummarizeFunctionProperty, value); }
        }

        internal bool HasSummarizeFunction
        {
            get { return (SummarizeFunction != null); }
        }

        internal void Summarize(IEnumerable<GridRowViewModel> rows)
        {
            var summarizeFunction = SummarizeFunction;
            if (summarizeFunction != null)
            {
                var values = new List<object>();
                foreach (var row in rows)
                    values.Add(row.GetValue(SourceProperty));

                FooterText = summarizeFunction(values);
            }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsReadOnly"/>
        /// </summary>
        public static readonly ModelProperty IsReadOnlyProperty =
            ModelProperty.Register(typeof(GridColumnDefinition), "IsReadOnly", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether this column is read only. (default is false)
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Width"/>
        /// </summary>
        public static readonly ModelProperty WidthProperty =
            ModelProperty.Register(typeof(GridColumnDefinition), "Width", typeof(int), 0);

        /// <summary>
        /// Gets or sets the width of the column if <see cref="WidthType"/> is Fixed.
        /// </summary>
        public int Width
        {
            get { return (int)GetValue(WidthProperty); }
            set 
            { 
                SetValue(WidthProperty, value);
                WidthType = GridColumnWidthType.Fixed;
            }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="WidthType"/>
        /// </summary>
        public static readonly ModelProperty WidthTypeProperty =
            ModelProperty.Register(typeof(GridColumnDefinition), "WidthType", typeof(GridColumnWidthType), GridColumnWidthType.Auto);

        /// <summary>
        /// Gets or sets whether the column should fill the remaining space of the grid.
        /// </summary>
        public GridColumnWidthType WidthType
        {
            get { return (GridColumnWidthType)GetValue(WidthTypeProperty); }
            set { SetValue(WidthTypeProperty, value); }
        }

        internal FieldViewModelBase CreateFieldViewModelInternal(GridRowViewModel row)
        {
            return CreateFieldViewModel(row);
        }

        /// <summary>
        /// Creates the FieldViewModel responsible for rendering this column and binds it to the provided row.
        /// </summary>
        protected abstract FieldViewModelBase CreateFieldViewModel(GridRowViewModel row);
    }
}
