using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TestHoursWorkedCalculator
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			SharedClasses.AutoUpdating.CheckForUpdates_ExceptionHandler();

			base.OnStartup(e);

			TestHoursWorkedCalculator.HoursWorkedCalculator mw = new HoursWorkedCalculator();
			mw.ShowDialog();
		}
	}
}
