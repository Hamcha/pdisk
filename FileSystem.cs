using System;
using Dokan;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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

		public ulong chunkSize;
		public ulong maxChunks;

		public string mountPoint;
	}

	public sealed class FileSystem : DokanOperations
	{
		private PFSSettings settings;
		private Dictionary<ulong, Chunk> loadedChunks;
		private Dictionary<ulong, ChunkMetadata> chunkMetadata;
		private Dictionary<string, ulong> files;
		private Dictionary<string, PFSDirectory> directories;

		private FileInformation rootData;
		private Metadata metadata;

		public FileSystem(PFSSettings _settings)
		{
			settings = _settings;
			loadedChunks = new Dictionary<ulong, Chunk>();
			metadata = new Metadata(settings);
			LoadMetadata(Directory.GetFiles(settings.basepath + settings.metafile + "\\"));
			Console.WriteLine("FS Created!");
		}

		private void LoadMetadata(string[] metadataFiles)
		{
			chunkMetadata = new Dictionary<ulong, ChunkMetadata>();
			files = new Dictionary<string,ulong>();
			Console.WriteLine("Loading "+metadataFiles.LongLength+" metadata files...");
			Regex _regex = new Regex(@"(.*)chunk-([0-9]+)\.meta$");
			// Load every file
			foreach (string mdpath in metadataFiles)
			{
				Match match = _regex.Match(mdpath);
				if (!match.Success) continue;
				ulong chunkId;
				if (!ulong.TryParse(match.Groups[2].Value, out chunkId)) continue;
				Console.WriteLine(" - Loading chunk #" + chunkId + " metadata...");
				chunkMetadata[chunkId] = metadata.LoadChunkMetadata(chunkId);
				foreach (KeyValuePair<string,FileMetadata> file in chunkMetadata[chunkId].files)
				{
					files[file.Key] = chunkId;
				}
			}
			// Load directories
			directories = metadata.LoadDirectoryMetadata();
			// Hard fix : Root dir
			// Root data
			rootData = new FileInformation
			{
				Attributes = FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.System,
				CreationTime = DateTime.Today,
				LastAccessTime = DateTime.Today,
				LastWriteTime = DateTime.Today,
				FileName = settings.mountPoint,
				Length = 0
			};
			// Root Dir Entry
			directories["\\"] = new PFSDirectory { name = "\\", info = rootData, innerdirs = null };

			Console.WriteLine(" - Loading directories metadata...");
			Console.WriteLine("Metadata loaded...");
		}

		private string GetPath(string filename)
		{
			string outd = filename.Substring(0,filename.LastIndexOf('\\'));
			return outd != "" ? outd : "\\";
		}

		private PFSDirectory GetDirectory(string fulldir)
		{
			// Hard fix : Root dir
			if (fulldir == "\\") return directories["\\"];
			// Split fulldir into parts
			List<string> dirs = new List<string>(fulldir.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
			string basedir = "\\" + dirs[0];
			// Remove basedir from array (Splicing)
			dirs.RemoveAt(0);
			if (!directories.ContainsKey(basedir)) return null;
			PFSDirectory current = directories[basedir];
			foreach (string dir in dirs)
			{
				if (!current.innerdirs.ContainsKey(dir)) return null;
				current = current.innerdirs[dir];
			}
			return current;
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
			return 0;
		}

		public int CloseFile(string filename, DokanFileInfo info)
		{
			return 0;
		}

		public int CreateDirectory(string filename, DokanFileInfo info)
		{
			Console.WriteLine("[CreateDirectory] called (filename: " + filename + ")");
			return -1;
		}

		public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
		{
			switch (mode)
			{
				case FileMode.Open:
					if (!files.ContainsKey(filename) && GetDirectory(filename) == null) return -DokanNet.ERROR_FILE_NOT_FOUND;
					return 0;
				case FileMode.CreateNew:
					if (files.ContainsKey(filename) || GetDirectory(filename) != null) return -DokanNet.ERROR_ALREADY_EXISTS;
					goto case FileMode.Create;
				case FileMode.Create:
					ulong chunkId = GetFreeChunk(0);
					loadedChunks[chunkId].Touch(filename);
					files[filename] = chunkId;
					return 0;
				case FileMode.OpenOrCreate:
					if (!files.ContainsKey(filename) && GetDirectory(filename) == null) goto case FileMode.Create;
					return 0;
				case FileMode.Truncate:
					if (files.ContainsKey(filename) || GetDirectory(filename) != null) return -DokanNet.ERROR_ALREADY_EXISTS;
					goto case FileMode.Create;
				case FileMode.Append:
					if (!files.ContainsKey(filename) && GetDirectory(filename) == null) goto case FileMode.Create;
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

		public int FindFiles(string filename, System.Collections.ArrayList ofiles, DokanFileInfo info)
		{
			// THIS SHOULD ALWAYS EXISTS -- IF IT DOESN'T.. SCREW YOU WINDOWS!
			PFSDirectory dir = GetDirectory(filename);
			// Still, a little check should be done anyway.
			if (dir == null) return -1;

			// Get file list
			foreach (KeyValuePair<string, ulong> file in files)
			{
				Console.WriteLine(GetPath(file.Key));
				if (GetPath(file.Key) != filename) continue;
				FileInformation fi = chunkMetadata[file.Value].files[file.Key].fileinfo;
				ofiles.Add(fi);
			}

			// Hard Fix : Root dir
			if (filename == "\\")
			{
				foreach (PFSDirectory innerdir in directories.Values)
				{
					if (innerdir.name == "\\") continue;
					ofiles.Add(innerdir.info);
				}
				return 0;
			}

			// Get directory list
			foreach (PFSDirectory innerdir in dir.innerdirs.Values)
			{
				ofiles.Add(innerdir.info);
			}
			return 0;
		}

		public int FlushFileBuffers(string filename, DokanFileInfo info)
		{
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
			// Is it a directory?
			PFSDirectory dir = GetDirectory(filename);
			if (dir != null)
			{
				fileinfo = dir.info;
				return 0;
			}
			// Or is it a file?
			if (files.ContainsKey(filename))
			{
				fileinfo = chunkMetadata[files[filename]].files[filename].fileinfo;
				return 0;
			}
			return -DokanNet.ERROR_FILE_NOT_FOUND;
		}

		public int LockFile(string filename, long offset, long length, DokanFileInfo info)
		{
			return 0;
		}

		public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
		{
			Console.WriteLine("[MoveFile] called (filename: "+filename+")");
			return -1;
		}

		public int OpenDirectory(string filename, DokanFileInfo info)
		{
			if (GetDirectory(filename) != null) return 0;
			return -DokanNet.ERROR_PATH_NOT_FOUND;
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
			return 0;
		}

		public int Unmount(DokanFileInfo info)
		{
			loadedChunks.Clear();
			return 0;
		}

		public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
		{
			Console.WriteLine("[WriteFile] called (filename: "+filename+")");
			return -1;
		}
	}
}
