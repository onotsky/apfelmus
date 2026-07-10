using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Apfelmus.Avalonia.ViewModels
{
    /// <summary>
    /// Einfaches asynchrones ICommand fuer Buttons. Verhindert Mehrfachausfuehrung waehrend
    /// eine laufende Aktion noch nicht abgeschlossen ist.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isRunning;

        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                _isRunning = true;
                RaiseCanExecuteChanged();
                await _execute();
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
