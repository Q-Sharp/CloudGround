namespace CloudGround.Templates.Tests;

public static class SkillScaffold
{
    private const string ModuleToken = "ModuleName";
    private const string LowercaseModuleToken = "modulename";
    private const string ModulePlaceholder = "<Module>";
    private const string GlobalUsingsFile = "GlobalUsings.cs";

    public static async Task ApplyModuleAsync(GeneratedSolution solution, string module)
    {
        CopyReplacingTokens(SkillDirectory(solution, "add-module", "src"), Path.Combine(solution.Root, "src"), module);
        CopyReplacingTokens(SkillDirectory(solution, "add-module", "tests"), Path.Combine(solution.Root, "tests"), module);

        var modulePrefix = $"{solution.Name}.Modules.{module}";
        var moduleProject = ModuleProject(solution, module);
        await AssertSucceedsAsync(solution, "sln", "add", moduleProject, $"tests/{modulePrefix}.Tests/{modulePrefix}.Tests.csproj");
        await AssertSucceedsAsync(solution, "reference", "add", moduleProject, "--project", $"src/{solution.Name}.Api/{solution.Name}.Api.csproj");

        var api = Path.Combine(solution.Root, "src", $"{solution.Name}.Api");
        AddGlobalUsings(Path.Combine(api, GlobalUsingsFile), modulePrefix);

        var program = Path.Combine(api, "Program.cs");
        InsertAfterLast(program, line => line == "builder.AddApiAuthentication();" || IsModuleCall(line, "builder.Add"), $"builder.Add{module}Module();");
        InsertAfterLast(program, line => line == "app.MapDefaultEndpoints();" || IsModuleCall(line, "app.Map"), $"app.Map{module}Module();");

        var appHost = Path.Combine(solution.Root, "src", $"{solution.Name}.AppHost", "AppHost.cs");
        InsertBefore(appHost, "builder.TagImagesWithVersion();", $"api.WithModuleMigrations(\"{module}\", database);", string.Empty);
    }

    public static Task ApplyExampleFeatureAsync(GeneratedSolution solution, string module)
    {
        var modulePrefix = $"{solution.Name}.Modules.{module}";
        var moduleDirectory = Path.Combine(solution.Root, "src", "Modules", modulePrefix);
        CopyReplacingTokens(SkillDirectory(solution, "add-feature", "example/module"), moduleDirectory, module);
        CopyReplacingTokens(SkillDirectory(solution, "add-feature", "example/module-tests"), Path.Combine(solution.Root, "tests", $"{modulePrefix}.Tests"), module);
        CopyReplacingTokens(SkillDirectory(solution, "add-feature", "example/integration-tests"), Path.Combine(solution.Root, "tests", $"{solution.Name}.Integration.Tests"), module);

        AddGlobalUsings(
            Path.Combine(moduleDirectory, GlobalUsingsFile),
            ImportsInFeatureSkill(solution, $"src/Modules/{solution.Name}.Modules.{ModulePlaceholder}/{GlobalUsingsFile}", module));
        AddGlobalUsings(
            Path.Combine(solution.Root, "tests", $"{modulePrefix}.Tests", GlobalUsingsFile),
            ImportsInFeatureSkill(solution, $"tests/{solution.Name}.Modules.{ModulePlaceholder}.Tests/{GlobalUsingsFile}", module));

        var context = Path.Combine(moduleDirectory, "Persistence", $"{module}DbContext.cs");
        InsertAfterLast(context, line => line.Contains("public const string Schema", StringComparison.Ordinal), string.Empty, "    public DbSet<Item> Items => Set<Item>();");

        var moduleClass = Path.Combine(moduleDirectory, $"{module}Module.cs");
        InsertAfterLast(moduleClass, line => line.Contains("TryAddSingleton(TimeProvider.System);", StringComparison.Ordinal), "        builder.Services.AddScoped<CreateItemHandler>();", "        builder.Services.AddScoped<GetItemHandler>();");
        ReplaceLine(moduleClass, $"            .WithTags(\"{module}\");", $"            .WithTags(\"{module}\")", "            .MapCreateItem()", "            .MapGetItem();");

        return Task.CompletedTask;
    }

