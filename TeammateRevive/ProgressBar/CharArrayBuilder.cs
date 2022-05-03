using System;
using System.Linq;

namespace TeammateRevive.ProgressBar
{
    /// <summary>
    /// Represents abstraction over character buffer. Used to avoid unnecessary string allocations when mutating contents.
    /// </summary>
    public class CharArrayBuilder
    {
        private struct Part
        {
            public int Start;
            public int Len;

            public Part(int start, int len)
            {
                this.Start = start;
                this.Len = len;
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
            this.Length = 0;
            foreach (var s in strs)
            {
                this.Length += s.Length;
            }

            // ensure array sizes
            if (this.Buffer.Length < this.Length) this.Buffer = new char[this.Length + 7];
            if (this.parts.Length < strs.Length) this.parts = new Part[strs.Length];

            // init buffer and parts arrays
            var idx = 0;
            for (var i = 0; i < strs.Length; i++)
            {
                var s = strs[i];
                s.CopyTo(0, this.Buffer, idx, s.Length);
                this.parts[i] = new Part(idx, s.Length);
                idx += s.Length;
            }
        }

        public void UpdatePart(int idx, string value)
        {
            var part = this.parts[idx];
            UpdatePartLen(ref part, value.Length, idx);
            value.CopyTo(0, this.Buffer, part.Start, value.Length);
        }

        private void UpdatePartLen(ref Part part, int len, int partIdx)
        {
            var diff = part.Len - len;

            // part have correct size, no action needed
            if (diff == 0) return;

            // expand buffer if needed
            var newLen = this.Length - diff;
            if (diff < 0 && this.Buffer.Length < newLen)
            {
                var newBuffer = new char[newLen];
                Array.Copy(this.Buffer, newBuffer, this.Length);
                this.Buffer = newBuffer;
            }

            // update buffer
            Array.Copy(this.Buffer,
                part.Start + part.Len,
                this.Buffer,
                part.Start + part.Len - diff,
                this.Length - part.Start - part.Len
            );

            // update lengths
            part.Len = len;
            this.Length -= diff;

            // update consequent parts
            for (var i = partIdx + 1; i < this.parts.Length; i++)
            {
                ref var p = ref this.parts[i];
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
            ref var part = ref this.parts[partIdx];
            UpdatePartLen(ref part, DIGITS_COUNT + 1, partIdx);
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
            return new string(this.Buffer, 0, Length);
        }

        public string TraceParts()
        {
            return string.Join("", this.parts.Select((p, i) => $"[{i}>" + new string(this.Buffer, p.Start, p.Len)));
        }
    }
}