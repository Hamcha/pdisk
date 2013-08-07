using Dokan;
using System.ServiceProcess;
using System;

namespace pdisk
{
	public partial class pdiskService : ServiceBase
	{
		public pdiskService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			
		}

		protected override void OnStop()
		{
		}
	}
}
