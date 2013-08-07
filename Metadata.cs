using Dokan;
using Newtonsoft.Json;
using System.IO;
namespace pdisk
{
	public struct ChunkMetadata
	{
		public long chunkId;
		public FileMetadata[] files;
	}

	public struct FileMetadata
	{
		public string filename;
		public long startIndex;
		public long fileLenght;
		public FileInformation fileinfo;
	}

	public class Metadata
	{
		PFSSettings settings;

		public Metadata(PFSSettings _set)
		{
			settings = _set;
		}

		public ChunkMetadata loadChunkMetadata(int chunkId)
		{
			string mdcontent = File.ReadAllText(settings.basepath + settings.metafile + "\\" + chunkId + ".mdt");
			return JsonConvert.DeserializeObject<ChunkMetadata>(mdcontent);
		}

		public void saveChunkMetadata(ChunkMetadata metadata)
		{
			string content = JsonConvert.SerializeObject(metadata);
			File.WriteAllText(settings.basepath + settings.metafile + "\\" + metadata.chunkId + ".mdt", content);
		}
	}
}
