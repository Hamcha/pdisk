using System;
using System.Collections.Generic;
using System.IO;
using Dokan;
namespace pdisk
{

	public class Chunk
	{
		public long id;
		public string path;
		public ulong chunkSize;
		public ChunkMetadata metadata;
		public Dictionary<string, PFSFile> files;

		/* TODO */
		// CURRENTLY OPEN FILE COUNT

		private ulong TotalFileSize
		{
			get 
			{
				ulong filesize = 0;
				foreach (KeyValuePair<string,FileMetadata> fdata in metadata.files)
					filesize += (ulong)fdata.Value.fileinfo.Length;
				return filesize;
			}
		}

		public Chunk(ChunkMetadata _metadata, string _path, ulong _chunkSize)
		{
			metadata = _metadata;
			id = metadata.chunkId;
			path = _path;
			chunkSize = _chunkSize;
			files = new Dictionary<string, PFSFile>();
		}

		public void Load()
		{
			// Read all bytes from file
			byte[] bytes = File.ReadAllBytes(path);
			// Parse metadata
			foreach (KeyValuePair<string,FileMetadata> fdata in metadata.files)
			{
				// Retrieve data from chunk
				byte[] data = new byte[fdata.Value.fileinfo.Length];
				Buffer.BlockCopy(bytes, (int)fdata.Value.startIndex, data, 0, (int)fdata.Value.fileinfo.Length);
				PFSFile curFile = new PFSFile
				{
					fileinfo = fdata.Value.fileinfo,
					content = data
				};
				// Create entry in the file dictionary
				files.Add(fdata.Key, curFile);
			}
		}

		public ChunkMetadata Save()
		{
			// Create byte array to populate
			byte[] bytes = new byte[chunkSize];
			List<FileMetadata> newmeta = new List<FileMetadata>();
			// Populate byte array
			long byteIndex = 0;
			foreach (KeyValuePair<string, PFSFile> file in files)
			{
				// Create metadata for retrieval
				FileMetadata tempmeta = new FileMetadata();
				tempmeta.fileinfo = file.Value.fileinfo;
				tempmeta.fileinfo.Length = file.Value.content.LongLength;
				tempmeta.startIndex = byteIndex;
				// Put metadata into struct
				metadata.files[file.Key] = tempmeta;
				// Copy file data into byte array
				file.Value.content.CopyTo(bytes, byteIndex);
				byteIndex += file.Value.content.LongLength;
			}
			// Save all bytes to file
			File.WriteAllBytes(path, bytes);

			return metadata;
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
			string[] sfnparts = filename.Split('\\');
			string fname = sfnparts[sfnparts.LongLength - 1];
			PFSFile emptyfile = new PFSFile
			{
				content = new byte[]{},
				fileinfo = new FileInformation
				{
					Attributes = FileAttributes.Normal | FileAttributes.NotContentIndexed,
					CreationTime = DateTime.Now,
					FileName = fname,
					LastAccessTime = DateTime.Now,
					LastWriteTime = DateTime.Now,
					Length = 0
				}
			};
			if (files.ContainsKey(filename))
				files[filename] = emptyfile;
			else
				files.Add(filename, emptyfile);
		}
	}
}