    public static async Task AddContractsAsync(GeneratedSolution solution, string module)
    {
        var contracts = $"src/Modules/{solution.Name}.Modules.{module}.Contracts/{solution.Name}.Modules.{module}.Contracts.csproj";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(solution.Root, contracts))!);
        File.WriteAllText(Path.Combine(solution.Root, contracts), $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}{Environment.NewLine}</Project>{Environment.NewLine}");

        await AssertSucceedsAsync(solution, "sln", "add", contracts);
        await AssertSucceedsAsync(solution, "reference", "add", contracts, "--project", ModuleProject(solution, module));
    }

    public static async Task<CliResult> AssertSucceedsAsync(GeneratedSolution solution, params string[] arguments)
    {
        var result = await DotnetCli.RunAsync(solution.Root, arguments);
        Assert.True(result.ExitCode == 0, $"dotnet {string.Join(' ', arguments)}: {result.Output}");
        return result;
    }

    private static string ModuleProject(GeneratedSolution solution, string module) =>
        $"src/Modules/{solution.Name}.Modules.{module}/{solution.Name}.Modules.{module}.csproj";

    private static string SkillDirectory(GeneratedSolution solution, string skill, string folder)
    {
        var directory = Path.Combine(solution.Root, ".claude", "skills", skill, folder);
        Assert.True(Directory.Exists(directory), $"{directory} does not exist.");
        return directory;
    }

    private static void CopyReplacingTokens(string source, string target, string module)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, ReplaceTokens(Path.GetRelativePath(source, file), module));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, ReplaceTokens(File.ReadAllText(file), module));
        }
    }

    private static string ReplaceTokens(string text, string module) =>
        text.Replace(ModuleToken, module, StringComparison.Ordinal)
            .Replace(LowercaseModuleToken, module.ToLowerInvariant(), StringComparison.Ordinal);

    private static bool IsModuleCall(string line, string start) =>
        line.StartsWith(start, StringComparison.Ordinal) && line.EndsWith("Module();", StringComparison.Ordinal);

    private static List<string> ImportsInFeatureSkill(GeneratedSolution solution, string file, string module)
    {
        var lines = File.ReadAllLines(Path.Combine(solution.Root, ".claude", "skills", "add-feature", "SKILL.md"));
        var bullet = Array.FindIndex(lines, line => line.Contains($"`{file}`", StringComparison.Ordinal));
        Assert.True(bullet >= 0, $"The add-feature skill lists no imports for {file}.");

        var imports = string.Concat(lines.Skip(bullet + 1).TakeWhile(line => !line.TrimStart().StartsWith('-')))
            .Split('`')
            .Where((_, index) => index % 2 == 1)
            .Select(name => name.Replace(ModulePlaceholder, module, StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(imports);
        return imports;
    }

    private static void AddGlobalUsings(string file, params IEnumerable<string> namespaces)
    {
        var directives = File.ReadAllLines(file)
            .Concat(namespaces.Select(name => $"global using {name};"))
            .Distinct()
            .OrderByDescending(directive => directive.StartsWith("global using System", StringComparison.Ordinal))
            .ThenBy(directive => directive.TrimEnd(';'), StringComparer.Ordinal)
            .ToList();
        File.WriteAllLines(file, directives);
    }

    private static void InsertAfterLast(string file, Func<string, bool> anchor, params string[] inserted)
    {
        var lines = File.ReadAllLines(file).ToList();
        var index = lines.FindLastIndex(candidate => anchor(candidate));
        Assert.True(index >= 0, $"{file} has no anchor for `{inserted.Last()}`.");
        lines.InsertRange(index + 1, inserted);
        File.WriteAllLines(file, lines);
    }

    private static void ReplaceLine(string file, string line, params string[] replacement)
    {
        var lines = File.ReadAllLines(file).ToList();
        var index = lines.IndexOf(line);
        Assert.True(index >= 0, $"{file} has no line `{line}`.");
        lines.RemoveAt(index);
        lines.InsertRange(index, replacement);
        File.WriteAllLines(file, lines);
    }

    private static void InsertBefore(string file, string anchor, params string[] inserted)
    {
        var lines = File.ReadAllLines(file).ToList();
        var index = lines.IndexOf(anchor);
        Assert.True(index >= 0, $"{file} has no line `{anchor}`.");
        lines.InsertRange(index, inserted);
        File.WriteAllLines(file, lines);
    }
}
