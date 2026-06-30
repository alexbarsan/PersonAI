namespace DreamLens.Api.Tests;

public sealed class ApiAssemblyTests
{
    [Fact]
    public void ApiAssemblyUsesExpectedName()
    {
        Assert.Equal("DreamLens.Api", typeof(Program).Assembly.GetName().Name);
    }
}
