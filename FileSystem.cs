using System;
using Dokan;
using System.Collections.Generic;
using LevelDB;
using System.IO;

namespace pdisk
{
	public struct PFSFile
	{
		public FileInformation fileinfo;
		public byte[] content;
	}

	public struct PFSSettings
	{
		public string basepath;
		public string metafile;
		public string chunkpath;

		public Options ldbsettings;
		public string  ldbpath;

		public ulong chunkSize;
		public ulong maxChunks;
	}

	public sealed class FileSystem : DokanOperations
	{
		private PFSSettings settings;
		private Dictionary<int, Chunk> loadedChunks;
		private DB leveldb;

		public FileSystem(PFSSettings _settings)
		{
			settings = _settings;
			loadedChunks = new Dictionary<int, Chunk>();
			leveldb = new DB(settings.ldbsettings, settings.basepath + settings.ldbpath);
			Console.WriteLine("FS Created!");
		}

		public int Cleanup(string filename, DokanFileInfo info)
		{
			return 0;
		}

		public int CloseFile(string filename, DokanFileInfo info)
		{
			return 0;
		}

		public int CreateDirectory(string filename, DokanFileInfo info)
		{
			return -1;
		}

		public int CreateFile(string filename, System.IO.FileAccess access, System.IO.FileShare share, System.IO.FileMode mode, System.IO.FileOptions options, DokanFileInfo info)
		{
			return 0;
		}

		public int DeleteDirectory(string filename, DokanFileInfo info)
		{
			return -1;
		}

		public int DeleteFile(string filename, DokanFileInfo info)
		{
			return -1;
		}

		public int FindFiles(string filename, System.Collections.ArrayList files, DokanFileInfo info)
		{
			return 0;
		}

		public int FlushFileBuffers(string filename, DokanFileInfo info)
		{
			return 0;
		}

		public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
		{
			ulong usedBytes = settings.chunkSize * (ulong)loadedChunks.Count;
			totalBytes = settings.chunkSize * settings.maxChunks;
			freeBytesAvailable = totalBytes - usedBytes;
			totalFreeBytes = totalBytes - usedBytes;
			return 0;
		}

		public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
		{
			return 0;
		}

		public int LockFile(string filename, long offset, long length, DokanFileInfo info)
		{
			return 0;
		}

		public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
		{
			return -1;
		}

		public int OpenDirectory(string filename, DokanFileInfo info)
		{
			return 0;
		}

		public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
		{
			return -1;
		}

		public int SetAllocationSize(string filename, long length, DokanFileInfo info)
		{
			return -1;
		}

		public int SetEndOfFile(string filename, long length, DokanFileInfo info)
		{
			return -1;
		}

		public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
		{
			return -1;
		}

		public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
		{
			return -1;
		}

		public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
		{
			return 0;
		}

		public int Unmount(DokanFileInfo info)
		{
			return 0;
		}

		public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
		{
			return -1;
		}
	}
}
