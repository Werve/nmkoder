﻿using Nmkoder.Data.Codecs;
using Nmkoder.Extensions;
using Nmkoder.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nmkoder.Data
{
    class CodecUtils
    {
        //public enum CodecType { Video, AnimImage, Image, Audio }

        public enum Av1anCodec { AomAv1, SvtAv1, VpxVp9, X265 };
        public enum VideoCodec { CopyVideo, StripVideo, H264, H265, H264Nvenc, H265Nvenc, Vp9, Av1, Gif, Png, Jpg };
        public enum AudioCodec { CopyAudio, StripAudio, Aac, Opus, Mp3, Flac };
        public enum SubtitleCodec { CopySubs, StripSubs, MovText, Srt, WebVtt };

        public static IEncoder GetCodec(VideoCodec c)
        {
            if (c == VideoCodec.StripVideo) return new StripVideo();
            if (c == VideoCodec.CopyVideo) return new CopyVideo();
            if (c == VideoCodec.H264) return new H264();
            if (c == VideoCodec.H264Nvenc) return new H264Nvenc();
            if (c == VideoCodec.H265) return new H265();
            if (c == VideoCodec.H265Nvenc) return new H265Nvenc();
            if (c == VideoCodec.Vp9) return new Vp9();
            if (c == VideoCodec.Av1) return new Av1();
            if (c == VideoCodec.Gif) return new Gif();
            if (c == VideoCodec.Png) return new Png();
            if (c == VideoCodec.Jpg) return new Jpg();
            return null;
        }

        public static IEncoder GetCodec(Av1anCodec c)
        {
            if (c == Av1anCodec.AomAv1) return new AomAv1();
            if (c == Av1anCodec.SvtAv1) return new SvtAv1();
            if (c == Av1anCodec.VpxVp9) return new VpxVp9();
            if (c == Av1anCodec.X265) return new X265();
            return null;
        }

        public static IEncoder GetCodec(AudioCodec c)
        {
            if (c == AudioCodec.StripAudio) return new StripAudio();
            if (c == AudioCodec.CopyAudio) return new CopyAudio();
            if (c == AudioCodec.Aac) return new Aac();
            if (c == AudioCodec.Opus) return new Opus();
            if (c == AudioCodec.Mp3) return new Mp3();
            if (c == AudioCodec.Flac) return new Flac();
            return null;
        }

        public static IEncoder GetCodec(SubtitleCodec c)
        {
            if (c == SubtitleCodec.StripSubs) return new StripSubs();
            if (c == SubtitleCodec.CopySubs) return new CopySubs();
            if (c == SubtitleCodec.MovText) return new MovText();
            if (c == SubtitleCodec.Srt) return new Srt();
            if (c == SubtitleCodec.WebVtt) return new WebVtt();
            return null;
        }

        public static string GetKeyIntArg(MediaFile mediaFile, int intervalSeconds, string arg = "-g ")
        {
            if (mediaFile == null || mediaFile.VideoStreams.Count < 1)
                return "";

            int keyInt = ((float)(mediaFile?.VideoStreams.FirstOrDefault().Rate.GetFloat() * intervalSeconds)).RoundToInt();
            return keyInt >= 24 ? $"{arg}{keyInt}" : "";
        }
    }
}
