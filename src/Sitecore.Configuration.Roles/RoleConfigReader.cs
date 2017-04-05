namespace Sitecore.Configuration.Roles
{
  using System.Collections;
  using System.Xml;
  using Sitecore.Diagnostics;
  using Sitecore.Xml.Patch;

  [UsedImplicitly]
  public class RoleConfigReader : ConfigReader
  {
    private readonly RoleConfigurationHelper RoleConfigurationHelper;

    private readonly XmlPatcher Patcher;

    public RoleConfigReader()
    {
      RoleConfigurationHelper = new RoleConfigurationHelper();
      Patcher = new XmlPatcher("http://www.sitecore.net/xmlconfig/set/", "http://www.sitecore.net/xmlconfig/", new RoleXmlPatchHelper(RoleConfigurationHelper));
    }

    protected override void ExpandIncludeFiles([NotNull] XmlNode rootNode, [NotNull] Hashtable cycleDetector)
    {
      Assert.ArgumentNotNull(rootNode, "rootNode");
      Assert.ArgumentNotNull(cycleDetector, "cycleDetector");

      RoleConfigurationHelper.LoadAppSetting();

      base.ExpandIncludeFiles(rootNode, cycleDetector);
    }

    protected override void ReplaceGlobalVariables([NotNull] XmlNode rootNode)
    {
      Assert.ArgumentNotNull(rootNode, "rootNode");

      base.ReplaceGlobalVariables(rootNode);

      RoleConfigurationHelper.Validate();
    }

    [NotNull]
    protected override ConfigPatcher GetConfigPatcher([NotNull] XmlNode element)
    {
      Assert.ArgumentNotNull(element, "element");

      return new ConfigPatcher(element, this.Patcher);
    }
  }
}
