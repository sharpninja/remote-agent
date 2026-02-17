using FluentAssertions;
using RemoteAgent.Desktop.Logging;

namespace RemoteAgent.Desktop.UiTests;

public sealed class StructuredLogStoreTests
{
    [Fact]
    public void Query_ShouldApplyFilterCriteria()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"desktop-log-tests-{Guid.NewGuid():N}.db");
        try
        {
            var store = new DesktopStructuredLogStore(dbPath);
            store.UpsertBatch(
            [
                new DesktopStructuredLogRecord
                {
                    EventId = 1,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = "INFO",
                    EventType = "session_started",
                    Message = "session opened",
                    SessionId = "s1",
                    CorrelationId = "c1",
                    Component = "AgentGatewayService",
                    SourceHost = "127.0.0.1",
                    SourcePort = 5243
                },
                new DesktopStructuredLogRecord
                {
                    EventId = 2,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = "WARN",
                    EventType = "session_limit_exceeded",
                    Message = "limit reached",
                    SessionId = "s2",
                    CorrelationId = "c2",
                    Component = "SessionCapacityService",
                    SourceHost = "127.0.0.1",
                    SourcePort = 5243
                }
            ]);

            var filter = new DesktopStructuredLogFilter
            {
                Level = "warn",
                SessionId = "s2",
                SourceHost = "127.0.0.1"
            };

            var rows = store.Query(filter, limit: 20);
            rows.Should().HaveCount(1);
            rows[0].EventId.Should().Be(2);
            rows[0].EventType.Should().Be("session_limit_exceeded");
        }
        finally
        {
            try { File.Delete(dbPath); } catch { }
        }
    }
}
