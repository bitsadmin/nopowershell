#if DLLBUILD
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell
{
    partial class Program
    {
        [DllExport("main", CallingConvention = CallingConvention.StdCall)]
        public static void Main()
        {
#if DEBUG
            ProgramDll nps = new ProgramDll();
            nps.NoPowerShellExecute();
#else
            try
            {
                ProgramDll nps = new ProgramDll();
                nps.NoPowerShellExecute();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "NoPowerShell", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif
        }

        public static void DllMain(string[] args)
        {
            Main(args);
        }
    }

    class ProgramDll
    {
        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();
        private const int STD_OUTPUT_HANDLE = -11;
        private const int MY_CODE_PAGE = 437;

        public void NoPowerShellExecute()
        {
            // Prepare console
            AllocConsole();
            IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
            FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
            Encoding encoding = Encoding.GetEncoding(MY_CODE_PAGE);
            StreamWriter standardOutput = new StreamWriter(fileStream, encoding) { AutoFlush = true };
            Console.SetOut(standardOutput);
            Console.Title = string.Format("NoPowerShell DLL v{0}", Program.VERSION);
            Console.TreatControlCAsInput = true;

            // Set colors
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            // Title
            Console.WriteLine("NoPowerShell DLL v{0} by Arris Huijgen (@bitsadmin)\r\nType 'help' to list all supported cmdlets.\r\n", Program.VERSION);

            // Main loop
            List<string> history = new List<string>();
            while (true)
            {
                Console.Write("NPS> ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                string line = ReadLine(history);
                Console.ForegroundColor = ConsoleColor.White;

                if (line == null)
                {
                    ConsoleColor previousColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("^C");
                    Console.ForegroundColor = previousColor;
                    continue;
                }
                else if (line == string.Empty)
                    continue;

                switch (line.ToLowerInvariant())
                {
                    case "exit":
                        return;
                    case "cls":
                    case "clear":
                        Console.Clear();
                        break;
                    case "history":
                        Console.WriteLine("  Id CommandLine");
                        Console.WriteLine("  -- -----------");
                        int i = 1;
                        foreach (string hline in history)
                        {
                            Console.WriteLine("{0} {1}", i.ToString().PadLeft(4), hline);
                            i++;
                        }
                        Console.WriteLine();
                        break;
                    default:
                        Program.DllMain(ParseArguments(line));
                        Console.WriteLine();
                        break;
                }

                history.Add(line);
            }
        }

        private static string[] ParseArguments(string args)
        {
            List<string> arguments = new List<string>();

            int i = 0;
            int start = 0;
            int length = 0;
            do
            {
                char c = args[i];

                switch (c)
                {
                    case '"':
                    case '\'':
                        int found = args.IndexOf(c, i + 1);
                        arguments.Add(args.Substring(start + 1, found - start - 1));
                        i = start = found + 2;
                        break;
                    case ' ':
                    case '\t':
                        if (length > 0)
                            arguments.Add(args.Substring(start, length));
                        i = start = i + 1;
                        length = 0;
                        break;
                    default:
                        i++;
                        length++;

                        // End of string
                        if (i == args.Length)
                            arguments.Add(args.Substring(start, length));
                        break;
                }
            }
            while (i < args.Length);

            return arguments.ToArray();
        }

        // Collect console input and handle special keys
        // Based on: https://github.com/PowerShell/PowerShell/blob/master/src/Microsoft.PowerShell.ConsoleHost/host/msh/ConsoleHostUserInterface.cs
        // TODO: Add Ctrl + Left/Right Arrows and Backspace
        private static string ReadLine(List<string> history)
        {
            ConsoleKeyInfo keyInfo;
            string s = string.Empty;
            int index = 0;
            int cursorLeft = Console.CursorLeft;
            int cursorCurrent = cursorLeft;
            int cursorTop = Console.CursorTop;
            bool insertMode = true;
            Console.TreatControlCAsInput = true;

            int historyIndex = history.Count;

            do
            {
                keyInfo = Console.ReadKey(true);

                // Handle Enter
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    // We're intercepting characters, so we need to echo the newline
                    Console.Out.WriteLine();
                    return s;
                }

                // Tab is unsupported
                if (keyInfo.Key == ConsoleKey.Tab)
                {
                    continue;
                }

                // Handle Backspace
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (index <= 0)
                        continue;

                    // Update string
                    int length = s.Length;
                    s = s.Remove(index - 1, 1);
                    index--;

                    // Rewrite screen
                    Console.CursorTop = cursorTop;
                    Console.CursorLeft = cursorLeft;
                    Console.Out.Write(s.PadRight(length));

                    // Update cursor position
                    if (Console.CursorTop > cursorTop)
                    {
                        if (Console.CursorLeft > 0)
                        {
                            Console.CursorLeft--;
                        }
                        else
                        {
                            Console.CursorTop--;
                            Console.CursorLeft = Console.BufferWidth - 1;
                        }
                    }
                    else if (Console.CursorLeft > cursorLeft)
                    {
                        Console.CursorLeft--;
                    }

                    continue;
                }

                // Handle Delete
                if (keyInfo.Key == ConsoleKey.Delete || (keyInfo.Key == ConsoleKey.D && keyInfo.Modifiers == ConsoleModifiers.Control))
                {
                    if (index >= s.Length)
                        continue;

                    // Update string
                    int length = s.Length;
                    s = s.Remove(index, 1);
                    cursorCurrent = Console.CursorLeft;

                    // Update screen
                    Console.CursorTop = cursorTop; // TODO: Still some bug where pressing delete when end of line will cause the cursor to be on the next line
                    Console.CursorLeft = cursorLeft;
                    Console.Out.Write(s.PadRight(length));
                    Console.CursorLeft = cursorCurrent;

                    continue;
                }

                // Handle Left arrow
                if (keyInfo.Key == ConsoleKey.LeftArrow || (keyInfo.Key == ConsoleKey.B && keyInfo.Modifiers == ConsoleModifiers.Control))
                {
                    if (Console.CursorTop > cursorTop)
                    {
                        if (Console.CursorLeft > 0)
                        {
                            Console.CursorLeft--;
                        }
                        else
                        {
                            Console.CursorTop--;
                            Console.CursorLeft = Console.BufferWidth - 1;
                        }
                    }
                    else if (Console.CursorLeft > cursorLeft)
                    {
                        Console.CursorLeft--;
                    }

                    index--;

                    continue;
                }

                // Handle Right arrow
                if (keyInfo.Key == ConsoleKey.RightArrow || (keyInfo.Key == ConsoleKey.F && keyInfo.Modifiers == ConsoleModifiers.Control))
                {
                    if (cursorLeft + s.Length % Console.BufferWidth == Console.CursorLeft)
                        continue;

                    if (Console.CursorLeft < Console.BufferWidth - 1)
                    {
                        Console.CursorLeft++;
                    }
                    else
                    {
                        Console.CursorTop++;
                        Console.CursorLeft = 0;
                    }

                    index++;

                    continue;
                }

                // Handle Up arrow
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (historyIndex > 0)
                    {
                        historyIndex--;
                        int length = s.Length;
                        s = history[historyIndex];
                        Console.CursorLeft = cursorLeft;
                        Console.CursorTop = cursorTop;
                        Console.Write(s.PadRight(length));
                        Console.CursorLeft = (cursorLeft + s.Length) % Console.BufferWidth;
                        index = s.Length;
                    }

                    continue;
                }

                // Handle Down arrow
                if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    if (historyIndex < history.Count - 1)
                    {
                        historyIndex++;
                        int length = s.Length;
                        s = history[historyIndex];
                        Console.CursorLeft = cursorLeft;
                        Console.CursorTop = cursorTop;
                        Console.Write(s.PadRight(length));
                        Console.CursorLeft = (cursorLeft + s.Length) % Console.BufferWidth;
                        index = s.Length;
                    }

                    continue;
                }

                // Arrow/Page Up/down is unimplemented, so fail gracefully
                if (keyInfo.Key == ConsoleKey.PageUp || keyInfo.Key == ConsoleKey.PageDown)
                {
                    continue;
                }

                // Handle Home
                if (keyInfo.Key == ConsoleKey.Home || (keyInfo.Key == ConsoleKey.A && keyInfo.Modifiers == ConsoleModifiers.Control))
                {
                    Console.CursorLeft = cursorLeft;
                    Console.CursorTop = cursorTop;
                    index = 0;
                    continue;
                }

                // Handle End
                if (keyInfo.Key == ConsoleKey.End || (keyInfo.Key == ConsoleKey.E && keyInfo.Modifiers == ConsoleModifiers.Control))
                {
                    int lines = (cursorLeft + s.Length) / Console.BufferWidth;
                    int left = s.Length - (Console.BufferWidth * lines);

                    Console.CursorTop = cursorTop + lines;
                    Console.CursorLeft = cursorLeft + left;
                    index = s.Length;
                    continue;
                }

                // Handle Ctrl + C
                if (keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.C)
                    return null;

                // Handle Escape
                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.CursorLeft = cursorLeft;
                    Console.CursorTop = cursorTop;
                    index = s.Length;
                    s = string.Empty;
                    Console.Write(s.PadRight(index));
                    Console.CursorLeft = cursorLeft;
                    Console.CursorTop = cursorTop;
                    continue;
                }

                // Toggle insert/overwrite mode
                if (keyInfo.Key == ConsoleKey.Insert)
                {
                    insertMode = !insertMode;
                    continue;
                }

                // Blacklist control characters
                if (char.IsControl(keyInfo.KeyChar))
                {
                    continue;
                }

                // Handle case where terminal gets reset and the index is outside of the buffer
                if (index > s.Length)
                {
                    index = s.Length;
                }

                // Modify string
                if (!insertMode && index < s.Length) // then overwrite mode
                {
                    s = s.Remove(index, 1);
                }

                s = s.Insert(index, keyInfo.KeyChar.ToString());
                index++;

                // Redisplay string
                cursorCurrent = Console.CursorLeft;
                Console.CursorLeft = cursorLeft;
                Console.CursorTop = cursorTop;
                Console.Out.Write(s);
                Console.CursorTop = cursorTop + ((cursorLeft + index) / Console.BufferWidth);
                Console.CursorLeft = (cursorCurrent + 1) % Console.BufferWidth;
            }
            while (true);
        }
    }
}
#endif