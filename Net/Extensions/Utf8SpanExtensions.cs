using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Utf8;

namespace Net.Extensions
{
    public static class Utf8SpanExtensions
    {
        public static int Length(this Utf8Span value) => value.Bytes.Length;

        public static Utf8String[] Split(this Utf8Span value, char seprator)
        {
            //TODO: THIS IS VERY OPTIMZIED INDEED

            string stringValue = (string)value;

            List<Utf8String> split = new List<Utf8String>();
            foreach (string result in stringValue.Split(seprator))
            {
                split.Add((Utf8String)result);
            }

            return split.ToArray();
        }

        public static Utf8String[] Split(this Utf8Span value, char seprator, int count)
        {
            //TODO: THIS IS VERY OPTIMZIED INDEED

            string stringValue = (string)value;

            List<Utf8String> split = new List<Utf8String>();
            foreach (string result in stringValue.Split(seprator, count))
            {
                split.Add((Utf8String)result);
            }

            return split.ToArray();
        }
    }
}
