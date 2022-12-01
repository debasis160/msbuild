﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.Logging.FancyLogger
{
    public class FancyLoggerBufferLine
    {
        private static int counter = 0;
        public int Id;
        public string Text;
        public FancyLoggerBufferLine()
        {
            Id = counter++;
            Text = String.Empty;
        }
        public FancyLoggerBufferLine(string text)
        {
            Id = counter++;
            Text = text;
        }
    }
    internal static class FancyLoggerBuffer
    {
        private static List<FancyLoggerBufferLine> lines = new();
        private static int Height {
            get { return Console.BufferHeight; }
        }
        private static int CurrentTopLineIndex = 0;
        public static bool AutoScrollEnabled = true;
        public static void Initialize()
        {
            // Setup event listeners
            var arrowsPressTask = Task.Run(() =>
            {
                while (true)
                {
                    switch (Console.ReadKey().Key)
                    {
                        case ConsoleKey.UpArrow:
                            ScrollUp();
                            break;
                        case ConsoleKey.DownArrow:
                            ScrollDown();
                            break;
                        case ConsoleKey.Spacebar:
                            ToggleAutoScroll();
                            break;
                    }
                }
            });
            // Switch to alternate buffer
            Console.Write(ANSIBuilder.Buffer.UseAlternateBuffer());
            // Render contents
            RenderTitleBar();
            RenderFooter();
            ScrollToEnd();
        }

        #region Rendering and scrolling
        private static void RenderTitleBar()
        {
            Console.Write(""
                + ANSIBuilder.Cursor.Home()
                + ANSIBuilder.Formatting.Inverse("                         MSBuild                         ")
            );
        }
        private static void RenderFooter()
        {
            Console.Write(""
                + ANSIBuilder.Cursor.Position(Height - 2, 0) // Position at bottom
                + "---------------------------------------------------------\n"
                + "Build: 13%"
            );
        }

        private static void ScrollToLine(int firstLineIndex)
        {
            if (firstLineIndex < 0) return;
            if (firstLineIndex >= lines.Count) return;
            CurrentTopLineIndex = firstLineIndex;
            for (int i = 0; i < Height - 4; i++)
            {
                // If line exists
                if (i + firstLineIndex < lines.Count)
                {
                    Console.Write(""
                        + ANSIBuilder.Cursor.Position(i + 2, 0)
                        + ANSIBuilder.Eraser.LineCursorToEnd()
                        + lines[i + firstLineIndex].Text);
                } else
                {
                    Console.Write(""
                        + ANSIBuilder.Cursor.Position(i + 2, 0)
                        + ANSIBuilder.Eraser.LineCursorToEnd()
                    );
                }
            }
            Console.Write(ANSIBuilder.Cursor.Position(Height, 0));
        }

        private static void ScrollToEnd()
        {
            // If number of lines is smaller than height
            if (lines.Count < Height - 2)
            {
                ScrollToLine(0);
            }
            else
            {
                ScrollToLine(lines.Count - Height + 4);
            }
            // Go to end
            Console.Write(ANSIBuilder.Cursor.Position(Height, 0));
        }
        #endregion

        private static void ScrollUp()
        {
            ScrollToLine(CurrentTopLineIndex - 1);
        }

        private static void ScrollDown()
        {
            ScrollToLine(CurrentTopLineIndex + 1);
        }

        private static void ToggleAutoScroll()
        {
            //
            AutoScrollEnabled = !AutoScrollEnabled;
        }

        public static int GetLineIndexById(int lineId)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Id == lineId) return i;
            }
            return -1;
        }
        public static FancyLoggerBufferLine? GetLineById(int lineId)
        {
            int i = GetLineIndexById(lineId);
            if (i == -1) return null;
            return lines[i];
        }

        public static FancyLoggerBufferLine WriteNewLine(string text)
        {
            // Create line
            FancyLoggerBufferLine line = new FancyLoggerBufferLine(text);
            // Add line
            lines.Add(line);
            // Update contents
            if (AutoScrollEnabled) ScrollToEnd();
            return line;
        }

        public static FancyLoggerBufferLine? UpdateLine(int lineId, string text)
        {
            FancyLoggerBufferLine? line = GetLineById(lineId);
            if (line == null) return null;

            line.Text = text;
            ScrollToLine(CurrentTopLineIndex);
            return line;
        }
    }
}
