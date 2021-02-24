using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.WkHtmlToX.Abstractions;
using AdaskoTheBeAsT.WkHtmlToX.Documents;
using AdaskoTheBeAsT.WkHtmlToX.Engine;
using AdaskoTheBeAsT.WkHtmlToX.Exceptions;
using AdaskoTheBeAsT.WkHtmlToX.WorkItems;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;

namespace AdaskoTheBeAsT.WkHtmlToX.Test.Engine
{
    public sealed class EngineTest
        : IDisposable
    {
        private readonly WkHtmlToXConfiguration _configuration;
        private readonly Mock<ILibraryLoader> _libraryLoaderMock;
        private readonly Mock<ILibraryLoaderFactory> _libraryLoaderFactoryMock;
        private readonly Mock<IPdfProcessor> _pdfProcessorMock;
        private readonly Mock<IImageProcessor> _imageProcessorMock;
        private readonly Mock<IWkHtmlToPdfModule> _pdfModuleMock;
        private readonly Mock<IWkHtmlToImageModule> _imageModuleMock;
        private readonly WkHtmlToXEngine _sut;

        public EngineTest()
        {
            _configuration = new WkHtmlToXConfiguration((int)Environment.OSVersion.Platform, null);
            _libraryLoaderMock = new Mock<ILibraryLoader>();
            _libraryLoaderFactoryMock = new Mock<ILibraryLoaderFactory>();
            _libraryLoaderFactoryMock
                .Setup(l => l.Create(It.IsAny<WkHtmlToXConfiguration>()))
                .Returns(_libraryLoaderMock.Object);

            _pdfModuleMock = new Mock<IWkHtmlToPdfModule>();
            _pdfProcessorMock = new Mock<IPdfProcessor>();
            _pdfProcessorMock.SetupGet(p => p.WkHtmlToPdfModule)
                .Returns(_pdfModuleMock.Object);

            _imageModuleMock = new Mock<IWkHtmlToImageModule>();
            _imageProcessorMock = new Mock<IImageProcessor>();
            _imageProcessorMock.SetupGet(p => p.WkHtmlToImageModule)
                .Returns(_imageModuleMock.Object);

            _sut = new WkHtmlToXEngine(
                _configuration,
                _libraryLoaderFactoryMock.Object,
                _pdfProcessorMock.Object,
                _imageProcessorMock.Object);
        }

        public void Dispose() => _sut.Dispose();

        [Fact]
        public void ShouldThrowExceptionWhenNullConfigurationPassed()
        {
            // Arrange
            Action action = () =>
            {
                using var engine = new WkHtmlToXEngine(
                    null!,
                    new Mock<ILibraryLoaderFactory>().Object,
                    new Mock<IPdfProcessor>().Object,
                    new Mock<IImageProcessor>().Object);
            };

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowExceptionWhenNullLibraryLoaderFactoryPassed()
        {
            // Arrange
            var configuration = new WkHtmlToXConfiguration((int)Environment.OSVersion.Platform, null);
            Action action = () =>
            {
                using var engine = new WkHtmlToXEngine(
                    configuration,
                    null!,
                    new Mock<IPdfProcessor>().Object,
                    new Mock<IImageProcessor>().Object);
            };

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowExceptionWhenNullPdfProcessorPassed()
        {
            // Arrange
            var configuration = new WkHtmlToXConfiguration((int)Environment.OSVersion.Platform, null);
            Action action = () =>
            {
                using var engine = new WkHtmlToXEngine(
                    configuration,
                    new Mock<ILibraryLoaderFactory>().Object,
                    null!,
                    new Mock<IImageProcessor>().Object);
            };

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowExceptionWhenNullImageProcessorPassed()
        {
            // Arrange
            var configuration = new WkHtmlToXConfiguration((int)Environment.OSVersion.Platform, null);
            Action action = () =>
            {
                using var engine = new WkHtmlToXEngine(
                    configuration,
                    new Mock<ILibraryLoaderFactory>().Object,
                    new Mock<IPdfProcessor>().Object,
                    null!);
            };

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ProcessShouldThrowExceptionWhenNullCancellationTokenPassed()
        {
            // Arrange
            Action action = () => _sut.Process(null);

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ProcessShouldThrowExceptionWhenNotCancellationTokenPassed()
        {
            // Arrange
            Action action = () => _sut.Process(new object());

            // Act & Assert
            action.Should().Throw<Exception>();
        }

        [Fact]
        public void ProcessShouldInvokeInitialization()
        {
            // Arrange
            WkHtmlToXConfiguration? result = null;
            _libraryLoaderFactoryMock
                .Setup(l => l.Create(It.IsAny<WkHtmlToXConfiguration>()))
                .Callback<WkHtmlToXConfiguration>(c => result = c)
                .Returns(_libraryLoaderMock.Object);
            _libraryLoaderMock.Setup(l => l.Load());

            _pdfModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);
            _imageModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);

            // Act
            using var source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            _sut.Process(source.Token);

            // Assert
            using (new AssertionScope())
            {
                result.Should().Be(_configuration);
                _libraryLoaderMock.Verify(l => l.Load(), Times.Once);
                _pdfModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
                _imageModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
            }
        }

        [Fact]
        public void ProcessShouldConsumeItemsFromQueue()
        {
            // Arrange
            _libraryLoaderFactoryMock
                .Setup(l => l.Create(It.IsAny<WkHtmlToXConfiguration>()))
                .Returns(_libraryLoaderMock.Object);
            _libraryLoaderMock.Setup(l => l.Load());

            _pdfModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);
            _imageModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);

            _pdfProcessorMock
                .Setup(p => p.Convert(It.IsAny<HtmlToPdfDocument>(), It.IsAny<Func<int, Stream>>()))
                .Returns(true);
            _imageProcessorMock
                .Setup(p => p.Convert(It.IsAny<HtmlToImageDocument>(), It.IsAny<Func<int, Stream>>()))
                .Returns(true);
            var pdfConvertWorkItem = new PdfConvertWorkItem(new HtmlToPdfDocument(), i => Stream.Null);
            var imageConvertWorkItem = new ImageConvertWorkItem(new HtmlToImageDocument(), i => Stream.Null);

            // Act
            _sut.AddConvertWorkItem(pdfConvertWorkItem, CancellationToken.None);
            _sut.AddConvertWorkItem(imageConvertWorkItem, CancellationToken.None);
            using var source = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            _sut.Process(source.Token);

            // Assert
            using (new AssertionScope())
            {
                _libraryLoaderMock.Verify(l => l.Load(), Times.Once);
                _pdfModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
                _imageModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
                _pdfProcessorMock.Verify(
                    p => p.Convert(It.IsAny<HtmlToPdfDocument>(), It.IsAny<Func<int, Stream>>()), Times.Once);
                _imageProcessorMock.Verify(
                    p => p.Convert(It.IsAny<HtmlToImageDocument>(), It.IsAny<Func<int, Stream>>()), Times.Once);
            }
        }

        [Fact]
        public void ProcessShouldConsumeItemsFromQueueAndThrowException()
        {
            // Arrange
            _libraryLoaderFactoryMock
                .Setup(l => l.Create(It.IsAny<WkHtmlToXConfiguration>()))
                .Returns(_libraryLoaderMock.Object);
            _libraryLoaderMock.Setup(l => l.Load());

            _pdfModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);
            _imageModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);

            _pdfProcessorMock
                .Setup(p => p.Convert(It.IsAny<HtmlToPdfDocument>(), It.IsAny<Func<int, Stream>>()))
                .Throws<ArgumentOutOfRangeException>();
            _imageProcessorMock
                .Setup(p => p.Convert(It.IsAny<HtmlToImageDocument>(), It.IsAny<Func<int, Stream>>()))
                .Throws<ArgumentOutOfRangeException>();
            var pdfConvertWorkItem = new PdfConvertWorkItem(new HtmlToPdfDocument(), i => Stream.Null);
            var imageConvertWorkItem = new ImageConvertWorkItem(new HtmlToImageDocument(), i => Stream.Null);

            // Act
            _sut.AddConvertWorkItem(pdfConvertWorkItem, CancellationToken.None);
            _sut.AddConvertWorkItem(imageConvertWorkItem, CancellationToken.None);
            using var source = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            _sut.Process(source.Token);

            Func<Task<bool>> action1 = async () => await pdfConvertWorkItem.TaskCompletionSource.Task.ConfigureAwait(false);
            Func<Task<bool>> action2 = async () => await imageConvertWorkItem.TaskCompletionSource.Task.ConfigureAwait(false);

            // Assert
            using (new AssertionScope())
            {
                _libraryLoaderMock.Verify(l => l.Load(), Times.Once);
                _pdfModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
                _imageModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
                _pdfProcessorMock.Verify(
                    p => p.Convert(It.IsAny<HtmlToPdfDocument>(), It.IsAny<Func<int, Stream>>()), Times.Once);
                _imageProcessorMock.Verify(
                    p => p.Convert(It.IsAny<HtmlToImageDocument>(), It.IsAny<Func<int, Stream>>()), Times.Once);
                action1.Should().Throw<ArgumentOutOfRangeException>();
                action2.Should().Throw<ArgumentOutOfRangeException>();
            }
        }

