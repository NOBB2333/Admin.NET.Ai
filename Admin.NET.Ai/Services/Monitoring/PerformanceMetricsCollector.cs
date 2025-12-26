using System.Diagnostics.Metrics;

namespace Admin.NET.Ai.Services.Monitoring;

public class PerformanceMetricsCollector
{
    private readonly Counter<int> _requestCounter;
    private readonly Histogram<double> _responseTimeHistogram;
    private readonly Counter<int> _errorCounter;
    
    public PerformanceMetricsCollector(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Microsoft.AgentFramework");
        
        _requestCounter = meter.CreateCounter<int>("agent.requests.total", 
            description: "Total agent requests");
            
        _responseTimeHistogram = meter.CreateHistogram<double>("agent.response.time",
            unit: "ms", description: "Agent response time distribution");
            
        _errorCounter = meter.CreateCounter<int>("agent.errors.total",
            description: "Total agent errors");
    }
    
    public async Task<T> TrackAgentExecutionAsync<T>(
        string agentName, 
        Func<Task<T>> operation)
    {
        var startTime = DateTime.UtcNow;
        _requestCounter.Add(1, new KeyValuePair<string, object?>("agent", agentName));
        
        try
        {
            var result = await operation();
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _responseTimeHistogram.Record(duration, 
                new KeyValuePair<string, object?>("agent", agentName),
                new KeyValuePair<string, object?>("success", true));
                
            return result;
        }
        catch (Exception ex)
        {
            _errorCounter.Add(1, 
                new KeyValuePair<string, object?>("agent", agentName),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            throw;
        }
    }
}
