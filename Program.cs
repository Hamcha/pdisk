using System;
using Dokan;
using Newtonsoft.Json;
using System.IO;

namespace pdisk
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			/*
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] 
			{ 
				new pdiskService() 
			};
			ServiceBase.Run(ServicesToRun);
			 * */
			// PFS Settings
			/* PFSSettings settings = new PFSSettings()
			{
				basepath = "C:\\psf\\",
				chunkSize = 16 * 1024 * 1024, // 16 Megabytes
				maxChunks = 4 * 1024,		  // 2^12 chunks
				metafile = "metadata",
				chunkpath = "chunks",
				mountPoint = "Q:\\"
			};*/
			string settingfilecontent = File.ReadAllText("settings.conf");
			PFSSettings settings = JsonConvert.DeserializeObject<PFSSettings>(settingfilecontent);
			FileSystem pfs = new FileSystem(settings);

			// Dokan VFS Options
			DokanOptions opt = new DokanOptions()
			{
				DebugMode = true,
				VolumeLabel = "Persistence",
				MountPoint = settings.mountPoint,
				ThreadCount = 1
			};
			int status = DokanNet.DokanMain(opt, pfs);
			switch (status)
			{
				case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
					Console.WriteLine("Drive letter error");
					break;
				case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
					Console.WriteLine("Driver install error");
					break;
				case DokanNet.DOKAN_MOUNT_ERROR:
					Console.WriteLine("Mount error");
					break;
				case DokanNet.DOKAN_START_ERROR:
					Console.WriteLine("Start error");
					break;
				case DokanNet.DOKAN_ERROR:
					Console.WriteLine("Unknown error");
					break;
				case DokanNet.DOKAN_SUCCESS:
					Console.WriteLine("Success");
					break;
				default:
					Console.WriteLine("Unknown status: %d", status);
					break;
			}
			while (true) ;
		}
	}
}
