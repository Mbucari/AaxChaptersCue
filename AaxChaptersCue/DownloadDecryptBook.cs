using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AaxDecrypter;
using AudibleApi;
using Dinah.Core;
using Dinah.Core.ErrorHandling;

namespace FileLiberator
{
	public class DownloadDecryptBook
	{

		/*

		Flatten Audible's new hierarchical chapters, combining children into parents.

		Audible may deliver chapters like this:

		00:00 - 00:10	Opening Credits
		00:10 - 00:12	Book 1
		00:12 - 00:14	|	Part 1
		00:14 - 01:40	|	|	Chapter 1
		01:40 - 03:20	|	|	Chapter 2
		03:20 - 03:22	|	Part 2
		03:22 - 05:00	|	|	Chapter 3
		05:00 - 06:40	|	|	Chapter 4
		06:40 - 06:42	Book 2
		06:42 - 06:44	|	Part 3
		06:44 - 08:20	|	|	Chapter 5
		08:20 - 10:00	|	|	Chapter 6
		10:00 - 10:02	|	Part 4
		10:02 - 11:40	|	|	Chapter 7
		11:40 - 13:20	|	|	Chapter 8
		13:20 - 13:30	End Credits

		And flattenChapters will combine them into this:

		00:00 - 00:10	Opening Credits
		00:10 - 01:40	Book 1: Part 1: Chapter 1
		01:40 - 03:20	Book 1: Part 1: Chapter 2
		03:20 - 05:00	Book 1: Part 2: Chapter 3
		05:00 - 06:40	Book 1: Part 2: Chapter 4
		06:40 - 08:20	Book 2: Part 3: Chapter 5
		08:20 - 10:00	Book 2: Part 3: Chapter 6
		10:00 - 11:40	Book 2: Part 4: Chapter 7
		11:40 - 13:20	Book 2: Part 4: Chapter 8
		13:20 - 13:40	End Credits

		However, if one of the parent chapters is longer than 10000 milliseconds, it's kept as its own
		chapter. A duration longer than a few seconds implies that the chapter contains more than just
		the narrator saying the chapter title, so it should probably be preserved as a separate chapter.
		Using the example above, if "Book 1" was 15 seconds long and "Part 3" was 20 seconds long:

		00:00 - 00:10	Opening Credits
		00:10 - 00:25	Book 1
		00:25 - 00:27	|	Part 1
		00:27 - 01:40	|	|	Chapter 1
		01:40 - 03:20	|	|	Chapter 2
		03:20 - 03:22	|	Part 2
		03:22 - 05:00	|	|	Chapter 3
		05:00 - 06:40	|	|	Chapter 4
		06:40 - 06:42	Book 2
		06:42 - 07:02	|	Part 3
		07:02 - 08:20	|	|	Chapter 5
		08:20 - 10:00	|	|	Chapter 6
		10:00 - 10:02	|	Part 4
		10:02 - 11:40	|	|	Chapter 7
		11:40 - 13:20	|	|	Chapter 8
		13:20 - 13:30	End Credits

		then flattenChapters will combine them into this:

		00:00 - 00:10	Opening Credits
		00:10 - 00:25	Book 1
		00:25 - 01:40	Book 1: Part 1: Chapter 1
		01:40 - 03:20	Book 1: Part 1: Chapter 2
		03:20 - 05:00	Book 1: Part 2: Chapter 3
		05:00 - 06:40	Book 1: Part 2: Chapter 4
		06:40 - 07:02	Book 2: Part 3
		07:02 - 08:20	Book 2: Part 3: Chapter 5
		08:20 - 10:00	Book 2: Part 3: Chapter 6
		10:00 - 11:40	Book 2: Part 4: Chapter 7
		11:40 - 13:20	Book 2: Part 4: Chapter 8
		13:20 - 13:40	End Credits

		*/

		public static List<AudibleApi.Common.Chapter> flattenChapters(IList<AudibleApi.Common.Chapter> chapters, string titleConcat = ": ")
		{
			List<AudibleApi.Common.Chapter> chaps = new();

			foreach (var c in chapters)
			{
				if (c.Chapters is not null)
				{
					if (c.LengthMs < 10000)
					{
						c.Chapters[0].StartOffsetMs = c.StartOffsetMs;
						c.Chapters[0].StartOffsetSec = c.StartOffsetSec;
						c.Chapters[0].LengthMs += c.LengthMs;
					}
					else
						chaps.Add(c);

					var children = flattenChapters(c.Chapters);

					foreach (var child in children)
						child.Title = $"{c.Title}{titleConcat}{child.Title}";

					chaps.AddRange(children);
					c.Chapters = null;
				}
				else
					chaps.Add(c);
			}
			return chaps;
		}

		public static void combineCredits(IList<AudibleApi.Common.Chapter> chapters)
		{
			if (chapters.Count > 1 && chapters[0].Title == "Opening Credits")
			{
				chapters[1].StartOffsetMs = chapters[0].StartOffsetMs;
				chapters[1].StartOffsetSec = chapters[0].StartOffsetSec;
				chapters[1].LengthMs += chapters[0].LengthMs;
				chapters.RemoveAt(0);
			}
			if (chapters.Count > 1 && chapters[^1].Title == "End Credits")
			{
				chapters[^2].LengthMs += chapters[^1].LengthMs;
				chapters.Remove(chapters[^1]);
			}
		}
	}
}
