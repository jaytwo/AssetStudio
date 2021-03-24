using AssetStudio.Mxr.Fields;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    static class MxrAnimation
    {
        public static string Read(ObjectReader objectReader, int frameCount)
        {
            var field = AnimationField.FrameBits;
            var counts = new List<int>();
            var destination = new StringBuilder();
            var frame = 0;

            ReadBitArray(objectReader, destination, field, counts, frameCount);

            var sections = new List<AnimationField>
            {
                AnimationField.Positions,
                AnimationField.Rotations,
                AnimationField.Scales,
                AnimationField.PositionBits,
                AnimationField.RotationBits,
                AnimationField.ScaleBits
            };

            while (objectReader.BaseStream.Position < objectReader.BaseStream.Length - 1)
            {
                var frameRepeats = (int)objectReader.ReadByte();
                if (frameRepeats == 255)
                    break;

                if (frameRepeats == (int)sections[0] || (sections[0] == AnimationField.PositionBits && frameRepeats == 34))
                {
                    var nextByte = objectReader.ReadByte();
                    objectReader.BaseStream.Position--;

                    if (field == AnimationField.FrameBits || frame + frameRepeats > frameCount)
                    {
                        field = (AnimationField)frameRepeats;

                        while (frameRepeats != (int)sections[0])
                            sections.RemoveAt(0);

                        sections.RemoveAt(0);
                        counts.Add(0);
                        frame = 0;

                        if (field >= AnimationField.Positions)
                        {
                            destination.AppendLine(field.ToString());
                            frameRepeats = objectReader.ReadByte();
                        }
                    }
                }

                if (field == AnimationField.PositionBits)
                {
                    counts.RemoveAt(counts.Count - 1);

                    ReadBitArray(objectReader, destination, field, counts, counts[1]);
                    field = (AnimationField)objectReader.ReadByte();
                    if (field != AnimationField.RotationBits)
                        throw new InvalidDataException();
                }

                if (field == AnimationField.RotationBits)
                {
                    ReadBitArray(objectReader, destination, field, counts, counts[2]);
                    field = (AnimationField)objectReader.ReadByte();
                    if (field == AnimationField.End)
                        break;
                    if (field != AnimationField.ScaleBits)
                        throw new InvalidDataException();

                    ReadBitArray(objectReader, destination, field, counts, counts[3]);
                    field = (AnimationField)objectReader.ReadByte();
                    if (field != AnimationField.End)
                        throw new InvalidDataException();

                    break;
                }

                if (frameRepeats == 128)
                    frameRepeats = objectReader.ReadInt32();

                var length = objectReader.ReadByte();
                var content = Enumerable.Range(0, length * 3)
                    .Select(i => objectReader.ReadSingle())
                    .ToArray();

                destination.AppendLine($"    Frames {frame}-{frame + frameRepeats}: " + string.Join(", ", content));
                counts[counts.Count - 1] += length;
                frame += frameRepeats + length;
            }

            return string.Join(" + ", counts) + Environment.NewLine + destination.ToString();
        }

        private static void ReadBitArray(ObjectReader objectReader, StringBuilder destination, AnimationField field, List<int> counts, int count)
        {
            var bits = new BitArray(objectReader.ReadBytes((int)Math.Ceiling(count / 8.0)));
            bits.Length = count;
            counts.Add(count);

            destination.AppendLine(field.ToString());
            destination.AppendLine($"    [{bits.Length}]: {string.Join(string.Empty, bits.Cast<bool>().Select(b => b ? "1" : "0"))}");
        }
    }
}