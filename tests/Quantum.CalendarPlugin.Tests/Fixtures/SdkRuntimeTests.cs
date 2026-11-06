using Quantum.Plugin.Abstraction;
using System.Reflection;

namespace Quantum.CalendarPlugin.Tests.Fixtures;

public sealed class SdkRuntimeTests
{
    [Fact]
    public void RuntimeLoadsTheSdkSelectedByTheCompatibilityMatrix()
    {
        var expected = Environment.GetEnvironmentVariable("QUANTUM_TEST_SDK_VERSION") ?? "0.3.0-rc.3";
        var actual = typeof(IQuantumPlugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
        Assert.Equal(expected, actual);
    }
}
