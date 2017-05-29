using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Transform.UnitTests
{
  using System.Linq;
  using System.Xml;

  [TestClass]
  public class Program_Tests
  {
    [TestMethod]
    public void ParseRole()
    {
      // arrange
      var webConfig = ParseXml(@"
<configuration>
  <appSettings>
    <add key='role:define' value='abc' />
  </appSettings>
</configuration>");

      // act
      var role = Program.ParseRole(webConfig);

      // assert
      Assert.AreEqual("abc", role);
    }

    [TestMethod]
    public void ProcessFile_Partial()
    {
      // arrange
      var xml = ParseXml(@"
<configuration xmlns:patch='http://www.sitecore.net/xmlconfig/'>
  <sitecore xmlns:role='http://www.sitecore.net/xmlconfig/role/'>
    <element role:require='Delivery'>
      <delivery1 />
    </element>
    <element role:require='Authoring'>
      <authoring xmlns:role2='http://www.sitecore.net/xmlconfig/role/' />
      <element role:require='Delivery'>
        <delivery2 />
      </element>
    </element>
  </sitecore>
</configuration>");

      var expected = ParseXml(@"
<configuration xmlns:patch='http://www.sitecore.net/xmlconfig/'>
  <sitecore>
    <element>
      <authoring />
    </element>
  </sitecore>
</configuration>");

      // act
      var result = Program.ProcessFile(xml.DocumentElement, "Authoring".Split());

      // assert
      CompareElements(expected.DocumentElement, xml.DocumentElement, "/configuration", 0);
      Assert.IsTrue(result);
    }

    [TestMethod]
    public void ProcessFile_Empty()
    {
      // arrange
      var xml = ParseXml(@"
<configuration xmlns:patch='http://www.sitecore.net/xmlconfig/' xmlns:role='http://www.sitecore.net/xmlconfig/role/'>
  <sitecore role:require='Delivery'>
    <delivery />
  </sitecore>
</configuration>");

      var expected = ParseXml(@"
<configuration xmlns:patch='http://www.sitecore.net/xmlconfig/'>
</configuration>");

      // act
      var result = Program.ProcessFile(xml.DocumentElement, "Authoring".Split());

      // assert
      CompareElements(expected.DocumentElement, xml.DocumentElement, "/configuration", 0);
      Assert.IsFalse(result);
    }

    private void CompareElements(XmlElement expected, XmlElement actual, string path, int childNumber)
    {
      CompareAttributes(expected.Attributes, actual.Attributes, path, childNumber);
      for (var i = 0; i < Math.Max(expected.ChildNodes.Count, actual.ChildNodes.Count); ++i)
      {
        var exp = i < expected.ChildNodes.Count ? (XmlElement)expected.ChildNodes[i] : null;
        var act = i < actual.ChildNodes.Count ? (XmlElement)actual.ChildNodes[i] : null;
        if (exp == null)
        {
          if (act == null)
          {
            throw new NotImplementedException("This cannot be");
          }

          Assert.Fail($"Unexpected xml element by path {path}[{childNumber}]: {act}");
        }
        else if (act == null)
        {
          Assert.Fail($"Missing xml element by path {path}[{childNumber}]: {exp}");
        }
        else
        {
          CompareElements(exp, act, path + "/" + exp.Name, i);
        }
      }
    }

    private void CompareAttributes(XmlAttributeCollection expected1, XmlAttributeCollection actual1, string path, int childNumber)
    {
      var expected = expected1.OfType<XmlAttribute>().ToList();
      var actual = actual1.OfType<XmlAttribute>().ToList();
      foreach (XmlAttribute exp in expected1)
      {
        expected.Remove(exp);
        var act = actual.FirstOrDefault(x => x.Name == exp.Name);
        Assert.IsNotNull(act, $"{path}[{childNumber}][@{exp.Name}]");

        Assert.AreEqual(exp.Value, act.Value);
        actual.Remove(act);
      }

      foreach (var act in actual)
      {
        Assert.Fail($"Unexpected attr {path}[{childNumber}][@{act.Name}='{act.Value}']");
      }
    }

    private static XmlDocument ParseXml(string xml)
    {
      var webConfig = new XmlDocument();
      webConfig.LoadXml(xml.Replace("'", "\"").Trim());
      return webConfig;
    }
  }
}
