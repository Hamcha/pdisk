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

		public string mountPoint;
	}

	public sealed class FileSystem : DokanOperations
	{
		private PFSSettings settings;
		private Dictionary<ulong, Chunk> loadedChunks;
		private DB leveldb;
		private FileInformation rootData;

		public FileSystem(PFSSettings _settings)
		{
			settings = _settings;
			loadedChunks = new Dictionary<ulong, Chunk>();
			leveldb = new DB(settings.ldbsettings, settings.basepath + settings.ldbpath);
			Console.WriteLine("FS Created!");
			rootData = new FileInformation
			{
				Attributes = FileAttributes.Directory | FileAttributes.NotContentIndexed,
				CreationTime = DateTime.Today,
				FileName = settings.mountPoint,
				LastAccessTime = DateTime.Today,
				LastWriteTime = DateTime.Today,
				Length = 0
			};
		}

		public ulong GetFreeChunk(ulong offset)
		{
			// Check loaded chunks
			// Check LevelDB for unloaded chunks
			// If all chunks are full, create a new one
			return 0;
		}

		public int Cleanup(string filename, DokanFileInfo info)
		{
			Console.WriteLine("[Cleanup] called (filename: " + filename + ")");
			return 0;
		}

		public int CloseFile(string filename, DokanFileInfo info)
		{
			Console.WriteLine("[CloseFile] called (filename: " + filename + ")");
			return 0;
		}

		public int CreateDirectory(string filename, DokanFileInfo info)
		{

			return -1;
		}

		public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
		{
			string res;
			switch (mode)
			{
				case FileMode.Open:
					res = leveldb.Get("file:" + filename);
					if (string.IsNullOrEmpty(res)) return -DokanNet.ERROR_FILE_NOT_FOUND;
					return 0;
				case FileMode.CreateNew:
					res = leveldb.Get("file:" + filename);
					if (!string.IsNullOrEmpty(res)) return -DokanNet.ERROR_ALREADY_EXISTS;
					goto case FileMode.Create;
				case FileMode.Create:
					ulong chunkId = GetFreeChunk(0);
					loadedChunks[chunkId].Touch(filename);
					leveldb.Put("file:" + filename, chunkId.ToString());
					return 0;
				case FileMode.OpenOrCreate:
					res = leveldb.Get("file:" + filename);
					if (string.IsNullOrEmpty(res)) goto case FileMode.Create;
					return 0;
				case FileMode.Truncate:
					res = leveldb.Get("file:" + filename);
					if (string.IsNullOrEmpty(res)) return -DokanNet.ERROR_ALREADY_EXISTS;
					goto case FileMode.Create;
				case FileMode.Append:
					res = leveldb.Get("file:" + filename);
					if (string.IsNullOrEmpty(res)) goto case FileMode.Create;
					return 0;
				default:
					Console.WriteLine("Error unknown FileMode {0}", mode);
					return -1;
			}
		}

		public int DeleteDirectory(string filename, DokanFileInfo info)
		{
			Console.WriteLine("[DeleteDirectory] called (filename: " + filename + ")");
			return -1;
		}

		public int DeleteFile(string filename, DokanFileInfo info)
		{
			Console.WriteLine("[DeleteFile] called (filename: " + filename + ")");
			return -1;
		}

		public int FindFiles(string filename, System.Collections.ArrayList files, DokanFileInfo info)
		{
			Console.WriteLine("[FindFiles] called (filename: " + filename + ")");
			return 0;
		}

		public int FlushFileBuffers(string filename, DokanFileInfo info)
		{
			Console.WriteLine("[FlushFileBuffers] called (filename: " + filename + ")");
			return 0;
		}

		public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
		{
			Console.WriteLine("[GetDiskFreeSpace] called");
			ulong usedBytes = settings.chunkSize * (ulong)loadedChunks.Count;
			totalBytes = settings.chunkSize * settings.maxChunks;
			freeBytesAvailable = totalBytes - usedBytes;
			totalFreeBytes = totalBytes - usedBytes;
			return 0;
		}

		public int GetFileInformation(string filename, ref FileInformation fileinfo, DokanFileInfo info)
		{
			Console.WriteLine("[GetFileInformation] called (filename: " + filename + ")");
			// Root check
			if (filename == "\\")
			{
				fileinfo = rootData;
				return 0;
			}
			return -DokanNet.ERROR_FILE_NOT_FOUND;
		}

		public int LockFile(string filename, long offset, long length, DokanFileInfo info)
		{
			Console.WriteLine("[LockFile] called (filename: "+filename+")");
			return 0;
		}

		public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
		{
			Console.WriteLine("[MoveFile] called (filename: "+filename+")");
			return -1;
		}

		public int OpenDirectory(string filename, DokanFileInfo info)
		{
			Console.WriteLine("[OpenDirectory] called (filename: "+filename+")");
			return 0;
		}

		public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
		{
			Console.WriteLine("[ReadFile] called (filename: "+filename+")");
			return -1;
		}

		public int SetAllocationSize(string filename, long length, DokanFileInfo info)
		{
			Console.WriteLine("[SetAllocationSize] called (filename: "+filename+")");
			return -1;
		}

		public int SetEndOfFile(string filename, long length, DokanFileInfo info)
		{
			Console.WriteLine("[SetEndOfFile] called (filename: "+filename+")");
			return -1;
		}

		public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
		{
			Console.WriteLine("[SetFileAttributes] called (filename: "+filename+")");
			return -1;
		}

		public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
		{
			Console.WriteLine("[SetFileTime] called (filename: "+filename+")");
			return -1;
		}

		public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
		{
			Console.WriteLine("[UnlockFile] called (filename: "+filename+")");
			return 0;
		}

		public int Unmount(DokanFileInfo info)
		{
			Console.WriteLine("[Unmount] called");
			return 0;
		}

		public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
		{
			Console.WriteLine("[WriteFile] called (filename: "+filename+")");
			return -1;
		}
	}
}
