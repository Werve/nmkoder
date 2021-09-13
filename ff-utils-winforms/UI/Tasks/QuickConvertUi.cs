﻿using Nmkoder.Data;
using Nmkoder.Data.Streams;
using Nmkoder.Extensions;
using Nmkoder.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nmkoder.UI.Tasks
{
    partial class QuickConvertUi : QuickConvert
    {
        private static Form1 form;

        public static void Init()
        {
            form = Program.mainForm;

            foreach (Codecs.VideoCodec c in Enum.GetValues(typeof(Codecs.VideoCodec)))  // Load video codecs
                form.encVidCodecsBox.Items.Add(Codecs.GetCodecInfo(c).FriendlyName);

            ConfigParser.LoadComboxIndex(form.encVidCodecsBox);

            foreach (Codecs.AudioCodec c in Enum.GetValues(typeof(Codecs.AudioCodec)))  // Load audio codecs
                form.encAudEnc.Items.Add(Codecs.GetCodecInfo(c).FriendlyName);

            ConfigParser.LoadComboxIndex(form.encAudEnc);

            foreach (Codecs.SubtitleCodec c in Enum.GetValues(typeof(Codecs.SubtitleCodec)))  // Load audio codecs
                form.encSubEnc.Items.Add(Codecs.GetCodecInfo(c).FriendlyName);

            ConfigParser.LoadComboxIndex(form.encSubEnc);

            foreach (string c in Enum.GetNames(typeof(Containers.Container)))   // Load containers
                form.containerBox.Items.Add(c.ToUpper());

            ConfigParser.LoadComboxIndex(form.containerBox);
        }

        public static void VidEncoderSelected(int index)
        {
            Codecs.VideoCodec c = (Codecs.VideoCodec)index;
            CodecInfo info = Codecs.GetCodecInfo(c);

            bool enableEncOptions = !(c == Codecs.VideoCodec.Copy || c == Codecs.VideoCodec.StripVideo);
            Program.mainForm.encVidQualityBox.Enabled = enableEncOptions;
            Program.mainForm.encVidPresetBox.Enabled = enableEncOptions;
            Program.mainForm.encVidColorsBox.Enabled = enableEncOptions;

            LoadQualityLevel(info);
            LoadPresets(info);
            LoadColorFormats(info);
            ValidateContainer();
        }

        public static void AudEncoderSelected(int index)
        {
            Codecs.AudioCodec c = (Codecs.AudioCodec)index;
            CodecInfo info = Codecs.GetCodecInfo(c);

            Program.mainForm.encAudCh.Enabled = !(c == Codecs.AudioCodec.Copy || c == Codecs.AudioCodec.StripAudio);
            LoadAudBitrate(info);
            ValidateContainer();
        }

        #region Load Video Options

        static void LoadQualityLevel (CodecInfo info)
        {
            if (info.QMax > 0)
                form.encVidQualityBox.Maximum = info.QMax;
            else
                form.encVidQualityBox.Maximum = 100;

            form.encVidQualityBox.Minimum = info.QMin;

            if (info.QDefault >= 0)
                form.encVidQualityBox.Text = info.QDefault.ToString();
            else
                form.encVidQualityBox.Text = "";
        }

        static void LoadPresets(CodecInfo info)
        {
            form.encVidPresetBox.Items.Clear();

            if (info.Presets != null)
                foreach (string p in info.Presets)
                    form.encVidPresetBox.Items.Add(p.ToTitleCase()); // Add every preset to the dropdown

            if (form.encVidPresetBox.Items.Count > 0)
                form.encVidPresetBox.SelectedIndex = info.PresetDef; // Select default preset
        }

        static void LoadColorFormats(CodecInfo info)
        {
            form.encVidColorsBox.Items.Clear();

            if (info.ColorFormats != null)
                foreach (string p in info.ColorFormats)
                    form.encVidColorsBox.Items.Add(p.ToUpper()); // Add every pix_fmt to the dropdown

            if (form.encVidColorsBox.Items.Count > 0)
                form.encVidColorsBox.SelectedIndex = info.ColorFormatDef; // Select default pix_fmt
        }

        #endregion

        #region Load Audio Options

        static void LoadAudBitrate(CodecInfo info)
        {
            if (info.QDefault >= 0)
                form.encAudBr.Text = info.QDefault.ToString();
            else
                form.encAudBr.Text = "";
        }

        #endregion

        public static void ValidateContainer()
        {
            if (form.containerBox.SelectedIndex < 0)
                return;

            Codecs.VideoCodec vCodec = (Codecs.VideoCodec)form.encVidCodecsBox.SelectedIndex;
            Codecs.AudioCodec aCodec = (Codecs.AudioCodec)form.encAudEnc.SelectedIndex;
            Codecs.SubtitleCodec sCodec = (Codecs.SubtitleCodec)form.encSubEnc.SelectedIndex;

            Containers.Container c = (Containers.Container)form.containerBox.SelectedIndex;

            if (!(Containers.ContainerSupports(c, vCodec) && Containers.ContainerSupports(c, aCodec) && Containers.ContainerSupports(c, sCodec)))
            {
                Containers.Container supported = Containers.GetSupportedContainer(vCodec, aCodec, sCodec);

                Logger.Log($"{c.ToString().ToUpper()} doesn't support one of the selected codecs - Auto-selected {supported.ToString().ToUpper()} instead.");

                for (int i = 0; i < form.containerBox.Items.Count; i++)
                    if (form.containerBox.Items[i].ToString().ToUpper() == supported.ToString().ToUpper())
                        form.containerBox.SelectedIndex = i;
            }

            Containers.Container current = (Containers.Container)form.containerBox.SelectedIndex;
            string path = Path.ChangeExtension(form.outputBox.Text.Trim(), current.ToString().ToLower());
            Program.mainForm.outputBox.Text = path;
        }

        public static Codecs.VideoCodec GetCurrentCodecV ()
        {
            return (Codecs.VideoCodec)form.encVidCodecsBox.SelectedIndex;
        }

        public static Codecs.AudioCodec GetCurrentCodecA()
        {
            return (Codecs.AudioCodec)form.encAudEnc.SelectedIndex;
        }

        public static Codecs.SubtitleCodec GetCurrentCodecS()
        {
            return (Codecs.SubtitleCodec)form.encSubEnc.SelectedIndex;
        }

        public static Dictionary<string, string> GetVideoArgsFromUi ()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("q", form.encVidQualityBox.Value.ToString());
            dict.Add("preset", form.encVidPresetBox.Text.ToLower());
            dict.Add("pixFmt", form.encVidColorsBox.Text.ToLower());
            return dict;
        }

        public static Dictionary<string, string> GetAudioArgsFromUi()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("bitrate", form.encAudBr.Text.ToLower());
            dict.Add("ac", form.encAudCh.Text.Split(' ')[0].Trim());
            return dict;
        }

        public static void SetAudioChannelsCombox (int? ch)
        {
            if(ch == null || ch < 1)
            {
                Logger.Log($"SetAudioChannelsCombox: ch is null or < 1 - returning", true);
                form.encAudCh.SelectedIndex = 1;
                return;
            }

            for (int i = 0; i < form.encAudCh.Items.Count; i++)
            {
                if (form.encAudCh.Items[i].ToString().Split(' ').First().GetInt() == ch)
                    form.encAudCh.SelectedIndex = i;
            }
        }

        #region Metadata Tab 

        public static void LoadMetadataGrid()
        {
            if (MediaInfo.current == null || !MediaInfo.streamListLoaded)
                return;

            DataGridView grid = Program.mainForm.metaGrid;
            MediaFile c = MediaInfo.current;

            if (grid.Columns.Count != 3)
            {
                grid.Columns.Clear();
                grid.Columns.Add("1", "Track");
                grid.Columns.Add("2", "Title");
                grid.Columns.Add("3", "Lang");
            }

            grid.Rows.Clear();

            grid.Rows.Add($"File", MediaInfo.current.Title, MediaInfo.current.Language);

            for (int i = 0; i < MediaInfo.current.VideoStreams.Count; i++)
                if(Program.mainForm.streamListBox.GetItemChecked(c.VideoStreams[i].Index))
                    grid.Rows.Add($"Video Track {i + 1}", c.VideoStreams[i].Title, c.VideoStreams[i].Language);

            for (int i = 0; i < MediaInfo.current.AudioStreams.Count; i++)
                if (Program.mainForm.streamListBox.GetItemChecked(MediaInfo.current.AudioStreams[i].Index))
                    grid.Rows.Add($"Audio Track {i + 1}", MediaInfo.current.AudioStreams[i].Title, c.AudioStreams[i].Language);

            for (int i = 0; i < MediaInfo.current.SubtitleStreams.Count; i++)
                if (Program.mainForm.streamListBox.GetItemChecked(MediaInfo.current.SubtitleStreams[i].Index))
                    grid.Rows.Add($"Subtitle Track {i + 1}", MediaInfo.current.SubtitleStreams[i].Title, c.SubtitleStreams[i].Language);

            grid.Columns[0].ReadOnly = true;
            grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[0].FillWeight = 15;
            grid.Columns[1].FillWeight = 75;
            grid.Columns[2].FillWeight = 10;
        }

        public static string GetMetadataArgs ()
        {
            DataGridView grid = Program.mainForm.metaGrid;
            bool map = Program.mainForm.mapMeta.Checked;
            List<string> args = new List<string>();

            foreach (DataGridViewRow row in grid.Rows)
            {
                string track = row.Cells[0].Value?.ToString();
                string title = row.Cells[1].Value?.ToString().Trim();
                string lang = row.Cells[2].Value?.ToString().Trim();

                int streamIdx = track.GetInt() - 1;

                if (!map && string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(lang))
                    continue;

                if(streamIdx < 0)
                {
                    args.Add($"-metadata title=\"{title}\"");
                }
                else
                {
                    if (track.ToLower().Contains("video"))
                        args.Add($"-metadata:s:v:{streamIdx} title=\"{title}\" -metadata:s:s:{streamIdx} language=\"{lang}\"");

                    if (track.ToLower().Contains("audio"))
                        args.Add($"-metadata:s:a:{streamIdx} title=\"{title}\" -metadata:s:s:{streamIdx} language=\"{lang}\"");

                    if (track.ToLower().Contains("subtitle"))
                        args.Add($"-metadata:s:s:{streamIdx} title=\"{title}\" -metadata:s:s:{streamIdx} language=\"{lang}\"");
                }
            }

            return $"-map_metadata {(map ? "0" : "-1")} {string.Join(" ", args)}";
        }

        #endregion
    }
}
