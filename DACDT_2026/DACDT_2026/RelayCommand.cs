using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DACDT_2026
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, Task> executeAsync;
        private readonly Predicate<object> canExecute;

        public RelayCommand(Action execute)
            : this(_ => execute(), null)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public RelayCommand(Func<Task> executeAsync)
            : this(_ => executeAsync(), null)
        {
        }

        public RelayCommand(Func<object, Task> executeAsync, Predicate<object> canExecute = null)
        {
            this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
            => canExecute == null || canExecute(parameter);

        public async void Execute(object parameter)
        {
            if (executeAsync != null)
            {
                await executeAsync(parameter);
                return;
            }

            execute(parameter);
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
