using System;
using System.Windows.Input;

namespace SimpleAdsClient
{
	public class RelayCommand : ICommand
	{
		private readonly Action<object> executeAction;
		private readonly Func<object, bool> canExecuteFunction;

		private bool canExecute = false;

		public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
		{
			executeAction = execute;
			canExecuteFunction = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			bool newCanExecute = (canExecuteFunction != null) ? canExecuteFunction(parameter) : true;

			if (newCanExecute != canExecute)
			{
				canExecute = newCanExecute;

				try
				{
					return canExecute;
				}
				finally
				{
					CanExecuteChanged?.Invoke(this, new EventArgs());
				}
			}
			else
				return canExecute;
		}

		public void Execute(object parameter)
		{
			executeAction(parameter);
		}

		public void UpdateCanExecute()
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}

		public event EventHandler CanExecuteChanged;
	}
}
