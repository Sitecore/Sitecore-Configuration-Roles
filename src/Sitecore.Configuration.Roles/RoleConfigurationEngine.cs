namespace Sitecore.Configuration.Roles
{
  using System.Xml;

  [UsedImplicitly]
  public class RoleConfigurationEngine : ConfigurationEngine
  {
    [NotNull]
    public override XmlDocument GetConfiguration()
    {
      return RoleConfigurationFactory.GetConfiguration();
    }
  }
}