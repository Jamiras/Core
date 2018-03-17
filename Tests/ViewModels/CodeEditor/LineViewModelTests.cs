using Jamiras.Services;
using Jamiras.ViewModels.CodeEditor;
using Moq;
using NUnit.Framework;
using System.Text;

namespace Jamiras.Core.Tests.ViewModels.CodeEditor
{
    [TestFixture]
    class LineViewModelTests
    {
        [SetUp]
        public void Setup()
        {
            var mockClipboard = new Mock<IClipboardService>();
            var mockTimer = new Mock<ITimerService>();
            var mockBackgroundWorkerService = new Mock<IBackgroundWorkerService>();
            codeEditor = new CodeEditorViewModel(mockClipboard.Object, mockTimer.Object, mockBackgroundWorkerService.Object);
            viewModel = new LineViewModel(codeEditor, 1);
        }

        CodeEditorViewModel codeEditor;
        LineViewModel viewModel;

        [Test]
        public void TestInitialization()
        {
            Assert.That(viewModel.Line, Is.EqualTo(1));
            Assert.That(viewModel.SelectionStart, Is.EqualTo(0));
            Assert.That(viewModel.SelectionEnd, Is.EqualTo(0));
            Assert.That(viewModel.CursorColumn, Is.EqualTo(0));
            Assert.That(viewModel.LineLength, Is.EqualTo(0));
            Assert.That(viewModel.Text, Is.EqualTo(""));
            Assert.That(viewModel.PendingText, Is.Null);
        }

        [Test]
        [TestCase(1, "-", "-This is a test.")]
        [TestCase(1, "1) ", "1) This is a test.")]
        [TestCase(2, "o ", "To his is a test.")]
        [TestCase(5, "?", "This? is a test.")]
        [TestCase(6, "t", "This tis a test.")]
        [TestCase(9, "not ", "This is not a test.")]
        [TestCase(15, "s", "This is a tests.")]
        [TestCase(16, "!", "This is a test.!")]
        public void TestInsert(int column, string str, string expected)
        {
            //                1234567890123456
            viewModel.Text = "This is a test.";
            viewModel.SetValue(LineViewModel.TextPiecesProperty, new[]
            {
                new TextPiece { Text = "This" },
                new TextPiece { Text = " " },
                new TextPiece { Text = "is" },
                new TextPiece { Text = " " },
                new TextPiece { Text = "a" },
                new TextPiece { Text = " " },
                new TextPiece { Text = "test" },
                new TextPiece { Text = "." },
            });

            viewModel.Insert(column, str);

            Assert.That(viewModel.Text, Is.EqualTo("This is a test."));
            Assert.That(viewModel.PendingText, Is.EqualTo(expected));

            var builder = new StringBuilder();
            foreach (var piece in viewModel.TextPieces)
                builder.Append(piece.Text);

            Assert.That(builder.ToString(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1, 1, "his is a test.")]
        [TestCase(1, 6, "s a test.")]
        [TestCase(2, 6, "Ts a test.")]
        [TestCase(8, 8, "This isa test.")]
        [TestCase(14, 14, "This is a tes.")]
        [TestCase(14, 15, "This is a tes")]
        [TestCase(1, 15, "")]
        public void TestRemove(int startColumn, int endColumn, string expected)
        {
            //                1234567890123456
            viewModel.Text = "This is a test.";
            viewModel.SetValue(LineViewModel.TextPiecesProperty, new[]
            {
                new TextPiece { Text = "This" },
                new TextPiece { Text = " " },
                new TextPiece { Text = "is" },
                new TextPiece { Text = " " },
                new TextPiece { Text = "a" },
                new TextPiece { Text = " " },
                new TextPiece { Text = "test" },
                new TextPiece { Text = "." },
            });

            viewModel.Remove(startColumn, endColumn);

            Assert.That(viewModel.Text, Is.EqualTo("This is a test."));
            Assert.That(viewModel.PendingText, Is.EqualTo(expected));

            var builder = new StringBuilder();
            foreach (var piece in viewModel.TextPieces)
                builder.Append(piece.Text);

            Assert.That(builder.ToString(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase(4, 5, 6, 7, 4, 5)]
        [TestCase(5, 6, 6, 7, 5, 5)]
        [TestCase(6, 7, 6, 7, 0, 0)]
        [TestCase(7, 8, 6, 7, 6, 6)]
        [TestCase(8, 9, 6, 7, 6, 7)]
        [TestCase(5, 8, 6, 7, 5, 6)]
        [TestCase(6, 7, 5, 8, 0, 0)]
        public void TestRemoveUpdatesSelection(int selectionStartColumn, int selectionEndColumn, int removeStartColumn, int removeEndColumn, int expectedSelectionStartColumn, int expectedSelectionEndColumn)
        {
            //                1234567890123456
            viewModel.Text = "This is a test.";
            viewModel.SetValue(LineViewModel.TextPiecesProperty, new[]
            {
                new TextPiece { Text = viewModel.Text }
            });

            viewModel.Select(selectionStartColumn, selectionEndColumn);
            viewModel.Remove(removeStartColumn, removeEndColumn);

            Assert.That(viewModel.SelectionStart, Is.EqualTo(expectedSelectionStartColumn));
            Assert.That(viewModel.SelectionEnd, Is.EqualTo(expectedSelectionEndColumn));
        }
    }
}
