using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;

var today = new DateOnly(2026, 11, 9);
var random = new Random(3222);
var course = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
var tasks = Enumerable.Range(0, 10000).Select(i => new StudyTask(new Guid(i, 0, 0, new byte[8]), course,
    $"Task {i:D5}", today.AddDays(random.Next(-7, 90)), random.Next(1, 6), random.Next(15, 1441))).ToArray();
for (var i = 0; i < 5; i++) TaskRules.Rank(tasks, today);
var measurements = new List<double>();
var allocations = new List<long>();
for (var i = 0; i < 25; i++)
{
    var startBytes = GC.GetAllocatedBytesForCurrentThread();
    var stopwatch = Stopwatch.StartNew();
    var result = TaskRules.Rank(tasks, today);
    stopwatch.Stop();
    GC.KeepAlive(result);
    measurements.Add(stopwatch.Elapsed.TotalMilliseconds);
    allocations.Add(GC.GetAllocatedBytesForCurrentThread() - startBytes);
}
var ordered = measurements.Order().ToArray();
Console.WriteLine(JsonSerializer.Serialize(new
{
    Framework = RuntimeInformation.FrameworkDescription,
    OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    TaskCount = tasks.Length,
    Warmups = 5,
    Iterations = 25,
    MedianMilliseconds = ordered[12],
    P95Milliseconds = ordered[23],
    MaxMilliseconds = ordered[^1],
    MeanAllocatedBytes = allocations.Average(),
    Scope = "In-process priority ranking only; excludes database, network and browser"
}, new JsonSerializerOptions { WriteIndented = true }));
