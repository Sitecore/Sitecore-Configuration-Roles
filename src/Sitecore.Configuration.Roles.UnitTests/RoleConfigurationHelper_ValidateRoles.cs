namespace Sitecore.Configuration.Roles.UnitTests
{
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using FluentAssertions;

  [TestClass]
  public class RoleConfigurationHelper_ValidateRoles
  {
    [TestMethod]
    public void Authoring()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring" })
        .Should().BeNull();
    }

    [TestMethod]
    public void Processing()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "processing" })
        .Should().BeNull();
    }

    [TestMethod]
    public void Reporting()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "reporting" })
        .Should().BeNull();
    }

    [TestMethod]
    public void Delivery()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "delivery" })
        .Should().BeNull();
    }
    
    [TestMethod]
    public void AuthoringDelivery()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "delivery" })
        .Should().BeNull();
    }

    [TestMethod]
    public void AuthoringDeliveryOne()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "delivery" })
        .Should().BeNull();
    }

    [TestMethod]
    public void AllInOneDeliveryOne()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "processing", "reporting", "delivery" })
        .Should().BeNull();
    }

    [TestMethod]
    public void AuthoringProcessingReporting()
    {
      RoleConfigurationHelper.ValidateRoles(new[] { "authoring", "processing", "reporting" })
        .Should().BeNull();
    }
  }
}
