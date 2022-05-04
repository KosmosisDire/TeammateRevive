using System;
using System.Linq;

namespace TeammateRevive.ProgressBar
{
    /// <summary>
    /// Represents abstraction over character buffer. Used to avoid unnecessary string allocations when mutating contents.
    /// </summary>
    public class CharArrayBuilder
    {
        private class Part
        {
            public int Start;
            public int Len;

            public Part(int start, int len)
            {
                Start = start;
                Len = len;
            }
        }

        private Part[] parts = Array.Empty<Part>();

        public char[] Buffer { get; private set; } = Array.Empty<char>();
        public int Length { get; set; }

        private const char PADDING = '​';

        private const int FRACTION_LEN = 1;
        private const int WHOLE_NUMBER_LEN = 3;
        private const int DIGITS_COUNT = WHOLE_NUMBER_LEN + FRACTION_LEN;
        private const int DIGITS_MULT = 1000; // 10^(DIGITS_COUNT - 1)

        public CharArrayBuilder(params string[] strs)
        {
            InitParts(strs);
        }

        public void InitParts(params string[] strs)
        {
            // init buffer length
            Length = 0;
            foreach (var s in strs)
            {
                Length += s.Length;
            }

            // ensure array sizes
            if (Buffer.Length < Length) Buffer = new char[Length + 7];
            if (parts.Length < strs.Length) parts = new Part[strs.Length];

            // init buffer and parts arrays
            var idx = 0;
            for (var i = 0; i < strs.Length; i++)
            {
                var s = strs[i];
                s.CopyTo(0, Buffer, idx, s.Length);
                parts[i] = new Part(idx, s.Length);
                idx += s.Length;
            }
        }

        public void UpdatePart(int idx, string value)
        {
            var part = parts[idx];
            UpdatePartLen(part, value.Length, idx);
            value.CopyTo(0, Buffer, part.Start, value.Length);
        }

        private void UpdatePartLen(Part part, int len, int partIdx)
        {
            var diff = part.Len - len;

            // part have correct size, no action needed
            if (diff == 0) return;

            // expand buffer if needed
            var newLen = Length - diff;
            if (diff < 0 && Buffer.Length < newLen)
            {
                var newBuffer = new char[newLen];
                Array.Copy(Buffer, newBuffer, Length);
                Buffer = newBuffer;
            }

            // update buffer
            Array.Copy(Buffer,
                part.Start + part.Len,
                Buffer,
                part.Start + part.Len - diff,
                Length - part.Start - part.Len
            );

            // update lengths
            part.Len = len;
            Length -= diff;

            // update consequent parts
            for (var i = partIdx + 1; i < parts.Length; i++)
            {
                var p = parts[i];
                p.Start -= diff;
            }
        }

        /// <summary>
        /// Formats float as percentage. Avoids shifting memory by padding value with zero-width whitespaces
        /// </summary>
        /// <param name="partIdx"></param>
        /// <param name="value"></param>
        public void SetPaddedPercentagePart(int partIdx, float value)
        {
            InternalSetPaddedFloatPart(partIdx, (int)(value * DIGITS_MULT));
        }

        private void InternalSetPaddedFloatPart(int partIdx, int value)
        {
            if (value > DIGITS_MULT) value = DIGITS_MULT;
            var part = parts[partIdx];
            UpdatePartLen(part, DIGITS_COUNT + 1, partIdx);
            var idx = part.Start;

            // pad buffer
            for (var i = 0; i < DIGITS_COUNT - 2; i++)
            {
                Buffer[idx + i] = PADDING;
            }

            var del1 = 10;
            var del2 = 1;
            var dot = 0;
            var r = false;

            for (var i = 0; i < DIGITS_COUNT; i++)
            {
                var d = value % del1;
                if (d == value) r = true;
                d /= del2;
                del1 *= 10;
                del2 *= 10;

                if (i == FRACTION_LEN)
                {
                    dot = 1;
                    Buffer[idx + DIGITS_COUNT - i] = '.';
                }

                Buffer[idx + DIGITS_COUNT - i - dot] = (char)(48 + d);
                if (r && dot == 1) return;
            }
        }

        public override string ToString()
        {
            return new string(Buffer, 0, Length);
        }

        public string TraceParts()
        {
            return string.Join("", parts.Select((p, i) => $"[{i}>" + new string(Buffer, p.Start, p.Len)));
        }
    }
}