namespace AnvilTests;

public sealed class JavaIntegrationFactAttribute : FactAttribute
{
    public const string EnabledEnvironmentVariable = "ANVIL_RUN_JAVA_INTEGRATION_TESTS";

    public JavaIntegrationFactAttribute()
    {
        if (!IsEnabled())
        {
            Skip =
                $"Set {EnabledEnvironmentVariable}=1 to run Java toolchain integration tests.";
        }
    }

    private static bool IsEnabled()
    {
        var value = Environment.GetEnvironmentVariable(EnabledEnvironmentVariable);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
