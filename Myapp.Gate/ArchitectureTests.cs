using System.Reflection;
using Myapp.Server.Api;
using Suwgate.Architecture.Build;
using Suwgate.Architecture.Code;
using Suwgate.Architecture.Design;
using Suwgate.Architecture.Sources;
using Suwgate.Architecture.Verification;
using Suwgate.Database.Migrations;

namespace Myapp.Gate;

public sealed class ArchitectureTests
{
    private static readonly RepositoryFiles Repository = RepositoryFiles.Find();
    private static readonly LayersFile LayersFile = LayersFile.Read(Repository, "layers.json");
    private static readonly Assembly Server = typeof(ServerApp).Assembly;
    private static readonly Assembly[] Assemblies = [Server, typeof(ArchitectureTests).Assembly];

    [Fact]
    public void Layers() => Guards.Verify(new Layers(LayersFile, Server));

    [Fact]
    public void Endpoints() => Guards.Verify(new Endpoints(Server, typeof(ServerApp).Namespace ?? "", "Endpoints"));

    [Fact]
    public void Project_references() => Guards.Verify(new ProjectReferences(Repository, "Myapp.slnx", LayersFile));

    [Fact]
    public void Every_project_guarded() => Guards.Verify(new EveryProjectGuarded(LayersFile, Assemblies));

    [Fact]
    public void Interfaces_have_two_implementations() => Guards.Verify(InterfacesHaveTwoImplementations.Of(Assemblies));

    [Fact]
    public void Nothing_obsolete() => Guards.Verify(new NothingObsolete(Assemblies));

    [Fact]
    public void No_unused_types() => Guards.Verify(new NoUnusedTypes([Server], [typeof(ServerApp).FullName ?? ""]));

    [Fact]
    public void Forbidden_type_names() => Guards.Verify(ForbiddenTypeNames.Of(Assemblies));

    [Fact]
    public void Unique_type_names() => Guards.Verify(UniqueTypeNames.Of(Assemblies));

    [Fact]
    public void One_type_per_file() => Guards.Verify(new OneTypePerFile(Repository.CSharpSources()));

    [Fact]
    public void No_escape_hatches() => Guards.Verify(new NoEscapeHatches(Repository.CSharpSources()));

    [Fact]
    public void No_fixture_in_field() =>
        Guards.Verify(new NoFixtureInField(Repository.CSharpSources("Myapp.Server.Tests/"), "TestApp", "ClassServer"));

    [Fact]
    public void Build_settings() => Guards.Verify(new BuildSettings(Repository));

    [Fact]
    public void Migration_script_names() =>
        Guards.Verify(new MigrationScriptNames(Path.Combine(Repository.Root, "Myapp.Server/Database/Migrations/up")));
}
