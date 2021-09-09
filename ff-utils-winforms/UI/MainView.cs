﻿using Nmkoder.Data;
using Nmkoder.Data.Streams;
using Nmkoder.Extensions;
using Nmkoder.IO;
using Nmkoder.Media;
using Nmkoder.Properties;
using Nmkoder.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Stream = Nmkoder.Data.Streams.Stream;

namespace Nmkoder.UI
{
    class MainView
    {
        public static MediaFile current; 

        public static async Task HandleFiles (string[] paths)
        {
            RemoveThumbs();

            if (paths.Length == 1)
            {
                await LoadFileInfo(paths[0]);
            }
            else
            {
                // many
            }
        }

        public static async Task LoadFileInfo (string path)
        {
            MediaFile mediaFile = new MediaFile(path);
            int streamCount = await FfmpegUtils.GetStreamCount(path);
            Logger.Log($"Loading info for {streamCount} streams.");
            await mediaFile.Initialize();
            Logger.Log($"Loaded all media info.");
            current = mediaFile;

            Task.Run(() => SaveThumbnails(current.File.FullName));

            string getTitle = await GetVideoInfo.GetFfprobeInfoAsync(path, GetVideoInfo.FfprobeMode.ShowFormat, "TAG:title");
            string titleStr = getTitle.Trim().Length > 2 ? $"Title: {getTitle.Trunc(25)} - " : "";
            string br = current.TotalKbits > 0 ? $" - Total Bitrate: {FormatUtils.Bitrate(current.TotalKbits)}" : "";
            string dur = FormatUtils.MsToTimestamp(FfmpegCommands.GetDurationMs(path));
            Program.mainForm.formatInfoLabel.Text = $"{titleStr}Format: {current.Ext.ToUpper()} - Streams: {current.StreamCount} - Duration: {dur}{br}";

            CheckedListBox box = Program.mainForm.streamListBox;
            box.Items.Clear();

            for (int i = 0; i < current.AllStreams.Count; i++)
            {
                foreach(Stream s in current.AllStreams) // This is somewhat unnecessary but ensures that the order of the list matches the stream index.
                {
                    if(s.Index == i)
                    {
                        string codec = FormatUtils.CapsIfShort(s.Codec, 5);
                        const int maxChars = 50;

                        if (s.Type == Stream.StreamType.Video)
                        {
                            VideoStream vs = (VideoStream)s;
                            string codecStr = vs.Kbits > 0 ? $"{codec} at {FormatUtils.Bitrate(vs.Kbits)}" : codec;
                            box.Items.Add($"#{i}: {s.Type} ({codecStr}) - {vs.Resolution.Width}x{vs.Resolution.Height} - {vs.Rate.GetString()} FPS");
                        }

                        if (s.Type == Stream.StreamType.Audio)
                        {
                            AudioStream @as = (AudioStream)s;
                            string title = string.IsNullOrWhiteSpace(@as.Title.Trim()) ? " " : $" - {@as.Title.Trunc(maxChars)} ";
                            string codecStr = @as.Kbits > 0 ? $"{codec} at {FormatUtils.Bitrate(@as.Kbits)}" : codec;
                            box.Items.Add($"#{i}: {s.Type} ({codecStr}){title}- {@as.Layout.ToTitleCase()}");
                        }

                        if (s.Type == Stream.StreamType.Subtitle)
                        {
                            SubtitleStream ss = (SubtitleStream)s;
                            string lang = string.IsNullOrWhiteSpace(ss.Language.Trim()) ? " " : $" - {FormatUtils.CapsIfShort(ss.Language, 4).Trunc(maxChars)} ";
                            string ttl = string.IsNullOrWhiteSpace(ss.Title.Trim()) ? " " : $" - {FormatUtils.CapsIfShort(ss.Title, 4).Trunc(maxChars)} ";
                            box.Items.Add($"#{i}: {s.Type} ({codec}){lang}{ttl}");
                        }

                        Program.mainForm.streamListBox.SetItemChecked(i, true);
                    }
                }
            }
        }

        public static void RemoveThumbs ()
        {
            Program.mainForm.thumbnailBox.Image = Resources.loadingThumbsText;
            IoUtils.DeleteContentsOfDir(Paths.GetThumbsPath());
        }

