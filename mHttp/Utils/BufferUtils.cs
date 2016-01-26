using System;
using System.Text;

namespace m.Utils
{
    static class BufferUtils
    {
        const byte CR = 13;
        const byte LF = 10;
        const byte SP = 32;

        public static int FindFirstCRLF(this byte[] buffer, int start, int end)
        {
            for (int i=start; i<end-1; i++)
            {
                if (buffer[i] == CR && buffer[i + 1] == LF)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindFirstCR(this byte[] buffer, int start, int end)
        {
            for (int i=start; i<end; i++)
            {
                if (buffer[i] == CR)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindFirstLF(this byte[] buffer, int start, int end)
        {
            for (int i=start; i<end; i++)
            {
                if (buffer[i] == LF)
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool TryExactMatch(this byte[] buffer,
                                         ref int start,
                                         int end,
                                         byte[] sequence)
        {
            if (start + sequence.Length > end)
            {
                return false;
            }

            for (int i=0; i<sequence.Length; i++)
            {
                if (buffer[start + i] != sequence[i])
                {
                    return false;
                }
            }

            start = start + sequence.Length;
            return true;
        }

        public static bool TryExactMatches(this byte[] buffer,
                                           ref int start,
                                           int end,
                                           byte[][] sequences,
                                           out int matchedIndex)
        {
            for (int i=0; i<sequences.Length; i++)
            {
                if (TryExactMatch(buffer, ref start, end, sequences[i]))
                {
                    matchedIndex = i;
                    return true;
                }
            }

            matchedIndex = -1;
            return false;
        }

        public static bool TryMatchSpaces(this byte[] buffer, ref int start, int end)
        {
            var initial = start;

            while (start < end && buffer[start] == SP)
            {
                start++;
            }

            return start > initial;
        }

        public static bool TryMatchUntil(this byte[] buffer,
                                         ref int start,
                                         int end,
                                         byte value,
                                         out string matched)
        {
            int i = start;

            while (i < end)
            {
                if (buffer[i] == value)
                {
                    matched = Encoding.ASCII.GetString(buffer, start, i - start);
                    start = i;
                    return true;
                }
                i++;
            }

            matched = null;
            return false;
        }

        public static bool TryMatchUntilAnyOf(this byte[] buffer,
                                              ref int start,
                                              int end,
                                              byte[] values,
                                              out string matched)
        {
            int i = start;

            while (i < end)
            {
                if (values.Contains(buffer[i]))
                {
                    matched = Encoding.ASCII.GetString(buffer, start, i - start);
                    start = i;
                    return true;
                }
                i++;
            }

            matched = null;
            return false;
        }

        public static bool TryMatch(this byte[] buffer,
                                    ref int start,
                                    byte value)
        {
            if (buffer[start] == value)
            {
                start++;
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool Contains(this byte[] values, byte v)
        {
            for (int i=0; i<values.Length; i++)
            {
                if (values[i] == v)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryMatchMany(this byte[] buffer,
                                        ref int start,
                                        int end,
                                        byte[] values,
                                        out string matched)
        {
            int i = start;
            while (i < end && values.Contains(buffer[i]))
            {
                i++;
            }

            if (i > start)
            {
                matched = Encoding.ASCII.GetString(buffer, start, i-start);
                start = i;
                return true;
            }
            else
            {
                matched = null;
                return false;
            }
        }

        public static void Compact(this byte[] buffer, ref int dataStart, ref int offset)
        {
            if (dataStart == offset)
            {
                dataStart = 0;
                offset = 0;
            }
            else
            {
                var available = offset - dataStart;
                for (int i=0; i<available; i++)
                {
                    buffer[i] = buffer[dataStart + i];
                }

                dataStart = 0;
                offset = available;
            }
        }
        
        public static void Expand(ref byte[] buffer, int space)
        {
            var newBuffer = new byte[buffer.Length + space];
            Array.Copy(buffer, newBuffer, buffer.Length);
            buffer = newBuffer;
        }
    }
}
