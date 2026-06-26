# AGENTS.md

## Project

AnvilASM — a C#/.NET library for reading, modifying, and generating JVM bytecode (`.class` files). Pure library, no executable entrypoint.

- **Target:** `net10.0` (requires .NET 10 SDK)
- **JVM spec target:** Java / JDK 26 (JVM Specification 26)
- **Test framework:** xUnit 2.9.3
- **License:** Apache 2.0

## Commands

```bash
dotnet build                  # Build the solution
dotnet test                   # Run all tests
dotnet test --filter "FullyQualifiedName~TestClass.TestMethod"  # Run a single test
dotnet restore                # Restore NuGet packages
```

No linting, formatting, or typecheck commands are configured beyond what the compiler provides.

## Architecture

**Two-layer design (incomplete):**
- **Low-Level** (implemented): Direct mapping of JVM ClassFile binary structure — `Structures/`, `Interfaces/`, `Types/`. Serializes/deserializes to/from raw bytes.
- **High-Level** (not yet built): User-facing object model. Empty directories `Core/`, `IO/`, `Models/`, `Serialization/` are declared in `Anvil.csproj` as placeholders. The README references `ClassNode`, `MethodNode`, etc. but these do not exist yet.

## Key conventions

- **Big-endian everywhere.** All JVM data is big-endian. The `IType<TSelf>` interface and all types in `Types/` (`TUByte`, `TUShort`, `TUInt`, `TInt`, `TLong`, `TFloat`, `TDouble`, `TShort`, `TBoolean`) encapsulate this. Never assume little-endian when reading/writing streams.
- **CRTP with static abstracts.** `IType<TSelf>` and `IStructure<TSelf>` use F-bounded generics with `static abstract TSelf Read(Stream)` — a .NET 7+ feature. Implementations must provide their own `Read` static method.
- **Constant pool quirks:**
  - The pool is `CpInfo?[]` — index 0 is always `null` (per JVM spec).
  - `Long` and `Double` entries consume two CP slots; the second slot is `null`.
  - `CpInfo.Read()` uses a factory dispatch pattern to deserialize the correct subclass based on `ConstantPoolTag`.
- **File-scoped namespaces** are used consistently (no block-scoped namespaces).
- **`ClassFile.Read(Stream)`** is the primary entrypoint for parsing a `.class` file.
- **`MethodBody`** is the main class for instruction-level work: label-based branch resolution (not raw offsets), opcode normalization (`ILOAD_0` → `ILOAD 0`), and automatic `GOTO` → `GOTO_W` widening.

## Testing

- The test project (`AnvilTests/`) has xUnit scaffolding but **zero test source files**. Do not be surprised by `dotnet test` passing trivially.
- `Test.java` at the repo root is sample Java source (exercises many JVM features) to compile into `.class` files as test input data — it is not a unit test.

## Dependencies note

The library references both `Newtonsoft.Json` (13.0.4) and `System.Text.Json` (10.0.1) plus `Newtonsoft.Json.Bson`. Be aware of the dual JSON libraries — check existing usage before writing new JSON code to match the pattern used in context.

## CI / hooks

No CI/CD pipelines, no git hooks, no pre-commit config. Manual build and test only.
