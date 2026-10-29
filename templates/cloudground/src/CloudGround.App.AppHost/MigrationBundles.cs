using Aspire.Hosting.EntityFrameworkCore;
using Aspire.Hosting.Pipelines;

namespace CloudGround.App.AppHost;

internal static class MigrationBundles
{
    private const string BundleStepSuffix = "-generate-migration-bundle";

    public static IResourceBuilder<ProjectResource> BuildImageAfterMigrationBundles(this IResourceBuilder<ProjectResource> project)
    {
#pragma warning disable ASPIREPIPELINES001
        return project.WithPipelineConfiguration(context =>
        {
            var bundles = context.Model.Resources
                .OfType<EFMigrationResource>()
                .SelectMany(migrations => context.GetSteps(migrations))
                .Where(step => step.Name.EndsWith(BundleStepSuffix, StringComparison.Ordinal))
                .ToList();

            foreach (var build in context.GetSteps(project.Resource, WellKnownPipelineTags.BuildCompute))
            {
                bundles.ForEach(bundle => build.DependsOn(bundle));
            }
        });
#pragma warning restore ASPIREPIPELINES001
    }
}
