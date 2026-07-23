using System.Diagnostics;

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

    [Fact]
    public void Write_SameBuilderTwice_ProducesStableBytecode()
    {
        using var input = GetResource("Test.class");
        var builder = ClassBuilder.Read(input);
        using var first = new MemoryStream();
        using var second = new MemoryStream();

        builder.Write(first);
        builder.Write(second);

        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    public void RoundTripTestClass_ShouldPassJvmVerificationWhenJavaIsAvailable()
    {
        if (!CanRunJava())
        {
            return;
        }

        var directory = Path.Combine(
            Path.GetTempPath(),
            $"anvil-jvm-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var classNames = new[]
            {
                "MathOperation.class",
                "State.class",
                "Test$1.class",
                "Test$1LocalClass.class",
                "Test$InnerMember.class",
                "Test$StaticNested.class",
                "TestMetadata.class"
            };
            foreach (var className in classNames)
            {
                using var input = GetResource(className);
                var builder = ClassBuilder.Read(input);
                using var output = File.Create(Path.Combine(directory, className));
                builder.Write(output);
            }

            using (var input = GetResource("Test.class"))
            {
                var builder = ClassBuilder.Read(input);
                using var output = File.Create(Path.Combine(directory, "Test.class"));
                builder.Write(output);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "java",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-Xverify:all");
            startInfo.ArgumentList.Add("-cp");
            startInfo.ArgumentList.Add(directory);
            startInfo.ArgumentList.Add("Test");

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            process!.WaitForExit();
            var standardError = process.StandardError.ReadToEnd();

            Assert.True(
                process.ExitCode == 0,
                $"JVM verification failed with exit code {process.ExitCode}: {standardError}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static bool CanRunJava()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "java",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-version");

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
