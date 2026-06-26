using Anvil.Core;
using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;
using Anvil.Structures;
using Anvil.Structures.Attributes;

namespace AnvilTests;

public class IntegrationTests
{
    private static Stream GetResource(string name) =>
        typeof(IntegrationTests).Assembly.GetManifestResourceStream($"AnvilTests.Resources.{name}")!;

    [Fact]
    public void ReadTestClass_ShouldParseSuccessfully()
    {
        using var stream = GetResource("Test.class");
        var builder = ClassBuilder.Read(stream);

        Assert.NotNull(builder.ClassFile);
        Assert.NotEmpty(builder.Methods);
        Assert.Contains(builder.Methods, m => m.Name == "testControlFlow");
        Assert.Contains(builder.Methods, m => m.Name == "testLambdaAndStreams");
        Assert.Contains(builder.Methods, m => m.Body != null);
        Assert.Contains(builder.Methods, m => m.Body == null); // native method has no body
    }

    [Fact]
    public void RoundTripTestClass_ShouldBeVerifiable()
    {
        using var stream = GetResource("Test.class");
        var builder = ClassBuilder.Read(stream);

        using var outStream = new MemoryStream();
        builder.Write(outStream);

        outStream.Position = 0;
        var roundTripped = ClassBuilder.Read(outStream);

        Assert.Equal(builder.Methods.Count, roundTripped.Methods.Count);
    }

    [Fact]
    public void ModifyMethodAndRebuild_ShouldGenerateValidBytecode()
    {
        using var stream = GetResource("Test.class");
        var builder = ClassBuilder.Read(stream);

        var method = builder.Methods.FirstOrDefault(m => m.Name == "testControlFlow" && m.Body != null);
        Assert.NotNull(method);
        Assert.NotNull(method!.Body);

        var body = method.Body;
        body.Normalize();

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.NotNull(codeAttr);
        Assert.NotEmpty(codeAttr.Code);
        Assert.NotEmpty(codeAttr.Attributes);
    }

    [Fact]
    public void ToCodeAttribute_ShouldIncludeExceptionTable()
    {
        using var stream = GetResource("Test.class");
        var builder = ClassBuilder.Read(stream);

        var method = builder.Methods.FirstOrDefault(m => m.Name == "testExceptions" && m.Body != null);
        Assert.NotNull(method);

        var body = method!.Body!;
        body.Normalize();

        var cp = new ConstantPoolBuilder();
        var codeAttr = body.ToCodeAttribute(cp);

        Assert.NotEmpty(body.TryCatchBlocks);
        Assert.NotEmpty(codeAttr.ExceptionTable);
    }

    [Fact]
    public void FromCodeAttribute_ShouldParseInstructions()
    {
        using var stream = GetResource("Test.class");
        var cf = ClassFile.Read(stream);

        var codeAttr = cf.Methods
            .SelectMany(m => m.Attributes)
            .Select(a => a.ResolveBody(cf.ConstantPool))
            .OfType<CodeAttribute>()
            .First();

        var body = MethodBody.FromCodeAttribute(codeAttr, cf.ConstantPool);

        Assert.NotEmpty(body.Instructions);
    }

    [Fact]
    public void AllClassFilesInResources_ShouldRoundTrip()
    {
        var classNames = new[]
        {
            "Test.class", "MathOperation.class", "State.class", "TestMetadata.class",
            "Test$StaticNested.class", "Test$InnerMember.class",
        };

        foreach (var name in classNames)
        {
            using var stream = GetResource(name);
            var builder = ClassBuilder.Read(stream);

            using var outStream = new MemoryStream();
            builder.Write(outStream);
            outStream.Position = 0;

            var rtBuilder = ClassBuilder.Read(outStream);
            Assert.Equal(builder.Methods.Count, rtBuilder.Methods.Count);
        }
    }
}
