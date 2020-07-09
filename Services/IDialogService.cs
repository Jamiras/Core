﻿using System;
using System.Windows;
using Jamiras.ViewModels;

namespace Jamiras.Services
{
    /// <summary>
    /// Service for showing dialog windows.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Gets or sets the main window of the application.
        /// </summary>
        Window MainWindow { get; set; }

        /// <summary>
        /// Gets or sets the default title to display in windows.
        /// </summary>
        string DefaultWindowTitle { get; set; }

        /// <summary>
        /// Registers a callback that creates the View for a ViewModel.
        /// </summary>
        /// <param name="viewModelType">Type of ViewModel to create View for (must inherit from DialogViewModelBase)</param>
        /// <param name="createViewDelegate">Delegate that returns a View instance.</param>
        void RegisterDialogHandler(Type viewModelType, Func<DialogViewModelBase, FrameworkElement> createViewDelegate);

        /// <summary>
        /// Gets whether or not a handler is registered for the provided type.
        /// </summary>
        /// <param name="viewModelType">Type of ViewModel to query for (must inherit from DialogViewModelBase)</param>
        /// <returns><c>true</c> if a handler is registered, <c>false</c> if not.</returns>
        bool HasDialogHandler(Type viewModelType);

        /// <summary>
        /// Shows the dialog for the provided ViewModel.
        /// </summary>
        /// <param name="viewModel">ViewModel to show dialog for.</param>
        /// <returns>How the dialog was dismissed.</returns>
        DialogResult ShowDialog(DialogViewModelBase viewModel);

        /// <summary>
        /// Gets the <see cref="Window"/> object for a topmost <see cref="DialogViewModelBase"/> being shown.
        /// </summary>
        /// <returns>Window for topmost ViewModel, <c>null</c> if no dialogs are open.</returns>
        Window GetTopMostDialog();
    }
}
