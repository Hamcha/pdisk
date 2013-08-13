using System;
using System.Collections.Generic;
using System.IO;
using Dokan;
namespace pdisk
{

	public class Chunk
	{
		public ulong id;
		public string path;
		public ulong chunkSize;
		public Dictionary<string, byte[]> files;
		public ulong rcount;
		public bool InUse { get { return rcount > 0; } }

		private ulong TotalFileSize
		{
			get 
			{
				ulong filesize = 0;
				foreach (KeyValuePair<string,FileMetadata> fdata in FileSystem.chunkMetadata[id].files)
					filesize += (ulong)fdata.Value.fileinfo.Length;
				return filesize;
			}
		}

		public Chunk(ulong _id, string _path, ulong _chunkSize)
		{
			id = _id;
			path = _path;
			chunkSize = _chunkSize;
			files = new Dictionary<string, byte[]>();
		}

		public void Load()
		{
			// Read all bytes from file
			byte[] bytes = File.ReadAllBytes(path);
			// Parse metadata
			foreach (KeyValuePair<string, FileMetadata> fdata in FileSystem.chunkMetadata[id].files)
			{
				// Retrieve data from chunk
				byte[] data = new byte[fdata.Value.fileinfo.Length];
				Buffer.BlockCopy(bytes, (int)fdata.Value.startIndex, data, 0, (int)fdata.Value.fileinfo.Length);
				// Create entry in the file dictionary
				files.Add(fdata.Key, data);
			}
		}

		public void Save()
		{
			// Create byte array to populate
			byte[] bytes = new byte[chunkSize];
			List<FileMetadata> newmeta = new List<FileMetadata>();
			// Populate byte array
			long byteIndex = 0;
			foreach (KeyValuePair<string, byte[]> file in files)
			{
				// Set metadata for retrieval
				FileMetadata tempmeta = FileSystem.chunkMetadata[id].files[file.Key];
				tempmeta.fileinfo.Length = file.Value.LongLength;
				tempmeta.startIndex = byteIndex;
				// Put metadata into struct
				FileSystem.chunkMetadata[id].files[file.Key] = tempmeta;
				// Copy file data into byte array
				file.Value.CopyTo(bytes, byteIndex);
				byteIndex += file.Value.LongLength;
			}
			// Save all bytes to file
			File.WriteAllBytes(path, bytes);
		}

		public bool IsFull() { return TotalFileSize >= chunkSize; }
		public bool IsFull(ulong offset) { return TotalFileSize + offset >= chunkSize; }

		public void MoveFileToChunk(string filename, ref Chunk dstChunk)
		{
			dstChunk.files.Add(filename, files[filename]);
			files.Remove(filename);
		}

		public void Touch(string filename)
		{
			byte[] emptyfile = new byte[]{};
			FileMetadata emptymeta = new FileMetadata
			{
				startIndex = 0,
				fileinfo = new FileInformation
				{
					Attributes = FileAttributes.Normal,
					CreationTime = DateTime.Now,
					FileName = FileSystem.GetFilename(filename),
					LastAccessTime = DateTime.Now,
					LastWriteTime = DateTime.Now,
					Length = 0
				}
			};
			if (files.ContainsKey(filename))
			{
				files[filename] = emptyfile;
				FileSystem.chunkMetadata[id].files[filename] = emptymeta;
			}
			else
			{
				files.Add(filename, emptyfile);
				FileSystem.chunkMetadata[id].files.Add(filename, emptymeta);
			}
		}
	}
}
