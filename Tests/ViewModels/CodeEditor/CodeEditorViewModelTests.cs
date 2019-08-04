using Jamiras.Services;
using Jamiras.ViewModels.CodeEditor;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Windows.Input;

namespace Jamiras.Core.Tests.ViewModels.CodeEditor
{
    [TestFixture]
    class CodeEditorViewModelTests
    {
        [SetUp]
        public void Setup()
        {
            //            0         1         2         3         4
            //            01234567890123456789012345678901234567890123456
            var content = "bool test_function(int param1, string param2)\n" + // 1
                          "{\n" +                                             // 2
                          "    // This is an unnecessary comment\n" +         // 3
                          "    if (param1 == 1)\n" +                          // 4
                          "        param2 = 'Hello, world.';\n" +             // 5
                          "    else\n" +                                      // 6
                          "        param2 = NULL;\n" +                        // 7
                          "\n" +                                              // 8
                          "    return true;\n" +                              // 9
                          "}\n";                                              // 10

            mockTimerService = new Mock<ITimerService>();
            mockTimerService.Setup(t => t.WaitForTyping(It.IsAny<Action>())).Callback((Action a) => typingCallback = a);
            mockClipboardService = new Mock<IClipboardService>();
            mockBackgroundWorkerService = new Mock<IBackgroundWorkerService>();
            mockBackgroundWorkerService.Setup(b => b.RunAsync(It.IsAny<Action>())).Callback((Action a) => a());
            viewModel = new CodeEditorViewModel(mockClipboardService.Object, mockTimerService.Object, mockBackgroundWorkerService.Object);
            viewModel.SetContent(content);
            viewModel.FormatLine += FormatLine;
        }

        CodeEditorViewModel viewModel;
        Mock<IClipboardService> mockClipboardService;
        Mock<ITimerService> mockTimerService;
        Mock<IBackgroundWorkerService> mockBackgroundWorkerService;
        Action typingCallback;

        private void CompleteTyping()
        {
            if (typingCallback != null)
                typingCallback();
        }

        private void FormatLine(object sender, LineFormatEventArgs e)
        {
            var text = e.Text;
            int i = 0;
            while (i < text.Length)
            {
                int start = i;

                switch (text[i])
                {
                    case '=':
                        if (text[i + 1] == '=')
                        {
                            e.SetColor(i + 1, 2, 99);
                            i += 2;
                            continue;
                        }
                        break;

                    case '\'':
                        do
                        {
                            i++;
                        } while (i < text.Length && text[i] != '\'');

                        e.SetColor(start + 1, i - start, 98);
                        continue;

                    case '/':
                        if (text[i + 1] == '/')
                        {
                            do
                            {
                                i++;
                            } while (i < text.Length && text[i] != '\n');

                            e.SetColor(start + 1, i - start, 97);
                            continue;
                        }
                        break;

                    case ' ':
                    case '\n':
                    case '\t':
                        i++;
                        continue;
                }

                if (Char.IsLetterOrDigit(text[i]))
                {
                    do
                    {
                        i++;
                    } while (i < text.Length && (Char.IsLetterOrDigit(text[i]) || text[i] == '_'));

                    e.SetColor(start + 1, i - start, i);
                }
                else
                {
                    e.SetColor(i + 1, 1, 99);
                    i++;
                }
            }
        }

        [Test]
        public void TestInitialization()
        {
            Assert.That(viewModel.AreLineNumbersVisible, Is.True);
            Assert.That(viewModel.VisibleLines, Is.EqualTo(20));
            Assert.That(viewModel.CursorLine, Is.EqualTo(1));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
            Assert.That(viewModel.Lines.Count, Is.EqualTo(10));
            Assert.That(viewModel.Lines[0].LineLength, Is.EqualTo(45));
            Assert.That(viewModel.Lines[1].LineLength, Is.EqualTo(1));
            Assert.That(viewModel.Lines[2].LineLength, Is.EqualTo(37));
            Assert.That(viewModel.Lines[3].LineLength, Is.EqualTo(20));
            Assert.That(viewModel.Lines[4].LineLength, Is.EqualTo(33));
            Assert.That(viewModel.Lines[5].LineLength, Is.EqualTo(8));
            Assert.That(viewModel.Lines[6].LineLength, Is.EqualTo(22));
            Assert.That(viewModel.Lines[7].LineLength, Is.EqualTo(0));
            Assert.That(viewModel.Lines[8].LineLength, Is.EqualTo(16));
            Assert.That(viewModel.Lines[9].LineLength, Is.EqualTo(1));
        }

