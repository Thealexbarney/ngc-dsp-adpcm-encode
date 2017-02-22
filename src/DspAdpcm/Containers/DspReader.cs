﻿using System.IO;
using DspAdpcm.Containers.Dsp;
using DspAdpcm.Formats;
using DspAdpcm.Formats.GcAdpcm;
using DspAdpcm.Utilities;
using static DspAdpcm.Formats.GcAdpcm.GcAdpcmHelpers;
using static DspAdpcm.Utilities.Helpers;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace DspAdpcm.Containers
{
    public class DspReader : AudioReader<DspReader, DspStructure>
    {
        private static int HeaderSize => 0x60;

        protected override DspStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                var structure = new DspStructure();

                ParseHeader(reader, structure);

                if (readAudioData)
                {
                    reader.BaseStream.Position = HeaderSize * structure.ChannelCount;
                    ParseData(reader, structure);
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(DspStructure structure)
        {
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < structure.ChannelCount; c++)
            {
                var channel = new GcAdpcmChannel(structure.SampleCount, structure.AudioData[c])
                {
                    Coefs = structure.Channels[c].Coefs,
                    Gain = structure.Channels[c].Gain,
                    Hist1 = structure.Channels[c].Hist1,
                    Hist2 = structure.Channels[c].Hist2,
                };
                channel.SetLoopContext(structure.LoopStart, structure.Channels[c].LoopPredScale,
                    structure.Channels[c].LoopHist1, structure.Channels[c].LoopHist2);

                channels[c] = channel;
            }

            var adpcm = new GcAdpcmFormat(structure.SampleCount, structure.SampleRate, channels);

            if (structure.Looping)
            {
                adpcm.SetLoop(structure.LoopStart, structure.LoopEnd);
            }

            return adpcm;
        }

        private static void ParseHeader(BinaryReader reader, DspStructure structure)
        {
            structure.SampleCount = reader.ReadInt32();
            structure.NibbleCount = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.Looping = reader.ReadInt16() == 1;
            structure.Format = reader.ReadInt16();
            structure.StartAddress = reader.ReadInt32();
            structure.EndAddress = reader.ReadInt32();
            structure.CurrentAddress = reader.ReadInt32();

            reader.BaseStream.Position = 0x4a;
            structure.ChannelCount = reader.ReadInt16();
            structure.FramesPerInterleave = reader.ReadInt16();
            structure.ChannelCount = structure.ChannelCount == 0 ? 1 : structure.ChannelCount;

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                reader.BaseStream.Position = HeaderSize * i + 0x1c;
                var channel = new GcAdpcmChannelInfo
                {
                    Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                    Gain = reader.ReadInt16(),
                    PredScale = reader.ReadInt16(),
                    Hist1 = reader.ReadInt16(),
                    Hist2 = reader.ReadInt16(),
                    LoopPredScale = reader.ReadInt16(),
                    LoopHist1 = reader.ReadInt16(),
                    LoopHist2 = reader.ReadInt16()
                };

                structure.Channels.Add(channel);
            }

            if (reader.BaseStream.Length < HeaderSize + SampleCountToByteCount(structure.SampleCount))
            {
                throw new InvalidDataException($"File doesn't contain enough data for {structure.SampleCount} samples");
            }

            if (SampleToNibble(structure.SampleCount) != structure.NibbleCount)
            {
                throw new InvalidDataException("Sample count and nibble count do not match");
            }

            if (structure.Format != 0)
            {
                throw new InvalidDataException($"File does not contain ADPCM audio. Specified format is {structure.Format}");
            }
        }

        private static void ParseData(BinaryReader reader, DspStructure structure)
        {
            if (structure.ChannelCount == 1)
            {
                structure.AudioData = new[] { reader.ReadBytes(SampleCountToByteCount(structure.SampleCount)) };
            }
            else
            {
                int dataLength = GetNextMultiple(SampleCountToByteCount(structure.SampleCount), 8) * structure.ChannelCount;
                int interleaveSize = structure.FramesPerInterleave * BytesPerFrame;
                structure.AudioData = reader.BaseStream.DeInterleave(dataLength, interleaveSize, structure.ChannelCount, SampleCountToByteCount(structure.SampleCount));
            }
        }
    }
}