using System;
using NSubstitute;
using Sentry.Infrastructure;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggerProviderTests
    {
        private class Fixture
        {
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public ISystemClock Clock { get; set; } = Substitute.For<ISystemClock>();
            public SentryLoggingOptions SentryLoggingOptions { get; set; } = new SentryLoggingOptions();
            public SentryLoggerProvider GetSut() => new SentryLoggerProvider(Hub, Clock, SentryLoggingOptions);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void CreateLogger_LoggerType_SentryLogger()
        {
            var sut = _fixture.GetSut();

            Assert.IsType<SentryLogger>(sut.CreateLogger("category"));
        }

        [Fact]
        public void CreateLogger_Category_AsProvided()
        {
            var expectedCategory = nameof(SentryLoggerProviderTests);

            var sut = _fixture.GetSut();

            var actual = (SentryLogger)sut.CreateLogger(expectedCategory);

            Assert.Equal(expectedCategory, actual.CategoryName);
        }

        [Fact]
        public void Ctor_DisabledHub_DoesNotCreatesScope()
        {
            _fixture.Hub.IsEnabled.Returns(false);
            _fixture.GetSut();
            _fixture.Hub.DidNotReceive().PushScope();
        }

        [Fact]
        public void Ctor_EnabledHub_CreatesScope()
        {
            _fixture.Hub.IsEnabled.Returns(true);
            _fixture.GetSut();
            _fixture.Hub.Received(1).PushScope();
        }

        [Fact]
        public void Dispose_DisposesNewScope()
        {
            _fixture.Hub.IsEnabled.Returns(true);
            var disposable = Substitute.For<IDisposable>();
            _fixture.Hub.PushScope().Returns(disposable);

            var sut = _fixture.GetSut();

            sut.Dispose();

            disposable.Received(1).Dispose();
        }

        [Fact]
        public void NameAndVersion_Name_NotNull() => Assert.NotNull(SentryLoggerProvider.NameAndVersion.Name);

        [Fact]
        public void NameAndVersion_Version_NotNull() => Assert.NotNull(SentryLoggerProvider.NameAndVersion.Version);

        [Fact]
        public void Ctor_ScopeSdk_ContainNameAndVersion()
        {
            _fixture.Hub.IsEnabled.Returns(true);
            var scope = new Scope(new SentryOptions());

            _fixture.Hub.When(w => w.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(info => info.Arg<Action<Scope>>()(scope));

            _fixture.GetSut();

            Assert.Equal(SentryLoggerProvider.NameAndVersion.Name, scope.Sdk.Name);
            Assert.Equal(SentryLoggerProvider.NameAndVersion.Version, scope.Sdk.Version);
        }
    }
}
