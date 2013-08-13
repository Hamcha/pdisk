using Dokan;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace pdisk
{
	public struct ChunkMetadata
	{
		public ulong chunkId;
		public Dictionary<string,FileMetadata> files;
		public ulong TotalLength 
		{
			get
			{
				ulong outv = 0;
				foreach (FileMetadata fm in files.Values)
					outv += (ulong)fm.fileinfo.Length;
				return outv;
			}
		}
	}

	public struct FileMetadata
	{
		public long startIndex;
		public FileInformation fileinfo;
	}

	public class PFSDirectory
	{
		public string name;
		public FileInformation info;
		public Dictionary<string, PFSDirectory> innerdirs;
	}

	public struct SavePeriod
	{
		public ulong edits;
		public ulong seconds;
	}

	public class Metadata
	{
		PFSSettings settings;

		public Metadata(PFSSettings _set)
		{
			settings = _set;
		}

		public ChunkMetadata LoadChunkMetadata(ulong chunkId)
		{
			string mdcontent = File.ReadAllText(settings.basepath + settings.metafile + "\\chunk-" + chunkId + ".meta");
			return JsonConvert.DeserializeObject<ChunkMetadata>(mdcontent);
		}

		public void SaveChunkMetadata(ChunkMetadata metadata)
		{
			string content = JsonConvert.SerializeObject(metadata);
			File.WriteAllText(settings.basepath + settings.metafile + "\\chunk-" + metadata.chunkId + ".meta", content);
		}

		public Dictionary<string, PFSDirectory> LoadDirectoryMetadata()
		{
			string mdcontent = File.ReadAllText(settings.basepath + settings.metafile + "\\dirs.meta");
			return JsonConvert.DeserializeObject<Dictionary<string, PFSDirectory>>(mdcontent);
		}

		public void SaveDirectoryMetadata(Dictionary<string, PFSDirectory> dirmetadata)
		{
			string content = JsonConvert.SerializeObject(dirmetadata);
			File.WriteAllText(settings.basepath + settings.metafile + "\\dirs.meta", content);
		}
	}
}
