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

		public Chunk(long chunkID, ulong _chunkSize, string _path, ChunkMetadata _metadata)
		{
			id = chunkID;
			path = _path;
			chunkSize = _chunkSize;
			metadata = _metadata;
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
					data = bytes
				};
				// Create entry in the file dictionary
				files.Add(fdata.filename, curFile);
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
				tempmeta.fileLenght = file.Value.data.LongLength;
				tempmeta.filename = file.Key;
				tempmeta.startIndex = byteIndex;
				// Put metadata into list
				newmeta.Add(tempmeta);
				// Copy file data into byte array
				file.Value.data.CopyTo(bytes, byteIndex);
				byteIndex += file.Value.data.LongLength;
			}
			// Save all bytes to file
			File.WriteAllBytes(path, bytes);

			metadata.files = newmeta.ToArray();
			return metadata;
		}
	}
}