        public static async Task SaveThumbnails(string path)
        {
            Directory.CreateDirectory(Paths.GetThumbsPath());
            int randThumbs = 4;

            try
            {
                if (!IoUtils.IsPathDirectory(path))     // If path is video - Extract frames
                {
                    string imgPath = Path.Combine(Paths.GetThumbsPath(), "thumb0.jpg");
                    await FfmpegExtract.ExtractSingleFrame(path, imgPath, 1, 360);

                    await FfmpegExtract.ExtractThumbs(path, Paths.GetThumbsPath(), randThumbs * 2);
                    FileInfo[] thumbs = IoUtils.GetFileInfosSorted(Paths.GetThumbsPath(), false, "*.*");

                    var smallerHalf = thumbs.Skip(1).OrderBy(f => f.Length).Take(randThumbs).ToList(); // Get smaller half of thumbs

                    foreach (FileInfo f in smallerHalf) // Delete smaller thumbs to only have high-information thumbs
                        f.Delete();
                }
                else     // Path is frame folder - Copy frames
                {
                    FileInfo[] frames = IoUtils.GetFileInfosSorted(path, false, "*.*");
                    Image img1 = IoUtils.GetImage(frames[0].FullName);
                    img1.Save(Path.Combine(Paths.GetThumbsPath(), $"thumb0.jpg"), ImageFormat.Jpeg);
                    Random rnd = new Random();
                    List<FileInfo> picks = frames.Skip(1).OrderBy(x => rnd.Next()).Take(randThumbs * 2).ToList();
                    Logger.Log(string.Join(", ", picks.Select(x => (x.Length / 1024).ToString())));
                    picks = picks.OrderBy(f => f.Length).Take(randThumbs).ToList(); // Delete smaller half of thumbs
                    Logger.Log(string.Join(", ", picks.Select(x => (x.Length / 1024).ToString())));

                    int idx = 1;

                    foreach(FileInfo pick in picks)
                    {
                        Logger.Log($"Saving thumb " + pick.Name);
                        IoUtils.GetImage(pick.FullName).Save(Path.Combine(Paths.GetThumbsPath(), $"thumb{idx}.jpg"), ImageFormat.Jpeg);
                        idx++;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("GetThumbnails Error: " + e.Message, true);
            }

            await Slideshow.RunFromPath(Program.mainForm.thumbnailBox, Paths.GetThumbsPath());
        }

        public static string GetStreamDetails(int index)
        {
            if (index < 0)
                return "";

            Stream stream = current.AllStreams[index];
            List<string> lines = new List<string>();
            lines.Add($"Codec: {stream.CodecLong}");

            if(stream.Type == Stream.StreamType.Video)
            {
                VideoStream v = (VideoStream)stream;
                lines.Add($"Resolution and Aspect Ratio: {v.Resolution.ToStringShort()} - SAR {v.Sar.ToStringShort(":")} - DAR {v.Dar.ToStringShort(":")}");
                lines.Add($"Color Space: {v.ColorSpace}{(v.ColorSpace.ToLower().Contains("p10") ? " (10-bit)" : " (8-bit)")}");
                lines.Add($"Frame Rate: {v.Rate} (~{v.Rate.GetString()} FPS)");
                lines.Add($"Language: {((v.Language.Trim().Length > 1) ? $"{FormatUtils.CapsIfShort(v.Language, 4)}" : "Unknown")}");
            }

            if (stream.Type == Stream.StreamType.Audio)
            {
                AudioStream a = (AudioStream)stream;
                lines.Add($"Title: {((a.Title.Trim().Length > 1) ? a.Title : "None")}");
                lines.Add($"Sample Rate: {((a.SampleRate > 1) ? $"{a.SampleRate} KHz" : "None")}");
                lines.Add($"Channels: {((a.Channels > 0) ? $"{a.Channels}" : "Unknown")} {(a.Layout.Trim().Length > 1 ? $"as {a.Layout.ToTitleCase()})" : "")}");
                lines.Add($"Language: {((a.Language.Trim().Length > 1) ? $"{FormatUtils.CapsIfShort(a.Language, 4)}" : "Unknown")}");
            }

            if (stream.Type == Stream.StreamType.Subtitle)
            {
                SubtitleStream s = (SubtitleStream)stream;
                lines.Add($"Title: {((s.Title.Trim().Length > 1) ? s.Title : "None")}");
                lines.Add($"Language: {((s.Language.Trim().Length > 1) ? $"{FormatUtils.CapsIfShort(s.Language, 4)}" : "Unknown")}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