        [Fact]
        public void InitializeInProcessingThreadShouldWork()
        {
            // Arrange
            WkHtmlToXConfiguration? result = null;
            _libraryLoaderFactoryMock
                .Setup(l => l.Create(It.IsAny<WkHtmlToXConfiguration>()))
                .Callback<WkHtmlToXConfiguration>(c => result = c)
                .Returns(_libraryLoaderMock.Object);
            _libraryLoaderMock.Setup(l => l.Load());

            _pdfModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);
            _imageModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);

            // Act
            _sut.InitializeInProcessingThread();

            // Assert
            using (new AssertionScope())
            {
                result.Should().Be(_configuration);
                _libraryLoaderMock.Verify(l => l.Load(), Times.Once);
                _pdfModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
                _imageModuleMock.Verify(l => l.Initialize(It.IsAny<int>()), Times.Once);
            }
        }

        [Fact]
        public void InitializeInProcessingThreadShouldThrowExceptionWhenPdfInitializationFailed()
        {
            // Arrange
            _libraryLoaderMock.Setup(l => l.Load());

            _pdfModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(0);
            _imageModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);

            // Act
            Action action = () => _sut.InitializeInProcessingThread();

            // Assert
            action.Should().Throw<PdfModuleInitializationException>();
        }

        [Fact]
        public void InitializeInProcessingThreadShouldThrowExceptionWhenImageInitializationFailed()
        {
            // Arrange
            _libraryLoaderMock.Setup(l => l.Load());

            _pdfModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(1);
            _imageModuleMock.Setup(p => p.Initialize(It.IsAny<int>()))
                .Returns(0);

            // Act
            Action action = () => _sut.InitializeInProcessingThread();

            // Assert
            action.Should().Throw<ImageModuleInitializationException>();
        }
    }
}
