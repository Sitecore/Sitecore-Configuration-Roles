namespace Sitecore.Configuration.Roles
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Xml;
  using Sitecore.Diagnostics;
  using Sitecore.Xml.Patch;

  [UsedImplicitly]
  public class RoleConfigReader : ConfigReader
  {
    [NotNull]
    private readonly string[] IncludeOverride = ReadIncludeOverride();

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

    protected override void LoadAutoIncludeFiles(XmlNode element)
    {
      Assert.ArgumentNotNull(element, "element");

      if (IncludeOverride.Length == 0)
      {
        base.LoadAutoIncludeFiles(element);

        return;
      }
      
      var configPatcher = GetConfigPatcher(element);
      LoadAutoIncludeFiles(configPatcher, MainUtil.MapPath("/App_Config/Sitecore/Components"));

      foreach (var path in IncludeOverride)
      {
        Assert.IsTrue(Directory.Exists(path), "The include:override setting points to non-existing folder: {0}", path);
        Assert.IsTrue(Path.IsPathRooted(path), "The include:override setting points to non-rooted path: {0}", path);

        LoadAutoIncludeFiles(configPatcher, path);
      }
    }

    [NotNull]
    private static string[] ReadIncludeOverride()
    {
      var includeOverride = System.Configuration.ConfigurationManager.AppSettings["include:override"];
      if (string.IsNullOrEmpty(includeOverride))
      {
        return new string[0];
      }

      return includeOverride.Split(";|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    }
  }
}