        [Test]
        [TestCase(1, 1, 1, 1)]
        [TestCase(5, 1, 5, 1)]
        [TestCase(10, 1, 10, 1)]
        [TestCase(15, 1, 10, 1)]
        [TestCase(1, 20, 1, 20)]
        [TestCase(1, 40, 1, 40)]
        [TestCase(1, 60, 1, 46)]
        [TestCase(6, 20, 6, 9)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(Int32.MaxValue, Int32.MaxValue, 10, 2)]
        public void TestMoveCursor(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 1)]
        [TestCase(2, 1, 1, 1)]
        [TestCase(10, 1, 9, 1)]
        [TestCase(3, 10, 2, 2)]
        [TestCase(9, 16, 8, 1)]
        public void TestKeyUp(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Up, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        public void TestKeyUpRememberColumn()
        {
            viewModel.MoveCursorTo(7, 20, CodeEditorViewModel.MoveCursorFlags.None);

            // there aren't 20 columns in line 6, so cursor should jump back to column 9
            Assert.That(viewModel.HandleKey(Key.Up, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(9));

            // but when the user continues to line 5, it should jump back to column 20
            Assert.That(viewModel.HandleKey(Key.Up, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(5));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(20));
        }

        [Test]
        [TestCase(1, 1, 2, 1)]
        [TestCase(9, 1, 10, 1)]
        [TestCase(10, 1, 10, 1)]
        [TestCase(1, 10, 2, 2)]
        [TestCase(9, 16, 10, 2)]
        public void TestKeyDown(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Down, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        public void TestKeyDownRememberColumn()
        {
            viewModel.MoveCursorTo(5, 20, CodeEditorViewModel.MoveCursorFlags.None);

            // there aren't 20 columns in line 6, so cursor should jump back to column 9
            Assert.That(viewModel.HandleKey(Key.Down, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(9));

            // but when the user continues to line 7, it should jump back to column 20
            Assert.That(viewModel.HandleKey(Key.Down, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(7));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(20));
        }

        [Test]
        [TestCase(1, 1, 1, 1)]
        [TestCase(1, 2, 1, 1)]
        [TestCase(2, 1, 1, 46)]
        [TestCase(5, 10, 5, 9)]
        public void TestKeyLeft(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Left, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 1)]
        [TestCase(1, 4, 1, 1)]
        [TestCase(1, 14, 1, 6)]
        [TestCase(1, 24, 1, 20)]
        [TestCase(5, 9, 4, 21)]
        [TestCase(5, 1, 4, 21)]
        public void TestKeyCtrlLeft(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Left, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 2)]
        [TestCase(1, 45, 1, 46)]
        [TestCase(1, 46, 2, 1)]
        [TestCase(5, 10, 5, 11)]
        public void TestKeyRight(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Right, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 6)]
        [TestCase(1, 5, 1, 6)]
        [TestCase(1, 6, 1, 19)]
        [TestCase(1, 23, 1, 24)]
        [TestCase(1, 24, 1, 30)]
        [TestCase(5, 1, 5, 9)]
        [TestCase(4, 21, 5, 9)]
        public void TestKeyCtrlRight(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Right, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 4, 1)]
        [TestCase(1, 30, 4, 21)]
        [TestCase(7, 10, 10, 2)]
        [TestCase(9, 10, 10, 2)]
        [TestCase(10, 1, 10, 1)]
        public void TestKeyPageDown(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.VisibleLines = 4;
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.PageDown, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 1)]
        [TestCase(2, 1, 1, 1)]
        [TestCase(4, 10, 1, 10)]
        [TestCase(5, 10, 2, 2)]
        [TestCase(10, 1, 7, 1)]
        public void TestKeyPageUp(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.VisibleLines = 4;
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.PageUp, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 1)]
        [TestCase(1, 9, 1, 1)]
        [TestCase(1, 42, 1, 1)]
        [TestCase(1, 46, 1, 1)]
        [TestCase(8, 1, 8, 1)]
        [TestCase(10, 1, 10, 1)]
        public void TestKeyHome(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Home, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 1)]
        [TestCase(1, 9, 1, 1)]
        [TestCase(1, 46, 1, 1)]
        [TestCase(8, 1, 1, 1)]
        [TestCase(10, 1, 1, 1)]
        public void TestKeyCtrlHome(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Home, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 1, 46)]
        [TestCase(1, 9, 1, 46)]
        [TestCase(1, 42, 1, 46)]
        [TestCase(1, 46, 1, 46)]
        [TestCase(8, 1, 8, 1)]
        [TestCase(10, 1, 10, 2)]
        public void TestKeyEnd(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.End, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        [TestCase(1, 1, 10, 2)]
        [TestCase(1, 9, 10, 2)]
        [TestCase(1, 46, 10, 2)]
        [TestCase(8, 1, 10, 2)]
        [TestCase(10, 1, 10, 2)]
        public void TestKeyCtrlEnd(int line, int column, int expectedLine, int expectedColumn)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.End, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(expectedLine));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
        }

        [Test]
        public void TestKeyBackspaceMidLine()
        {
            viewModel.MoveCursorTo(6, 7, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Back, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(6));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo("    ese"));
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
        }

        [Test]
        public void TestKeyBackspaceBeginLine()
        {
            viewModel.MoveCursorTo(7, 1, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Back, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(9));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo("    else        param2 = NULL;"));
            Assert.That(viewModel.LineCount, Is.EqualTo(9));
        }

        [Test]
        public void TestKeyBackspaceBeginDocument()
        {
            viewModel.MoveCursorTo(1, 1, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Back, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(1));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
            Assert.That(viewModel.Lines[5].PendingText, Is.Null);
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
        }

        [Test]
        public void TestKeyBackspaceAfterTypingAtStartOfLine()
        {
            // a negative cursor index was causing an exception when the Undo is finalized by CompleteTyping
            viewModel.MoveCursorTo(8, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.HandleCharacter('m');
            viewModel.HandleCharacter('y');
            viewModel.HandleCharacter('_');
            Assert.That(viewModel.HandleKey(Key.Back, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(8));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(3));
            Assert.That(viewModel.Lines[7].PendingText, Is.EqualTo("my"));
            CompleteTyping();
        }

        [Test]
        public void TestKeyDeleteMidLine()
        {
            viewModel.MoveCursorTo(6, 7, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Delete, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(7));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo("    ele"));
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
        }

        [Test]
        public void TestKeyDeleteEndLine()
        {
            viewModel.MoveCursorTo(6, 9, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Delete, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(9));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo("    else        param2 = NULL;"));
            Assert.That(viewModel.LineCount, Is.EqualTo(9));
        }

        [Test]
        public void TestKeyDeleteEndDocument()
        {
            viewModel.MoveCursorTo(10, 2, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Delete, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(10));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(2));
            Assert.That(viewModel.Lines[5].PendingText, Is.Null);
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
        }

        [Test]
        public void TestKeyEnterMidLine()
        {
            viewModel.MoveCursorTo(6, 7, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Enter, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(7));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(5));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo("    el"));
            Assert.That(viewModel.Lines[6].PendingText, Is.EqualTo("    se"));
            Assert.That(viewModel.Lines[7].Text, Is.EqualTo("        param2 = NULL;"));
            Assert.That(viewModel.Lines[5].Line, Is.EqualTo(6));
            Assert.That(viewModel.Lines[6].Line, Is.EqualTo(7));
            Assert.That(viewModel.Lines[7].Line, Is.EqualTo(8));
            Assert.That(viewModel.LineCount, Is.EqualTo(11));
        }

        [Test]
        public void TestKeyEnterBeginLine()
        {
            viewModel.MoveCursorTo(6, 1, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Enter, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(7));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo(""));
            Assert.That(viewModel.Lines[6].PendingText, Is.EqualTo("    else"));
            Assert.That(viewModel.Lines[7].Text, Is.EqualTo("        param2 = NULL;"));
            Assert.That(viewModel.Lines[5].Line, Is.EqualTo(6));
            Assert.That(viewModel.Lines[6].Line, Is.EqualTo(7));
            Assert.That(viewModel.Lines[7].Line, Is.EqualTo(8));
            Assert.That(viewModel.LineCount, Is.EqualTo(11));
        }

        [Test]
        public void TestKeyEnterEndLine()
        {
            viewModel.MoveCursorTo(6, 9, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Enter, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(7));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(5));
            Assert.That(viewModel.Lines[5].Text, Is.EqualTo("    else"));
            Assert.That(viewModel.Lines[5].PendingText, Is.Null);
            Assert.That(viewModel.Lines[6].PendingText, Is.EqualTo("    "));
            Assert.That(viewModel.Lines[7].Text, Is.EqualTo("        param2 = NULL;"));
            Assert.That(viewModel.Lines[5].Line, Is.EqualTo(6));
            Assert.That(viewModel.Lines[6].Line, Is.EqualTo(7));
            Assert.That(viewModel.Lines[7].Line, Is.EqualTo(8));
            Assert.That(viewModel.LineCount, Is.EqualTo(11));
        }

        [Test]
        [TestCase(1, 5, "        else")]
        [TestCase(3, 5, "      else")]
        [TestCase(5, 9, "        else")]
        [TestCase(6, 9, "    e   lse")]
        [TestCase(8, 9, "    els e")]
        [TestCase(9, 13, "    else    ")]
        public void TestKeyTab(int column, int expectedColumn, string expectedText)
        {
            viewModel.MoveCursorTo(6, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Tab, ModifierKeys.None), Is.True);
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo(expectedText));
        }

        [Test]
        public void TestHandleCharacter()
        {
            viewModel.MoveCursorTo(6, 7, CodeEditorViewModel.MoveCursorFlags.None);

            // can't call HandleKey(Key.I) because the key=>char mapping relies on the keyboard state
            viewModel.HandleCharacter('i');
            Assert.That(viewModel.CursorLine, Is.EqualTo(6));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(8));
            Assert.That(viewModel.Lines[5].PendingText, Is.EqualTo("    elise"));
        }

        [Test]
        [TestCase(1, 1, 5, "bool")]
        [TestCase(1, 3, 5, "bool")]
        [TestCase(1, 4, 5, "bool")]
        [TestCase(1, 5, 6, " ")]
        [TestCase(1, 6, 19, "test_function")]
        [TestCase(3, 10, 12, "This")]
        [TestCase(4, 16, 18, "==")]
        [TestCase(5, 21, 24, "Hello")]
        [TestCase(5, 24, 25, ",")]
        [TestCase(7, 20, 22, "NULL")]
        public void TestHightlightWordAt(int line, int column, int expectedColumn, string expectedText)
        {
            viewModel.HighlightWordAt(line, column);
            Assert.That(viewModel.CursorLine, Is.EqualTo(line));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(expectedColumn));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(expectedText));
        }

        [Test]
        [TestCase(1, 11, "f")]
        [TestCase(2, 2, "\n")]
        public void TestHighlightKeyRight(int line, int column, string expectedText)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Right, ModifierKeys.Shift), Is.True);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(expectedText));
        }

        [Test]
        [TestCase(1, 11, "_")]
        [TestCase(3, 1, "\n")]
        public void TestHighlightKeyLeft(int line, int column, string expectedText)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Left, ModifierKeys.Shift), Is.True);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(expectedText));
        }

        [Test]
        [TestCase(4, 12, " is an unnecessary comment\n    if (par")]
        [TestCase(9, 7, "\n    re")]
        public void TestHighlightKeyUp(int line, int column, string expectedText)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Up, ModifierKeys.Shift), Is.True);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(expectedText));
        }

        [Test]
        [TestCase(4, 12, "am1 == 1)\n        par")]
        [TestCase(9, 7, "turn true;\n}")]
        public void TestHighlightKeyDown(int line, int column, string expectedText)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Down, ModifierKeys.Shift), Is.True);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(expectedText));
        }

        [Test]
        [TestCase(4, 12, "    if (par")]
        [TestCase(9, 7, "    re")]
        [TestCase(6, 1, "")]
        public void TestHighlightKeyHome(int line, int column, string expectedText)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.Home, ModifierKeys.Shift), Is.True);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(expectedText));
        }

        [Test]
        [TestCase(4, 12, "am1 == 1)")]
        [TestCase(9, 7, "turn true;")]
        [TestCase(6, 9, "")]
        public void TestHighlightKeyEnd(int line, int column, string expectedText)
        {
            viewModel.MoveCursorTo(line, column, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.End, ModifierKeys.Shift), Is.True);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(expectedText));
        }

        [Test]
        public void TestHighlightMultiLine()
        {
            viewModel.MoveCursorTo(5, 12, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(4, 12, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo("am1 == 1)\n        par"));
            Assert.That(viewModel.Lines[3].SelectionStart, Is.EqualTo(12));
            Assert.That(viewModel.Lines[3].SelectionEnd, Is.EqualTo(20));
            Assert.That(viewModel.Lines[4].SelectionStart, Is.EqualTo(1));
            Assert.That(viewModel.Lines[4].SelectionEnd, Is.EqualTo(11));

            viewModel.MoveCursorTo(5, 14, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo("am"));
            Assert.That(viewModel.Lines[3].SelectionStart, Is.EqualTo(0));
            Assert.That(viewModel.Lines[3].SelectionEnd, Is.EqualTo(0));
            Assert.That(viewModel.Lines[4].SelectionStart, Is.EqualTo(12));
            Assert.That(viewModel.Lines[4].SelectionEnd, Is.EqualTo(13));
        }

        [Test]
        public void TestKeyCtrlA()
        {
            viewModel.MoveCursorTo(5, 20, CodeEditorViewModel.MoveCursorFlags.None);
            Assert.That(viewModel.HandleKey(Key.A, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.GetSelectedText().Length, Is.EqualTo(192));

            // trim Content() since SelectedText doesn't contain the trailing newline
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(viewModel.GetContent().TrimEnd()));
        }

        [Test]
        [TestCase(4, 9, 4, 15, "param1")]
        [TestCase(4, 9, 4, 5, "if (")]
        [TestCase(4, 9, 4, 9, "")]
        [TestCase(9, 12, 10, 2, "true;\n}")]
        [TestCase(10, 2, 9, 12, "true;\n}")]
        public void TestKeyCtrlC(int startLine, int startColumn, int endLine, int endColumn, string expectedText)
        {
            string clipboardText = null;
            mockClipboardService.Setup(c => c.SetData(It.IsAny<string>())).Callback<string>((t) => clipboardText = t);

            viewModel.MoveCursorTo(startLine, startColumn, CodeEditorViewModel.MoveCursorFlags.None);
            if (endColumn != startColumn || endLine != startLine)
                viewModel.MoveCursorTo(endLine, endColumn, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            Assert.That(viewModel.HandleKey(Key.C, ModifierKeys.Control), Is.True);
            Assert.That(clipboardText, Is.EqualTo(expectedText));
        }

        [Test]
        [TestCase(4, 9, 4, 15, "param1", "    if ( == 1)")]
        public void TestKeyCtrlX(int startLine, int startColumn, int endLine, int endColumn, string expectedText, string remainingText)
        {
            string clipboardText = null;
            mockClipboardService.Setup(c => c.SetData(It.IsAny<string>())).Callback<string>((t) => clipboardText = t);

            viewModel.MoveCursorTo(startLine, startColumn, CodeEditorViewModel.MoveCursorFlags.None);
            if (endColumn != startColumn || endLine != startLine)
                viewModel.MoveCursorTo(endLine, endColumn, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            Assert.That(viewModel.HandleKey(Key.X, ModifierKeys.Control), Is.True);
            Assert.That(clipboardText, Is.EqualTo(expectedText));
            Assert.That(viewModel.Lines[3].Text, Is.EqualTo(remainingText));
        }

        [Test]
        public void TestReplaceTextWord()
        {
            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("orange");

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool orange(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(12));
        }

        [Test]
        public void TestReplaceTextWordReverse()
        {
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("orange");

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool orange(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(12));
        }

        [Test]
        public void TestUndoReplaceTextWord()
        {
            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("orange");
            viewModel.HandleKey(Key.Z, ModifierKeys.Control);

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool test_function(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(19));
        }

        [Test]
        public void TestUndoReplaceTextWordReverse()
        {
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("orange");
            viewModel.HandleKey(Key.Z, ModifierKeys.Control);

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool test_function(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(6));
        }

        [Test]
        public void TestReplaceTextWordWithMultipleLines()
        {
            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("orange\r\nbanana");

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool orange"));
            Assert.That(viewModel.Lines[1].Text, Is.EqualTo("banana(int param1, string param2)"));

            Assert.That(viewModel.Lines[0].Line, Is.EqualTo(1));
            Assert.That(viewModel.Lines[1].Line, Is.EqualTo(2));
            Assert.That(viewModel.Lines[2].Line, Is.EqualTo(3));
            Assert.That(viewModel.Lines[10].Line, Is.EqualTo(11));

            Assert.That(viewModel.CursorLine, Is.EqualTo(2));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(7));
        }

        [Test]
        public void TestReplaceMultipleLinesWithWord()
        {
            viewModel.MoveCursorTo(5, 18, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(7, 22, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("6");

            Assert.That(viewModel.Lines[4].Text, Is.EqualTo("        param2 = 6;"));
            Assert.That(viewModel.Lines[5].Text, Is.EqualTo("")); // used to be line 8

            Assert.That(viewModel.Lines[4].Line, Is.EqualTo(5));
            Assert.That(viewModel.Lines[5].Line, Is.EqualTo(6));
            Assert.That(viewModel.Lines[7].Line, Is.EqualTo(8));
            Assert.That(viewModel.Lines.Count, Is.EqualTo(8));

            Assert.That(viewModel.CursorLine, Is.EqualTo(5));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(19));
        }

        [Test]
        public void TestReplaceMultipleLinesWithMultipleLines()
        {
            viewModel.MoveCursorTo(3, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(4, 21, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("    // New\n    if (param2 == 2)");

            Assert.That(viewModel.Lines[2].Text, Is.EqualTo("    // New"));
            Assert.That(viewModel.Lines[3].Text, Is.EqualTo("    if (param2 == 2)"));

            Assert.That(viewModel.Lines[2].Line, Is.EqualTo(3));
            Assert.That(viewModel.Lines[3].Line, Is.EqualTo(4));
            Assert.That(viewModel.Lines[4].Line, Is.EqualTo(5));
            Assert.That(viewModel.Lines.Count, Is.EqualTo(10));

            Assert.That(viewModel.CursorLine, Is.EqualTo(4));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(21));
        }

        [Test]
        public void TestReplaceMultipleLinesWithMultipleLinesAtEndOfFile()
        {
            viewModel.MoveCursorTo(8, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(10, 2, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.ReplaceSelection("test");

            Assert.That(viewModel.Lines[7].Text, Is.EqualTo("test"));
            Assert.That(viewModel.Lines[7].Line, Is.EqualTo(8));
            Assert.That(viewModel.Lines.Count, Is.EqualTo(8));

            Assert.That(viewModel.CursorLine, Is.EqualTo(8));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(5));
        }

        class CodeEditorViewModelContentChangedHarness : CodeEditorViewModel
        {
            public CodeEditorViewModelContentChangedHarness(IClipboardService clipboardService, ITimerService timerService, IBackgroundWorkerService backgroundWorkerService)
                : base(clipboardService, timerService, backgroundWorkerService)
            {
            }

            private int _expectUpdatedLines = 0;

            public void ExpectUpdatedLines(int count)
            {
                _expectUpdatedLines = count;
            }

            protected override void OnContentChanged(ContentChangedEventArgs e)
            {
                Assert.That(e.UpdatedLines.Count(), Is.EqualTo(_expectUpdatedLines));
                base.OnContentChanged(e);
            }
        }

        [Test]
        public void TestReplaceMultipleLinesWithMultipleLinesContentChanged()
        {
            var viewModel2 = new CodeEditorViewModelContentChangedHarness(mockClipboardService.Object, mockTimerService.Object, mockBackgroundWorkerService.Object);
            viewModel2.SetContent(viewModel.GetContent());
            viewModel2.FormatLine += FormatLine;
            viewModel2.ExpectUpdatedLines(3);

            viewModel2.MoveCursorTo(3, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel2.MoveCursorTo(5, 34, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel2.ReplaceSelection("    // New\n    if (param2 == 2)\n        param2 = 'New';");

            Assert.That(viewModel2.Lines[2].Text, Is.EqualTo("    // New"));
            Assert.That(viewModel2.Lines[3].Text, Is.EqualTo("    if (param2 == 2)"));
            Assert.That(viewModel2.Lines[4].Text, Is.EqualTo("        param2 = 'New';"));

            Assert.That(viewModel2.Lines[2].Line, Is.EqualTo(3));
            Assert.That(viewModel2.Lines[3].Line, Is.EqualTo(4));
            Assert.That(viewModel2.Lines[4].Line, Is.EqualTo(5));
            Assert.That(viewModel2.Lines.Count, Is.EqualTo(10));

            Assert.That(viewModel2.CursorLine, Is.EqualTo(5));
            Assert.That(viewModel2.CursorColumn, Is.EqualTo(24));
        }

        [Test]
        public void TestReplaceTextWordNoSelection()
        {
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.ReplaceSelection("_2");

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool test_function_2(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(21));
        }

        [Test]
        public void TestKeyCtrlV()
        {
            mockClipboardService.Setup(c => c.GetText()).Returns("orange");

            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            Assert.That(viewModel.HandleKey(Key.V, ModifierKeys.Control), Is.True);

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool orange(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(12));
        }

        [Test]
        public void TestKeyCtrlZ()
        {
            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            Assert.That(viewModel.HandleKey(Key.X, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.HandleKey(Key.Z, ModifierKeys.Control), Is.True);

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool test_function(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(19));
        }

        [Test]
        public void TestKeyCtrlY()
        {
            mockClipboardService.Setup(c => c.GetText()).Returns("orange");

            viewModel.MoveCursorTo(1, 6, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 19, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            Assert.That(viewModel.HandleKey(Key.V, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.HandleKey(Key.Z, ModifierKeys.Control), Is.True);
            Assert.That(viewModel.HandleKey(Key.Y, ModifierKeys.Control), Is.True);

            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool orange(int param1, string param2)"));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(12));
        }

        [Test]
        public void TestUndoTyping()
        {
            viewModel.MoveCursorTo(9, 16, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);

            CompleteTyping();

            viewModel.HandleKey(Key.F, ModifierKeys.None);
            viewModel.HandleKey(Key.A, ModifierKeys.None);
            viewModel.HandleKey(Key.L, ModifierKeys.None);
            viewModel.HandleKey(Key.S, ModifierKeys.None);
            viewModel.HandleKey(Key.E, ModifierKeys.None);

            CompleteTyping();

            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return false;"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return ;"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
        }

        [Test]
        public void TestUndoTypingAcrossLines()
        {
            viewModel.MoveCursorTo(9, 5, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.HandleCharacter('a');
            viewModel.HandleCharacter('=');
            viewModel.HandleCharacter('3');
            viewModel.HandleKey(Key.Enter, ModifierKeys.None);
            viewModel.HandleCharacter('/');
            viewModel.HandleCharacter('/');
            viewModel.HandleCharacter(' ');
            CompleteTyping();

            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    a=3"));
            Assert.That(viewModel.Lines[9].Text, Is.EqualTo("    // return true;"));
            Assert.That(viewModel.LineCount, Is.EqualTo(11));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
        }

        [Test]
        public void TestUndoTypingImplicitStopWhenChangingDirections()
        {
            viewModel.MoveCursorTo(9, 16, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);

            // no CompleteTyping here - change in direction forces new undo block

            viewModel.HandleKey(Key.F, ModifierKeys.None);
            viewModel.HandleKey(Key.A, ModifierKeys.None);
            viewModel.HandleKey(Key.L, ModifierKeys.None);
            viewModel.HandleKey(Key.S, ModifierKeys.None);
            viewModel.HandleKey(Key.E, ModifierKeys.None);

            CompleteTyping();

            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return false;"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return ;"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
        }

        [Test]
        public void TestUndoTypingDeleted()
        {
            viewModel.MoveCursorTo(9, 12, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.HandleKey(Key.Delete, ModifierKeys.None);
            viewModel.HandleKey(Key.Delete, ModifierKeys.None);
            viewModel.HandleKey(Key.Delete, ModifierKeys.None);
            viewModel.HandleKey(Key.Delete, ModifierKeys.None);

            // no change in direction, can merge undo block

            viewModel.HandleKey(Key.F, ModifierKeys.None);
            viewModel.HandleKey(Key.A, ModifierKeys.None);
            viewModel.HandleKey(Key.L, ModifierKeys.None);
            viewModel.HandleKey(Key.S, ModifierKeys.None);
            viewModel.HandleKey(Key.E, ModifierKeys.None);

            CompleteTyping();

            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return false;"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
        }

        [Test]
        public void TestUndoTypingReplacingSelection()
        {
            viewModel.MoveCursorTo(9, 12, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(9, 16, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            viewModel.HandleKey(Key.F, ModifierKeys.None);
            viewModel.HandleKey(Key.A, ModifierKeys.None);
            viewModel.HandleKey(Key.L, ModifierKeys.None);
            viewModel.HandleKey(Key.S, ModifierKeys.None);
            viewModel.HandleKey(Key.E, ModifierKeys.None);

            CompleteTyping();

            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return false;"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
        }

        [Test]
        public void TestUndoTypingDeleteAcrossLines()
        {
            viewModel.MoveCursorTo(9, 16, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.HandleKey(Key.Delete, ModifierKeys.None);
            viewModel.HandleKey(Key.Delete, ModifierKeys.None);

            CompleteTyping();

            Assert.That(viewModel.LineCount, Is.EqualTo(9));
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true}"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
            Assert.That(viewModel.Lines[9].Text, Is.EqualTo("}"));
        }

        [Test]
        public void TestUndoTypingBackspaceAcrossLines()
        {
            viewModel.MoveCursorTo(10, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);
            viewModel.HandleKey(Key.Back, ModifierKeys.None);

            CompleteTyping();

            Assert.That(viewModel.LineCount, Is.EqualTo(9));
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true}"));

            viewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(viewModel.LineCount, Is.EqualTo(10));
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
            Assert.That(viewModel.Lines[9].Text, Is.EqualTo("}"));
        }

        [Test]
        public void TestIndentLine()
        {
            viewModel.MoveCursorTo(9, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(9, 17, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            viewModel.HandleKey(Key.Tab, ModifierKeys.None);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("        return true;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo("        return true;\n"));
            Assert.That(viewModel.CursorLine, Is.EqualTo(10));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
        }

        [Test]
        public void TestIndentMultipleLines()
        {
            viewModel.MoveCursorTo(7, 9, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(9, 9, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            viewModel.HandleKey(Key.Tab, ModifierKeys.None);
            Assert.That(viewModel.Lines[6].Text, Is.EqualTo("            param2 = NULL;"));
            Assert.That(viewModel.Lines[7].Text, Is.EqualTo("")); // whitepsace only line shouldn't be affected
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("        return true;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo("            param2 = NULL;\n\n" +
                                                                "        return true;\n"));
            Assert.That(viewModel.CursorLine, Is.EqualTo(10));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
        }

        [Test]
        public void TestUndoIndentMultipleLines()
        {
            viewModel.MoveCursorTo(7, 9, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(9, 9, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            viewModel.HandleKey(Key.Tab, ModifierKeys.None);
            viewModel.HandleKey(Key.Z, ModifierKeys.Control);

            Assert.That(viewModel.Lines[6].Text, Is.EqualTo("        param2 = NULL;"));
            Assert.That(viewModel.Lines[7].Text, Is.EqualTo(""));
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(""));
            Assert.That(viewModel.CursorLine, Is.EqualTo(10));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
        }

        [Test]
        public void TestUnindentLine()
        {
            viewModel.MoveCursorTo(9, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(9, 17, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            viewModel.HandleKey(Key.Tab, ModifierKeys.Shift);
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("return true;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo("return true;\n"));
            Assert.That(viewModel.CursorLine, Is.EqualTo(10));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
        }

        [Test]
        public void TestUnindentMultipleLines()
        {
            viewModel.MoveCursorTo(7, 9, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(9, 9, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            viewModel.HandleKey(Key.Tab, ModifierKeys.Shift);
            Assert.That(viewModel.Lines[6].Text, Is.EqualTo("    param2 = NULL;"));
            Assert.That(viewModel.Lines[7].Text, Is.EqualTo("")); // whitepsace only line shouldn't be affected
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("return true;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo("    param2 = NULL;\n\n" +
                                                                "return true;\n"));
            Assert.That(viewModel.CursorLine, Is.EqualTo(10));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
        }

        [Test]
        public void TestUndoUnindentMultipleLines()
        {
            viewModel.MoveCursorTo(7, 9, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(9, 9, CodeEditorViewModel.MoveCursorFlags.Highlighting);

            viewModel.HandleKey(Key.Tab, ModifierKeys.Shift);
            viewModel.HandleKey(Key.Z, ModifierKeys.Control);

            Assert.That(viewModel.Lines[6].Text, Is.EqualTo("        param2 = NULL;"));
            Assert.That(viewModel.Lines[7].Text, Is.EqualTo(""));
            Assert.That(viewModel.Lines[8].Text, Is.EqualTo("    return true;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(""));
            Assert.That(viewModel.CursorLine, Is.EqualTo(10));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
        }

        [Test]
        public void TestDeleteSelectionMidLine()
        {
            viewModel.MoveCursorTo(1, 20, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(1, 45, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.DeleteSelection();
            Assert.That(viewModel.CursorLine, Is.EqualTo(1));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(20));
            Assert.That(viewModel.Lines[0].Text, Is.EqualTo("bool test_function()"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(""));
        }

        [Test]
        public void TestDeleteSelectionEntireLine()
        {
            viewModel.MoveCursorTo(3, 1, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(4, 1, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.DeleteSelection();
            Assert.That(viewModel.CursorLine, Is.EqualTo(3));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(1));
            Assert.That(viewModel.LineCount, Is.EqualTo(9));
            Assert.That(viewModel.Lines[2].Text, Is.EqualTo("    if (param1 == 1)"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(""));
        }

        [Test]
        public void TestDeleteSelectionMultiLine()
        {
            viewModel.MoveCursorTo(4, 5, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(7, 9, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.DeleteSelection();
            Assert.That(viewModel.CursorLine, Is.EqualTo(4));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(5));
            Assert.That(viewModel.LineCount, Is.EqualTo(7));
            Assert.That(viewModel.Lines[3].Text, Is.EqualTo("    param2 = NULL;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(""));
        }

        [Test]
        public void TestDeleteSelectionMultiLineReverse()
        {
            viewModel.MoveCursorTo(7, 9, CodeEditorViewModel.MoveCursorFlags.None);
            viewModel.MoveCursorTo(4, 5, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            viewModel.DeleteSelection();
            Assert.That(viewModel.CursorLine, Is.EqualTo(4));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(5));
            Assert.That(viewModel.LineCount, Is.EqualTo(7));
            Assert.That(viewModel.Lines[3].Text, Is.EqualTo("    param2 = NULL;"));
            Assert.That(viewModel.GetSelectedText(), Is.EqualTo(""));
        }

        class BracingCodeEditorViewModel : CodeEditorViewModel
        {
            public BracingCodeEditorViewModel(IClipboardService clipboardService, ITimerService timerService, IBackgroundWorkerService backgroundWorkerService)
                : base(clipboardService, timerService, backgroundWorkerService)
            {
                Braces['('] = ')';

                SetContent("");
            }
        }

        private BracingCodeEditorViewModel CreateBracingCodeEditorViewModel()
        {
            return new BracingCodeEditorViewModel(mockClipboardService.Object, mockTimerService.Object, mockBackgroundWorkerService.Object);
        }

        [Test]
        public void TestBraceMatching()
        {
            var bracingViewModel = CreateBracingCodeEditorViewModel();
            bracingViewModel.HandleCharacter('f');
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f()"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(3));

            bracingViewModel.HandleCharacter('1');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f(1)"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(4));

            bracingViewModel.HandleCharacter(')');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f(1)"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(5));
        }

        [Test]
        public void TestBraceMatchingDelete()
        {
            var bracingViewModel = CreateBracingCodeEditorViewModel();
            bracingViewModel.HandleCharacter('f');
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f()"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(3));

            bracingViewModel.HandleKey(Key.Delete, ModifierKeys.None);
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f("));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(3));
        }

        [Test]
        public void TestBraceMatchingBackspace()
        {
            var bracingViewModel = CreateBracingCodeEditorViewModel();
            bracingViewModel.HandleCharacter('f');
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f()"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(3));

            bracingViewModel.HandleKey(Key.Back, ModifierKeys.None);
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(2));
        }

        [Test]
        public void TestBraceMatchingUndo()
        {
            var bracingViewModel = CreateBracingCodeEditorViewModel();
            bracingViewModel.HandleCharacter('f');
            CompleteTyping();
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f()"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(3));

            bracingViewModel.HandleKey(Key.Z, ModifierKeys.Control);
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(2));
        }

        [Test]
        public void TestBraceMatchingStoppedByCursorMovement()
        {
            var bracingViewModel = CreateBracingCodeEditorViewModel();
            bracingViewModel.HandleCharacter('f');
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f()"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(3));

            bracingViewModel.HandleKey(Key.Left, ModifierKeys.None);
            bracingViewModel.HandleKey(Key.Right, ModifierKeys.None);

            bracingViewModel.HandleCharacter(')');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f())"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(4));
        }

        [Test]
        public void TestBraceMatchingNewLine()
        {
            var bracingViewModel = CreateBracingCodeEditorViewModel();
            bracingViewModel.HandleCharacter('f');
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f()"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(3));

            bracingViewModel.HandleKey(Key.Enter, ModifierKeys.None);
            CompleteTyping();
            Assert.That(bracingViewModel.LineCount, Is.EqualTo(3));
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("f("));
            Assert.That(bracingViewModel.Lines[1].Text, Is.EqualTo("    "));
            Assert.That(bracingViewModel.Lines[2].Text, Is.EqualTo(")"));
            Assert.That(bracingViewModel.CursorLine, Is.EqualTo(2));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(5));

            bracingViewModel.HandleCharacter(')');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[1].Text, Is.EqualTo("    )"));
            Assert.That(bracingViewModel.Lines[2].Text, Is.EqualTo(")"));
            Assert.That(bracingViewModel.CursorColumn, Is.EqualTo(6));
        }

        [Test]
        public void TestBraceMatchingNextChar()
        {
            var bracingViewModel = CreateBracingCodeEditorViewModel();
            bracingViewModel.HandleCharacter('a');
            bracingViewModel.HandleCharacter(' ');
            bracingViewModel.HandleCharacter('b');
            bracingViewModel.HandleCharacter('.');
            bracingViewModel.HandleCharacter('c');

            // end of line should add match
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("a b.c()"));

            // before punctuation should add match
            bracingViewModel.MoveCursorTo(1, 4, CodeEditorViewModel.MoveCursorFlags.None);
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("a b().c()"));

            // before space should add match
            bracingViewModel.MoveCursorTo(1, 2, CodeEditorViewModel.MoveCursorFlags.None);
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("a() b().c()"));

            // before letter should not add match
            bracingViewModel.MoveCursorTo(1, 5, CodeEditorViewModel.MoveCursorFlags.None);
            bracingViewModel.HandleCharacter('(');
            CompleteTyping();
            Assert.That(bracingViewModel.Lines[0].Text, Is.EqualTo("a() (b().c()"));
        }
    }
}
