using Anvil.Constants.Flags;
using Anvil.Core;
using Anvil.Instructions;
using Anvil.Structures;

namespace AnvilTests;

[Trait("Category", "JavaIntegration")]
public class JavaToolIntegrationTests
{
    private const string HelloWorldOutput = "Hello, AnvilASM!";

    [JavaIntegrationFact]
    public async Task BuildHelloWorldFromScratch_ExecutesAndCanBeDisassembled()
    {
        var directory = CreateTemporaryDirectory("anvil-hello-world");

        try
        {
            const string className = "HelloWorld";
            var builder = ClassBuilder.Create(majorVersion: 65);
            builder.Name = className;
            builder.SuperName = "java/lang/Object";
            builder.SourceFile = $"{className}.java";
            builder.ClassFile.AccessFlags = ClassAccessFlags.Public | ClassAccessFlags.Super;

            var main = builder.AddMethod(
                "main",
                "([Ljava/lang/String;)V",
                MethodAccessFlags.Public | MethodAccessFlags.Static);
            main.Body = new MethodBody
            {
                Instructions =
                [
                    new FieldInstruction(
                        OperationCode.GETSTATIC,
                        "java/lang/System",
                        "out",
                        "Ljava/io/PrintStream;"),
                    new LdcInstruction(HelloWorldOutput),
                    new MethodInstruction(
                        OperationCode.INVOKEVIRTUAL,
                        "java/io/PrintStream",
                        "println",
                        "(Ljava/lang/String;)V"),
                    new InsnInstruction(OperationCode.RETURN)
                ]
            };

            var classFilePath = Path.Combine(directory, $"{className}.class");
            await using (var output = File.Create(classFilePath))
            {
                builder.Write(output);
            }

            var execution = await JavaToolRunner.RunAsync(
                "java",
                "-Xverify:all",
                "-cp",
                directory,
                className);

            AssertSuccessful(execution, "java");
            Assert.Equal(HelloWorldOutput, execution.StandardOutput.Trim());

            var disassembly = await JavaToolRunner.RunAsync(
                "javap",
                "-classpath",
                directory,
                "-c",
                "-verbose",
                className);

            AssertSuccessful(disassembly, "javap");
            Assert.Contains("major version: 65", disassembly.StandardOutput);
            Assert.Contains("getstatic", disassembly.StandardOutput);
            Assert.Contains("ldc", disassembly.StandardOutput);
            Assert.Contains("invokevirtual", disassembly.StandardOutput);
            Assert.Contains(HelloWorldOutput, disassembly.StandardOutput);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [JavaIntegrationFact]
    public async Task JavacControlFlowClass_RoundTripsAndExecutes()
    {
        var directory = CreateTemporaryDirectory("anvil-javac-control-flow");

        try
        {
            const string className = "CompiledFixture";
            var sourcePath = Path.Combine(directory, $"{className}.java");
            var compiledDirectory = Path.Combine(directory, "compiled");
            var roundTrippedDirectory = Path.Combine(directory, "round-tripped");
            Directory.CreateDirectory(compiledDirectory);
            Directory.CreateDirectory(roundTrippedDirectory);

            await File.WriteAllTextAsync(sourcePath, """
                public class CompiledFixture {
                    static int dense(int value) {
                        return switch (value) {
                            case 0 -> 10;
                            case 1 -> 20;
                            case 2 -> 30;
                            default -> -1;
                        };
                    }

                    static int sparse(int value) {
                        return switch (value) {
                            case 10 -> 1;
                            case 1000 -> 2;
                            case 100000 -> 3;
                            default -> 0;
                        };
                    }

                    public static void main(String[] args) {
                        int total = 0;
                        for (int i = 0; i < 3; i++) {
                            total += dense(i);
                        }

                        try {
                            throw new IllegalStateException("boom");
                        } catch (IllegalStateException exception) {
                            System.out.println((total + sparse(1000)) + ":" + exception.getMessage());
                        }
                    }
                }
                """);

            var compilation = await JavaToolRunner.RunAsync(
                "javac",
                "--release",
                "21",
                "-g",
                "-d",
                compiledDirectory,
                sourcePath);
            AssertSuccessful(compilation, "javac");

            var compiledClassPath = Path.Combine(compiledDirectory, $"{className}.class");
            var roundTrippedClassPath =
                Path.Combine(roundTrippedDirectory, $"{className}.class");

            await using (var input = File.OpenRead(compiledClassPath))
            {
                var builder = ClassBuilder.Read(input);
                foreach (var method in builder.Methods)
                {
                    if (method.Body is null)
                    {
                        continue;
                    }

                    method.Body.MaxStack = 0;
                    method.Body.MaxLocals = 0;
                }

                await using var output = File.Create(roundTrippedClassPath);
                builder.Write(output);
            }

            var execution = await JavaToolRunner.RunAsync(
                "java",
                "-Xverify:all",
                "-cp",
                roundTrippedDirectory,
                className);

            AssertSuccessful(execution, "java");
            Assert.Equal("62:boom", execution.StandardOutput.Trim());

            var disassembly = await JavaToolRunner.RunAsync(
                "javap",
                "-classpath",
                roundTrippedDirectory,
                "-c",
                className);

            AssertSuccessful(disassembly, "javap");
            Assert.Contains("tableswitch", disassembly.StandardOutput);
            Assert.Contains("lookupswitch", disassembly.StandardOutput);
            Assert.Contains("Exception table:", disassembly.StandardOutput);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void AssertSuccessful(
        (int ExitCode, string StandardOutput, string StandardError) result,
        string tool)
    {
        Assert.True(
            result.ExitCode == 0,
            $"{tool} failed with exit code {result.ExitCode}.{Environment.NewLine}"
            + $"stdout:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}"
            + $"stderr:{Environment.NewLine}{result.StandardError}");
    }
}
