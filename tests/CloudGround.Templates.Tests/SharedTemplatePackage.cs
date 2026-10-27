namespace CloudGround.Templates.Tests;

[CollectionDefinition(Name)]
public sealed class SharedTemplatePackage : ICollectionFixture<TemplatePackage>
{
    public const string Name = "template";
}
