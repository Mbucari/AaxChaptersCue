using System.Collections.Concurrent;
using System.Linq;

namespace AaxChaptersCue
{
	public partial class Form1 : Form
	{
		ConcurrentQueue<string> fileQueue = new();
		CancellationTokenSource? tokenSource;

		private Task? ReadAsinTask;
		public Form1()
		{
			InitializeComponent();
			comboBox1.Items.AddRange(AudibleApi.Localization.Locales.Select(l => l.Name).ToArray());
			comboBox1.SelectedIndex = 0;
			AddListviewInstruction();
		}
		private void AddListviewInstruction()
		{
			var lvi = new ListViewItem("Drag-drop Aax files here. The .cue files will be saved in the Aax file's folder.");
			lvi.Font = new Font(lvi.Font, FontStyle.Italic);
			lvi.ForeColor = Color.Gray;
			listView1.Items.Add(lvi);
		}

		private void listView1_DragEnter(object sender, DragEventArgs e)
		{
			if (e?.Data?.GetDataPresent(DataFormats.FileDrop) is true)
				 e.Effect = DragDropEffects.Copy;
		}

		private void listView1_DragDrop(object sender, DragEventArgs e)
		{
			var data = e?.Data?.GetData(DataFormats.FileDrop);
			if (data is not string[] files) return;
			AddFilesToQueue(files);
		}

		private async Task ReadAsins(CancellationToken cancellationToken)
		{
			while(fileQueue.TryDequeue(out var filePath))
			{
				try
				{
					var file = File.OpenRead(filePath);
					using var aaxFile = new AAXClean.AaxFile(file);
					var asin = aaxFile.AppleTags.Asin;
					var api = new AudibleApi.ApiUnauthenticated(AudibleApi.Localization.Get((string)comboBox1.SelectedItem));
					var meta = await api.GetContentMetadataAsync(asin);
					var chaps = FileLiberator.DownloadDecryptBook.flattenChapters(meta.ChapterInfo.Chapters);

					var chInfo = new AAXClean.ChapterInfo();
					foreach (var c in chaps)
						chInfo.AddChapter(c.Title, TimeSpan.FromMilliseconds(c.LengthMs));

					var cue = AaxDecrypter.Cue.CreateContents(filePath, chInfo);

					var cuePath = Path.ChangeExtension(filePath, ".cue");

					if (cancellationToken.IsCancellationRequested)
						return;

					File.WriteAllText(cuePath, cue);

					var lvi = listIndex(filePath);
					if (lvi is not null)
						lvi.BackColor = Color.LightGreen;
				}
				catch
				{
					var lvi = listIndex(filePath);
					if (lvi is not null)
						lvi.BackColor = Color.LightCoral;
				}
			}
		}

		public void AddFilesToQueue(string[] files)
		{
			var descLvi = listView1.Items.Cast<ListViewItem>().FirstOrDefault(lvi => lvi.Tag is null);
			if (descLvi is not null)
				listView1.Items.Remove(descLvi);

			foreach (var f in files)
			{
				if (!fileQueue.Contains(f) && listIndex(f) is null)
				{
					fileQueue.Enqueue(f);
					var lvi = new ListViewItem(new string[] { Path.GetFileName(f) });
					lvi.Tag = f;
					listView1.Items.Add(lvi);
				}
			}
			if (fileQueue.Count > 0 && (ReadAsinTask is null || ReadAsinTask.IsCompleted))
				ReadAsinTask = ReadAsins((tokenSource = new()).Token);
		}

		private ListViewItem? listIndex(string filePath) => listView1.Items.Cast<ListViewItem>().FirstOrDefault(lvi => lvi.Tag is string s && s == filePath);

		private void button1_Click(object sender, EventArgs e)
		{
			tokenSource?.Cancel();
			fileQueue.Clear();
			listView1.Items.Clear();
			AddListviewInstruction();
		}
	}
}