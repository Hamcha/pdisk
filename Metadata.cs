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
		public FileInformation fileinfo;
	}

	public class Metadata
	{
		PFSSettings settings;

		public Metadata(PFSSettings _set)
		{
			settings = _set;
		}

		public ChunkMetadata LoadChunkMetadata(int chunkId)
		{
			string mdcontent = File.ReadAllText(settings.basepath + settings.metafile + "\\" + chunkId + ".meta");
			return JsonConvert.DeserializeObject<ChunkMetadata>(mdcontent);
		}

		public void SaveChunkMetadata(ChunkMetadata metadata)
		{
			string content = JsonConvert.SerializeObject(metadata);
			File.WriteAllText(settings.basepath + settings.metafile + "\\" + metadata.chunkId + ".meta", content);
		}
	}
}
