// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Rendering;
using System.IO;

namespace scriptwich
{
    public static class FormattingExtensions
    {
        private static readonly TextSpanFormatter _spanFormatter = new TextSpanFormatter();

        internal static FormattableString Red(this string message)
        {
            return $"{ForegroundColorSpan.Red()}{message}{ForegroundColorSpan.Reset()}";
        }

        internal static FormattableString Gray(this string message)
        {
            return $"{ForegroundColorSpan.DarkGray()}{message}{ForegroundColorSpan.Reset()}";
        }

        internal static FormattableString Default(this string message)
        {
            return $"{ForegroundColorSpan.Reset()}{message}{ForegroundColorSpan.Reset()}";
        }

        public static void WriteAnsi(this TextWriter writer, FormattableString formattableString)
        {
            var span = _spanFormatter.ParseToSpan(formattableString);

            writer.Write(span.ToString(OutputMode.Ansi));
        }
    }
}