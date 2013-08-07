using System;
using System.Collections.Generic;
using System.IO;
namespace pdisk
{

	public class Chunk
	{
		public long id;
		public string path;
		public ulong chunkSize;
		public ChunkMetadata metadata;
		public Dictionary<string, PFSFile> files;

		public Chunk(ChunkMetadata _metadata, string _path, ulong _chunkSize)
		{
			metadata = _metadata;
			id = metadata.chunkId;
			path = _path;
			chunkSize = _chunkSize;
			files = new Dictionary<string, PFSFile>();
		}

		public void load()
		{
			// Read all bytes from file
			byte[] bytes = File.ReadAllBytes(path);
			// Parse metadata
			foreach (FileMetadata fdata in metadata.files)
			{
				// Retrieve data from chunk
				byte[] data = new byte[fdata.fileLenght];
				Buffer.BlockCopy(bytes, (int)fdata.startIndex, data, 0, (int)fdata.fileLenght * sizeof(byte));
				PFSFile curFile = new PFSFile
				{
					fileinfo = fdata.fileinfo,
					content = bytes
				};
				// Create entry in the file dictionary
				files.Add(fdata.fileinfo.FileName, curFile);
			}
		}

		public ChunkMetadata save()
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
				tempmeta.fileinfo.FileName = file.Key;
				tempmeta.fileLenght = file.Value.content.LongLength;
				tempmeta.startIndex = byteIndex;
				// Put metadata into list
				newmeta.Add(tempmeta);
				// Copy file data into byte array
				file.Value.content.CopyTo(bytes, byteIndex);
				byteIndex += file.Value.content.LongLength;
			}
			// Save all bytes to file
			File.WriteAllBytes(path, bytes);

			metadata.files = newmeta.ToArray();
			return metadata;
		}
	}
}
