using System.Reflection;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Communication.EdgeAdapter.Demo.Nodes;
using Xunit;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Tests.Nodes;

/// <summary>
///     AB#5228 — guard: every trigger this adapter ships must state whether it is process-bound.
/// </summary>
/// <remarks>
///     <para>
///         A trigger whose firing depends on THIS adapter process staying alive (an in-process
///         polling loop, an in-memory event subscription, a socket the process itself listens on)
///         MUST carry <c>[NodeRequiresRunningProcess]</c>. Without it the communication controller
///         classifies the workload on-demand capable, hibernation scales it to zero, and the trigger
///         stops firing with no error and no alarm — data simply stops arriving (Epic AB#4914).
///     </para>
///     <para>
///         🔴 <c>ReviewedWakeCapableTriggers</c> is the only escape, and an entry on it is a
///         reviewed claim that something OUTSIDE this process can wake a hibernated workload, with
///         the wake path named. A new trigger that is neither marked nor listed fails here rather
///         than shipping a silent hibernation bug.
///     </para>
/// </remarks>
public class TriggerProcessBoundGuardTests
{
    /// <summary>
    ///     Triggers reviewed and found NOT process-bound, keyed by "Name@Version", value = wake path.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> ReviewedWakeCapableTriggers =
        new Dictionary<string, string>
        {
            // (empty — every trigger in this assembly is process-bound)
        };

    public static TheoryData<string> TriggerConfigurationNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in AllTriggerConfigurations().Select(QualifiedName).Order())
        {
            data.Add(name);
        }

        return data;
    }

    private static IEnumerable<Type> AllTriggerConfigurations()
    {
        // This adapter's own node assembly only — never the test assembly.
        return typeof(DemoTriggerNodeConfiguration).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(ITriggerNodeConfiguration).IsAssignableFrom(t)
                        && t.GetCustomAttribute<NodeNameAttribute>() != null);
    }

    private static string QualifiedName(Type configurationType)
    {
        var nodeName = configurationType.GetCustomAttribute<NodeNameAttribute>()!;
        return $"{nodeName.Name}@{nodeName.Version}";
    }

    [Theory]
    [MemberData(nameof(TriggerConfigurationNames))]
    public void EveryTriggerConfiguration_IsEitherProcessBoundOrOnTheReviewedAllowList(string qualifiedName)
    {
        var configurationType = AllTriggerConfigurations().Single(t => QualifiedName(t) == qualifiedName);
        var isProcessBound =
            configurationType.GetCustomAttribute<NodeRequiresRunningProcessAttribute>() != null;
        var isReviewedWakeCapable = ReviewedWakeCapableTriggers.ContainsKey(qualifiedName);

        Assert.True(isProcessBound || isReviewedWakeCapable,
            $"Trigger '{qualifiedName}' ({configurationType.FullName}) carries neither " +
            $"[{nameof(NodeRequiresRunningProcessAttribute)}] nor an entry on the reviewed " +
            "wake-capable allow-list in this test. Read its StartAsync/StopAsync: if it only fires " +
            "while the adapter process is alive, add the attribute; if something external can wake a " +
            "hibernated workload, add it to ReviewedWakeCapableTriggers WITH the wake path named. " +
            "Getting this wrong silently stops the trigger under scale-to-zero (AB#4914 / AB#5228).");

        Assert.False(isProcessBound && isReviewedWakeCapable,
            $"Trigger '{qualifiedName}' is marked [{nameof(NodeRequiresRunningProcessAttribute)}] " +
            "and is also on the reviewed wake-capable allow-list. Exactly one of the two is true.");
    }

    [Fact]
    public void AllowList_HasNoStaleEntries()
    {
        var known = AllTriggerConfigurations().Select(QualifiedName).ToHashSet();
        var stale = ReviewedWakeCapableTriggers.Keys.Where(k => !known.Contains(k)).ToList();

        Assert.True(stale.Count == 0,
            $"Allow-list entries with no matching trigger configuration: {string.Join(", ", stale)}. " +
            "A renamed or deleted trigger must not leave a blanket exemption behind.");
    }

    [Fact]
    public void AdapterDeclaresAtLeastOneTrigger()
    {
        // Cheap canary: a reflection guard that silently finds nothing proves nothing.
        Assert.NotEmpty(AllTriggerConfigurations());
    }

    [Fact]
    public void DemoTrigger_V1_IsProcessBound()
    {
        // TCP listener opened by this process — a hibernated workload just refuses the connection.
        Assert.Contains("DemoTrigger@1", AllTriggerConfigurations()
            .Where(t => t.GetCustomAttribute<NodeRequiresRunningProcessAttribute>() != null)
            .Select(QualifiedName));
    }
}
