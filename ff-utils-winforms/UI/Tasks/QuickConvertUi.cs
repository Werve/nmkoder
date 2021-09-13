﻿using Nmkoder.Data;
using Nmkoder.Extensions;
using Nmkoder.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            LoadQualityLevel(info);
            LoadPresets(info);
            LoadColorFormats(info);
            ValidateContainer();
        }

        public static void AudEncoderSelected(int index)
        {
            Codecs.AudioCodec c = (Codecs.AudioCodec)index;
            CodecInfo info = Codecs.GetCodecInfo(c);

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

                Logger.Log($"{c} doesn't support one of the selected codecs - Auto-selected {supported} instead.");

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
    }
}
