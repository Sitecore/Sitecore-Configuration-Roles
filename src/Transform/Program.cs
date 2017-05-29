namespace Transform
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Xml;
  using Sitecore.Configuration.Roles.BooleanLogic;

  public static class Program
  {
    public static void Main(string[] args)
    {
      if (args.Length == 0)
      {
        Console.WriteLine("Transform.exe - the tool is part of Sitecore Configuration Roles 1.2");
        Console.WriteLine();
        Console.WriteLine("The only purpose is to convert roles-enabled configuration files and");
        Console.WriteLine("downgrade them to regular configuration files: ");
        Console.WriteLine(" * update web.config file with stock config provider");
        Console.WriteLine(" * update all include files by removing all sections that do not comply ");
        Console.WriteLine("   with roles specified in role:define setting in web.config file and all");
        Console.WriteLine("   signs of configuration roles used.");
        Console.WriteLine();
        Console.WriteLine("Usage: ");
        Console.WriteLine("  > Transform.exe <path-to-web.config> [<path-to-output-folder>]");
        Console.WriteLine("    (if output folder omitted, creates Include_<timestamp> folder instead");

        return;
      }

      var filePath = GetWebConfigPath(args);
      var webConfig = ReadWebConfigFile(filePath);
      var role = ParseRole(webConfig);
      if (string.IsNullOrEmpty(role))
      {
        throw new NotSupportedException("Cannot find <add key=\"role:define\" value=\"...\"/> child element of <appSettings>, or the value is empty");
      }

      var timestamp = $"{DateTime.Now:yyyyMMdd-HHmmss}";
      ChangeConfigProvider(webConfig, $"{filePath}_{timestamp}");

      var folderPath = Path.GetDirectoryName(filePath);
      folderPath = Path.Combine(folderPath, "App_Config\\Include");

      var outputDir = GetOutputDirectoryPath(args);
      outputDir = string.IsNullOrEmpty(outputDir) ? $"{folderPath}_{timestamp}" : Path.Combine(outputDir, "App_Config\\Include");
      Directory.CreateDirectory(outputDir);

      var files = Directory.GetFiles(folderPath, "*.config", SearchOption.AllDirectories);
      foreach (var file in files)
      {
        ProcessFile(file, folderPath, outputDir, role.Split("|;,".ToCharArray()));
      }
    }

    private static void ProcessFile(string filePath, string sourceFolderPath, string outputFolderPath, string[] roles)
    {
      var relativePath = filePath.Substring(sourceFolderPath.Length).TrimStart("\\/".ToCharArray());
      var newFilePath = Path.Combine(outputFolderPath, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));

      var outputStream = File.OpenWrite(newFilePath);
      try
      {
        var xml = new XmlDocument();
        xml.Load(filePath);

        // do work
        if (!ProcessFile(xml.DocumentElement, roles))
        {
          // delete (do not copy to output folder) include file if it does't have <sitecore> element
          return;
        }

        using (var writer = new XmlTextWriter(outputStream, Encoding.Unicode))
        {
          xml.WriteTo(writer);
        }
      }
      finally
      {
        outputStream.Close();
      }
    }

    internal static bool ProcessFile(XmlElement xml, string[] roles)
    {
      ProcessElement(xml, roles);
      StripNamespace(xml);

      return xml.SelectSingleNode("sitecore") != null;
    }

    private static void StripNamespace(XmlElement xml)
    {
      // remove namespace
      xml.Attributes
        .OfType<XmlAttribute>()
        .Where(x => x.Prefix == "xmlns" && x.Value == "http://www.sitecore.net/xmlconfig/role/")
        .ToList()
        .ForEach(x => 
          x.OwnerElement.Attributes.Remove(x));

      foreach (var child in xml.ChildNodes.OfType<XmlElement>())
      {
        StripNamespace(child);
      }
    }

    private static void ProcessElement(XmlElement xml, string[] roles)
    {
      var require = xml.Attributes["require", "http://www.sitecore.net/xmlconfig/role/"];
      if (require != null)
      {
        var tokens = new Tokenizer(require.Value, roles).Tokenize();
        if (!new Parser(tokens).Parse())
        {
          xml.ParentNode.RemoveChild(xml);
          return;
        }
      }

      if (require != null)
      {
        xml.Attributes.Remove(require);
      }

      // ToArray is important as the collection can be modified
      foreach (var child in xml.ChildNodes.OfType<XmlElement>().ToArray())
      {
        ProcessElement(child, roles);
      }
    }

    internal static string ParseRole(XmlDocument webConfig)
    {
      return ((XmlElement)webConfig.DocumentElement.SelectSingleNode("appSettings/add[@key='role:define']"))?.GetAttribute("value");
    }

    internal static void ChangeConfigProvider(XmlDocument webConfig, string filePath)
    {
      var section = ((XmlElement)webConfig.DocumentElement.SelectSingleNode("configSections/section[@name='sitecore']"));
      if (!section.GetAttribute("type").StartsWith("Sitecore.Configuration.Roles.RoleConfigReader"))
      {
        return;
      }

      section.SetAttribute("type", "Sitecore.Configuration.ConfigReader, Sitecore.Kernel");
      webConfig.Save(filePath);
    }

    private static XmlDocument ReadWebConfigFile(string filePath)
    {
      var webConfig = new XmlDocument();
      webConfig.Load(filePath);

      return webConfig;
    }

    private static string GetWebConfigPath(string[] args)
    {
      return GetFirstArgument(args);
    }

    private static string GetFirstArgument(string[] args)
    {
      return args[0];
    }

    private static string GetOutputDirectoryPath(string[] args)
    {
      return args.Skip(1).FirstOrDefault();
    }
  }
}
