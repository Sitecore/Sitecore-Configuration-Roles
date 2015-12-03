namespace Sitecore.Configuration.Roles
{
  using System;
  using System.Collections.Generic;
  using System.Configuration;
  using System.Linq;
  using System.Text.RegularExpressions;
  using System.Threading;
  using System.Xml;
  using Sitecore.Diagnostics;
  using Sitecore.Xml.Patch;

  /// <summary>
  /// The configuration roles helper.
  /// </summary>
  public static class RoleConfigurationHelper
  {

    [CanBeNull]
    private static string[] definedRoles;

    /// <summary>
    /// List of defined roles.
    /// </summary>
    /// <value>
    /// The defined roles.
    /// </value>
    [NotNull]
    public static IEnumerable<string> DefinedRoles
    {
      get
      {
        return (definedRoles ?? new string[0]).ToArray();
      }
    }

    [CanBeNull]
    internal static string DefinedRolesSource { get; private set; }

    [CanBeNull]
    internal static string DefinedRolesErrorSource { get; private set; }

    [CanBeNull]
    private static string DefinedRolesErrorMessage { get; set; }

    internal static void LoadAppSetting()
    {
      var roleDefine = System.Configuration.ConfigurationManager.AppSettings["role:define"];
      if (!string.IsNullOrEmpty(roleDefine))
      {
        RoleConfigurationHelper.DefineRolesOnce(roleDefine, "web.config");
      }
    }

    internal static void Validate()
    {
      if (string.IsNullOrEmpty(DefinedRolesErrorSource) && string.IsNullOrEmpty(DefinedRolesErrorMessage))
      {
        return;
      }

      throw new ConfigurationErrorsException(DefinedRolesErrorMessage, DefinedRolesErrorSource, 0);
    }

    internal static bool ProcessRolesNamespace([NotNull] IXmlNode attribute)
    {
      Assert.ArgumentNotNull(attribute, "node");
      Assert.ArgumentCondition(attribute.NodeType == XmlNodeType.Attribute, "attribute", "The attribute node is not an XmlNodeType.Attribute");

      var name = attribute.LocalName;
      var value = attribute.Value;
      switch (name)
      {
        case "d":
        case "define":
          if (!string.IsNullOrEmpty(value))
          {
            RoleConfigurationHelper.DefineRolesOnce(value, attribute);

            return false;
          }

          break;

        case "r":
        case "require":
          if (!string.IsNullOrEmpty(value))
          {
            var words = value.Split("|;,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            return words.Any(word => RoleConfigurationHelper.DefinedRoles.Contains(word));
          }

          break;
      }

      return true;
    }

    private static void DefineRolesOnce([NotNull] string value, [NotNull] IXmlNode node)
    {
      Assert.ArgumentNotNull(value, "value");
      Assert.ArgumentNotNull(node, "node");

      var source = (IXmlSource)node;
      var sourceName = source.SourceName;

      DefineRolesOnce(value, sourceName);
    }

    private static void DefineRolesOnce([NotNull] string value, [NotNull] string sourceName)
    {
      Assert.ArgumentNotNull(value, "value");
      Assert.ArgumentNotNull(sourceName, "sourceName");

      if (definedRoles != null && DefinedRolesSource != sourceName)
      {
        DefinedRolesErrorSource = sourceName;
        DefinedRolesErrorMessage = string.Format(
          "Current set of roles defined in the \"{0}\" file was attempted to be modified in the \"{1}\" file. " +
          "This is not allowed to prevent unintended configuration changes. " +
          "If roles from both files are valid, they need to be merged into a single file.",
          DefinedRolesSource,
          DefinedRolesErrorSource);

        return;
      }

      var roles = value.Split("|,;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.ToLowerInvariant())
        .Distinct()
        .ToList();

      // expand some-role-1 into "some-role|some-role-1" and "dedicated-publishing" to "publishing|publishing-1|dedicated-publishing"
      var regex = new Regex(@"([^|;,]+)\-(\d+)$");
      var rolesCopy = roles.ToArray();
      var shift = 0;
      for (var i = 0; i < rolesCopy.Length; i++)
      {
        var role = rolesCopy[i];
        var match = regex.Match(role);
        if (match.Success && match.Groups.Count >= 1)
        {
          var newRole = match.Groups[1].Value;
          if (!roles.Contains(newRole))
          {
            roles.Insert(i + shift++, newRole);
          }
        }
        else if (role == "dedicated-publishing")
        {
          if (!roles.Contains("publishing"))
          {
            roles.Insert(i + shift++, "publishing");
          }

          if (!roles.Contains("publishing-1"))
          {
            roles.Insert(i + shift++, "publishing-1");
          }
        }
      }

      var error = ValidateRoles(roles);
      if (!string.IsNullOrEmpty(error))
      {
        DefinedRolesErrorMessage = error;
        DefinedRolesErrorSource = sourceName;

        return;
      }

      definedRoles = roles.ToArray();
      DefinedRolesSource = sourceName;
    }

    private static string ValidateRoles(ICollection<string> roles)
    {
      if (roles.Contains("delivery"))
      {
        var incompatibleRole = roles.FirstOrDefault(x => x != "delivery" && !x.StartsWith("delivery-"));
        if (incompatibleRole != null)
        {
          return string.Format("The delivery role is specified alongside with {0} which is not supported.", incompatibleRole);
        }
      }

      return null;
    }
  }
}
