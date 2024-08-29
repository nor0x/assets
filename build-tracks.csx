#r "nuget: YoutubeExplode, 6.4.0"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

var result = new List<Track>();
//clear tracks.json
File.WriteAllText("tracks.json", string.Empty);
await GetTracks();


async Task<List<Track>> GetTracks()
{
	try
	{
		var youtube = new YoutubeClient();
		var playlistUrl = "https://www.youtube.com/playlist?list=PLEelxuGt2Io5jGNnA44S9lRhclhz7po1U";

		// Get all playlist videos
		var videos = await youtube.Playlists.GetVideosAsync(playlistUrl);

		var videosSubset = await youtube.Playlists.GetVideosAsync(playlistUrl).CollectAsync(201);
		//randomize the order of the videos
		videosSubset = videosSubset.OrderBy(_ => Guid.NewGuid()).ToList().ToList();
		var options = new ParallelOptions
		{
			MaxDegreeOfParallelism = 4
		};
		//iterate parallelly through the videos and get the audio stream
		await Parallel.ForEachAsync(videosSubset, options, async (video, token) =>
		{
			//get audio stream
			try
			{
				var streamInfoSet = await youtube.Videos.Streams.GetManifestAsync(video.Id);
				var audioStreamInfo = streamInfoSet.GetAudioStreams().GetWithHighestBitrate();
				if (audioStreamInfo is not null)
				{
					var duration = video.Duration?.TotalSeconds ?? 0;
					var track = new Track(video.Id, video.Title, video.Author.ChannelTitle, (int)duration, audioStreamInfo.Url);
					result.Add(track);
					//write result to file
					File.WriteAllText("tracks.json", string.Empty);
					File.WriteAllText("tracks.json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
				};
				Console.WriteLine($"Added {video.Title}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred while processing video {video.Id} {ex}");
			}
		});


		return result;
	}
	catch (Exception ex)
	{
		Console.WriteLine("An error occurred " + ex);
		return result;
	}
}

public record Track(string Id, string Name, string Artist, int Duration, string Url)
{
	public override string ToString() => $"{Id} - {Name} - {Artist} ({Duration}s) - {Url}";
}
