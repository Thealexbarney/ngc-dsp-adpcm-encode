﻿using System.IO;
using System.Text;
using VGAudio.Containers;
using VGAudio.Containers.Dsp;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Dsp : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            var dsp = structure as DspStructure;
            if (dsp == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = dsp.SampleCount,
                SampleRate = dsp.SampleRate,
                ChannelCount = dsp.ChannelCount,
                Format = AudioFormat.GcAdpcm,
                Looping = dsp.Looping,
                LoopStart = dsp.LoopStart,
                LoopEnd = dsp.LoopEnd
            };
        }

        public override object ReadMetadata(Stream stream) => new DspReader().ReadMetadata(stream);

        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            var dsp = structure as DspStructure;
            if (dsp == null) throw new InvalidDataException("Could not parse file metadata.");

            GcAdpcm.PrintAdpcmMetadata(dsp.Channels, builder);
        }
    }
}