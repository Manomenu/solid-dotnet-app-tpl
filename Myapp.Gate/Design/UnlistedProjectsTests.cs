namespace Myapp.Gate.Design;

// Why: a guard over a list passes in silence for whatever is left off the list, so what the list leaves out has to fail by itself
public sealed class UnlistedProjectsTests
{
    private static IEnumerable<string> ProjectsWithProductionCode() =>
        LayersFile.Projects().Keys.Where(project => !project.EndsWith(".Tests", StringComparison.Ordinal));

    [Fact]
    public void Every_project_is_read_by_the_guards_over_types()
    {
        var read = SolutionTypes.Assemblies.Select(assembly => assembly.GetName().Name);
        var unread = ProjectsWithProductionCode().Except(read).Order().ToList();

        Assert.True(
            unread.Count == 0,
            $"add typeof(<a type of the project>).Assembly to Assemblies in Myapp.Gate/Design/SolutionTypes.cs, or the naming, interface and obsolete guards never see it: {string.Join(", ", unread)}"
        );
    }

    [Fact]
    public void Every_project_has_layers()
    {
        var gate = typeof(UnlistedProjectsTests).Assembly.GetName().Name;
        var unlayered = ProjectsWithProductionCode()
            .Where(project => project != gate)
            .Except(ContractsTests.LayeredProjects)
            .Order()
            .ToList();

        Assert.True(
            unlayered.Count == 0,
            $"give the project an order in Myapp.Gate/layers.json and the tests serverLayers has in ContractsTests, let SubtractTests read it for unused types, then add it to LayeredProjects in ContractsTests: {string.Join(", ", unlayered)}"
        );
    }
}
