using System;
using Dokan;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace pdisk
{
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
		public static Dictionary<ulong, Chunk> loadedChunks;
		public static Dictionary<ulong, ChunkMetadata> chunkMetadata;
		public static Dictionary<string, ulong> files;
		public static Dictionary<string, PFSDirectory> directories;

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

		private string GetFilename(string fullpath)
		{
			int start = fullpath.LastIndexOf('\\');
			return fullpath.Substring(start<0?0:start+1);
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

		public Chunk GetChunk(ulong index)
		{
			if (loadedChunks.ContainsKey(index))
				return loadedChunks[index];

			Chunk mychk = new Chunk(index, settings.basepath + settings.chunkpath + "\\"+index.ToString()+".chunk", settings.chunkSize);
			mychk.Load();
			loadedChunks[index] = mychk;
			return mychk;
		}

		public ulong GetFreeChunk(ulong offset)
		{
			// Check all loaded chunks
			foreach (KeyValuePair<ulong,Chunk> chk in loadedChunks)
				if (!chk.Value.IsFull()) return chk.Key;

			// How about unloaded chunks?
			foreach (KeyValuePair<ulong, ChunkMetadata> chkmd in chunkMetadata)
			{
				if (loadedChunks.ContainsKey(chkmd.Key)) continue;
				if (chkmd.Value.TotalLength < settings.chunkSize) return chkmd.Key;
			}

			// Still nothing? New chunk!
			ChunkMetadata nmd = new ChunkMetadata
			{
				chunkId = (ulong)chunkMetadata.Count,
				files = new Dictionary<string, FileMetadata>()
			};

			chunkMetadata.Add((ulong)nmd.chunkId, nmd);

			return (ulong)nmd.chunkId;
		}

		public int Cleanup(string filename, DokanFileInfo info)
		{
			return 0;
		}

		public int CloseFile(string filename, DokanFileInfo info)
		{
			if (files.ContainsKey(filename))
			{
				loadedChunks[files[filename]].rcount -= 1;
			}
			return 0;
		}

		public int CreateDirectory(string filename, DokanFileInfo info)
		{
			if (GetDirectory(filename) == null)
			{
				// Split fulldir into parts
				List<string> dirs = new List<string>(filename.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
				string basedir = "\\" + dirs[0];
				// Remove basedir from array (Splicing)
				dirs.RemoveAt(0);
				if (!directories.ContainsKey(basedir))
				{
					PFSDirectory basedirval = new PFSDirectory
					{
						name = GetFilename(basedir),
						info = new FileInformation
						{
							Attributes = FileAttributes.Directory, FileName = GetFilename(basedir), Length = 0,
							CreationTime = DateTime.Now, LastAccessTime = DateTime.Now,	LastWriteTime = DateTime.Now
						},
						innerdirs = new Dictionary<string, PFSDirectory>()
					};
					directories.Add(basedir, basedirval);
				}
				PFSDirectory current = directories[basedir];
				foreach (string dir in dirs)
				{
					if (!current.innerdirs.ContainsKey(dir))
					{
						PFSDirectory dirval = new PFSDirectory
						{
							name = GetFilename(dir),
							info = new FileInformation
							{
								Attributes = FileAttributes.Directory, FileName = GetFilename(dir), Length = 0,
								CreationTime = DateTime.Now, LastAccessTime = DateTime.Now, LastWriteTime = DateTime.Now
							},
							innerdirs = new Dictionary<string, PFSDirectory>()
						};
						current.innerdirs.Add(dir, dirval);
					}
					current = current.innerdirs[dir];
				}
			}
			return 0;
		}

		public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
		{
			switch (mode)
			{
				case FileMode.Open:
					if (!files.ContainsKey(filename) && GetDirectory(filename) == null) return -DokanNet.ERROR_FILE_NOT_FOUND;
					break;
				case FileMode.CreateNew:
					if (files.ContainsKey(filename) || GetDirectory(filename) != null) return -DokanNet.ERROR_ALREADY_EXISTS;
					goto case FileMode.Create;
				case FileMode.Create:
					ulong chunkId = GetFreeChunk(0);
					loadedChunks[chunkId].Touch(filename);
					files[filename] = chunkId;
					loadedChunks[chunkId].rcount += 1;
					return 0;
				case FileMode.OpenOrCreate:
					if (!files.ContainsKey(filename) && GetDirectory(filename) == null) goto case FileMode.Create;
					break;
				case FileMode.Truncate:
					if (files.ContainsKey(filename) || GetDirectory(filename) != null) return -DokanNet.ERROR_ALREADY_EXISTS;
					goto case FileMode.Create;
				case FileMode.Append:
					if (!files.ContainsKey(filename) && GetDirectory(filename) == null) goto case FileMode.Create;
					break;
				default:
					Console.WriteLine("Error unknown FileMode {0}", mode);
					return -1;
			}
			if (files.ContainsKey(filename))
			{
				ulong chunkId = files[filename];
				GetChunk(chunkId);
				loadedChunks[chunkId].rcount += 1;
			}
			return 0;
		}

		public int DeleteDirectory(string filename, DokanFileInfo info)
		{
			PFSDirectory dir = GetDirectory(filename);
			if (dir == null) return -1;
			Queue<string> todel = new Queue<string>();
			foreach (KeyValuePair<ulong, ChunkMetadata> cmd in chunkMetadata)
			{
				foreach (KeyValuePair<string, FileMetadata> file in cmd.Value.files)
				{
					if (GetPath(file.Key) != filename) continue;
					todel.Enqueue(file.Key);
				}
				while (todel.Count > 0)
				{
					string curItem = todel.Dequeue();
					files.Remove(curItem);
					chunkMetadata[cmd.Key].files.Remove(curItem);
					GetChunk(cmd.Key).files.Remove(filename);
				}
			}
			directories.Remove(filename);
			return 0;
		}

		public int DeleteFile(string filename, DokanFileInfo info)
		{
			Console.WriteLine("Deletefile!");
			if (!files.ContainsKey(filename)) return -1;
			ulong id = files[filename];
			files.Remove(filename);
			chunkMetadata[id].files.Remove(filename);
			GetChunk(id).files.Remove(filename);
			return 0;
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
			else if (files.ContainsKey(filename))
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
			PFSDirectory dir = GetDirectory(filename);
			// Is it a file?
			if (files.ContainsKey(filename))
			{
				ulong id = files[filename];
				// Change File record and metadata
				files.Add(newname, id);
				chunkMetadata[id].files.Add(newname, chunkMetadata[id].files[filename]);
				chunkMetadata[id].files[newname].fileinfo.FileName = GetFilename(newname);
				// Load and change inner Chunk record (if loaded)
				if (loadedChunks.ContainsKey(id))
				{
					loadedChunks[id].files.Add(newname, loadedChunks[id].files[filename]);
					// Remove old file from chunk
					loadedChunks[id].files.Remove(filename);
				}
				// Remove old record from metadata and filelist
				chunkMetadata[id].files.Remove(filename);
				files.Remove(filename);
				return 0;
			}
			// Or is it a directory
			else if (dir != null)
			{
				// Check if it's a root dir
				if (directories.ContainsKey(filename))
				{
					directories.Add(newname, directories[filename]);
					directories[newname].name = directories[newname].info.FileName = GetFilename(newname);
					directories.Remove(filename);
				}
				else
				{
					// Find parent dir
					// Split fulldir into parts
					List<string> dirs = new List<string>(filename.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
					string basedir = "\\" + dirs[0];
					// Remove basedir from array (Splicing)
					dirs.RemoveAt(0);
					PFSDirectory current = directories[basedir];
					string targetName = GetFilename(filename), newfname = GetFilename(newname);
					foreach (string dirp in dirs)
					{
						if (dirp == targetName)
						{
							current.innerdirs.Add(newfname, current.innerdirs[targetName]);
							current.innerdirs.Remove(targetName);
							current = current.innerdirs[newfname];
							current.name = current.info.FileName = newfname;
							break;
						}
						current = current.innerdirs[dirp];
					}
				}
				Queue<Tuple<string, string>> renames = new Queue<Tuple<string, string>>();
				foreach (KeyValuePair<ulong,ChunkMetadata> cmd in chunkMetadata)
				{
					bool isChunkLoaded = loadedChunks.ContainsKey(cmd.Key);
					foreach (KeyValuePair<string,FileMetadata> file in cmd.Value.files)
					{
						if (file.Key.IndexOf(filename+"\\") < 0) continue;
						string newpath = file.Key.Replace(filename, newname);
						renames.Enqueue(new Tuple<string, string>(file.Key, newpath));
					}
					while (renames.Count > 0)
					{
						Tuple<string, string> curItem = renames.Dequeue();
						files.Add(curItem.Item2, files[curItem.Item1]);
						files.Remove(curItem.Item1);
						chunkMetadata[cmd.Key].files.Add(curItem.Item2, chunkMetadata[cmd.Key].files[curItem.Item1]);
						chunkMetadata[cmd.Key].files.Remove(curItem.Item1);
						if (isChunkLoaded)
						{
							loadedChunks[cmd.Key].files.Add(curItem.Item2, loadedChunks[cmd.Key].files[curItem.Item1]);
							loadedChunks[cmd.Key].files.Remove(curItem.Item1);
						}
					}
				}
				return 0;
			}

			return -1;
		}

		public int OpenDirectory(string filename, DokanFileInfo info)
		{
			if (GetDirectory(filename) != null) return 0;
			return -DokanNet.ERROR_PATH_NOT_FOUND;
		}

		public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
		{
			if (!files.ContainsKey(filename)) return -1;
			Chunk c = GetChunk(files[filename]);
			Buffer.BlockCopy(c.files[filename], (int)offset, buffer, 0, c.files[filename].Length - (int)offset);
			readBytes = (uint)c.files[filename].Length;
			return 0;
		}

		public int SetAllocationSize(string filename, long length, DokanFileInfo info)
		{
			return -1;
		}

		public int SetEndOfFile(string filename, long length, DokanFileInfo info)
		{
			if (!files.ContainsKey(filename)) return -1;
			ulong id = files[filename];
			chunkMetadata[id].files[filename].fileinfo.Length = length;
			return 0;
		}

		public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
		{
			if (!files.ContainsKey(filename)) return -1;
			ulong id = files[filename];
			chunkMetadata[id].files[filename].fileinfo.Attributes = attr;
			return 0;
		}

		public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
		{
			if (!files.ContainsKey(filename)) return -1;
			ulong id = files[filename];
			chunkMetadata[id].files[filename].fileinfo.CreationTime = ctime;
			chunkMetadata[id].files[filename].fileinfo.LastAccessTime = atime;
			chunkMetadata[id].files[filename].fileinfo.LastWriteTime = mtime;
			return 0;
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
			if (!files.ContainsKey(filename)) return -1;
			Chunk c = GetChunk(files[filename]);
			if (c.files[filename].Length < buffer.Length + offset)
			{
				byte[] val = (byte[])c.files[filename].Clone();
				c.files[filename] = new byte[buffer.Length + offset];
				Buffer.BlockCopy(val, 0, c.files[filename], 0, val.Length);
			}
			Buffer.BlockCopy(buffer, 0, c.files[filename], (int)offset, buffer.Length);
			writtenBytes = (uint)buffer.Length;
			return 0;
		}
	}
}
