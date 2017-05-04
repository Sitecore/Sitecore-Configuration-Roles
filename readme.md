# CONFIGURATION ROLES SUPPORT

The aim of this project to make Sitecore pre-configured for one of pre-defined 
configuration roles, so after installing a Sitecore instance the only setting 
should be changes: the roles the instance should have.

**NOTE:** This PoC is being evaluated by Sitecore at the moment - we will keep you posted on any news. You are welcome to use
it in non-production environment - please let us know if you have any comments.

### Index

##### [Prerequsites](#prerequsites)  

##### [How To](#how-to)  
1. [Install NuGet Package](#1-install-nuget-package)  
2. [Replace Include Configuration Files](#2-replace-include-configuration-files)  
3. [Deploy](#3-deploy)  
4. [Update web.config files](#4-update-webconfig-files)  
5. [Verify if it works](#5-verify-if-it-works)  

##### [Details](#details)  
1. [Define Role Command](#1--define-role-command)  
2. [Require Role Command](#2--require-role-command)  
3. [Modified configuration files](#3--modified-configuration-files)  

##### [Comments](#comments)

## Prerequsites

* **Sitecore CMS 8.1 rev. 160302 (Update-2)**
* **Sitecore CMS 8.1 rev. 160302 (Update-3)**
* **Sitecore CMS 8.2 - all releases**

In this project Sitecore configuration engine was extended with two simple commands
and modified configuration files that use them. It is distributed as a module which
is based on the `Sitecore CMS 8.1 rev. 160302 (Update-2)` which now allows patching 
configuration engine.

## How to

### 1. Install NuGet Package

First you need to do is to install the `Sitecore.Configuration.Roles` NuGet package:
```ps
PS> Install-Package Sitecore.Configuration.Roles
```
Alternatively, you can [download it here](https://github.com/Sitecore/Sitecore-Configuration-Roles/releases).

### 2. Replace Include Configuration Files

Replace default Sitecore configuration files in `App_Config/Include` folder with annotated ones. To do so delete entire contents of `App_Config/Include` folder (excepting for your custom files) and replace with files from one of the branches:
* Sitecore 8.1 Update-3 - [configuration/8.1.3](https://github.com/Sitecore/Sitecore-Configuration-Roles/tree/configuration/8.1.3)
* Sitecore 8.2 Update-2 - [configuration/8.1.3](https://github.com/Sitecore/Sitecore-Configuration-Roles/tree/configuration/8.2.2)
* Sitecore 8.2 Update-3 - [configuration/8.1.3](https://github.com/Sitecore/Sitecore-Configuration-Roles/tree/configuration/8.2.3)

Go through your custom configuration files and annotate configuration nodes that must be presented only in certain kind of instances. For examplem, the item saved event handlers in `Customization.config` file to be used only in the `ContentManagement` environment:
```xml
 <configuration xmlns:role="http://www.sitecore.net/xmlconfig/role/">
    <sitecore role:require="ContentManagement">
      <events>
        <event name="item:saved" role:require="ContentManagement">
          <handler type="Website.Class1, Website" method="OnItemSaved" />
```

### 3. Deploy

Your solution is ready to deploy, so deploy the following files to both `ContentManagement` and `ContentDelivery` Sitecore instances:
```
App_Config/Include/Sitecore.ContentSearch.Lucene.Index.Master.config
bin/Sitecore.Configuration.Roles.dll
```

### 4. Update web.config files

Last step is to change `web.config` files of both `ContentManagement` and `ContentDelivery` Sitecore instances so they are aware of their role. So for `ContentManagement` instance it should be:
```xml
  <configSections>
    <section name="sitecore" type="Sitecore.Configuration.Roles.RoleConfigReader, Sitecore.Configuration.Roles" />
    ...
  </configSections>
  <appSettings>
    ...
    <add key="role:define" value="ContentManagement" />
  </appSettings>
```
and this one for `ContentDelivery`:
```xml
  <configSections>
    <section name="sitecore" type="Sitecore.Configuration.Roles.RoleConfigReader, Sitecore.Configuration.Roles" />
    ...
  </configSections>
  <appSettings>
    ...
    <add key="role:define" value="ContentDelivery" />
  </appSettings>
```

### 5. Verify if it works

It is not required, but you can verify if it works by opening `/sitecore/admin/showconfig.aspx` and trying to find definition of `sitecore_master_index` index configuration element. If everything went smoothly, you should find it in `ContentManagement` environment and shouldn't in `ContentDelivery`.

## Details

### 1.  Define Role Command

    This command can be used only once to avoid accidential misconfiguration. 
    It defines pipe-separated "white list" of roles this Sitecore instance have. 
    
    Example:
    
    /web.config
    <configuration>
      <appSettings>
        <add key="role:define" value="role1|role2|..." />
      </appSettings>
    </configuration>
    
    Roles can be any string, but out of the box this POC offers these roles:
    * Standalone
    * ContentManagement
    * Reporting
    * Processing
    * ContentDelivery
    
    These roles are described below.       
    
### 2.  Require Role Command

    When this command is applied to a node within Sitecore include config file
    the node will be ignored if the boolean expression is false. The logic is
    simple: when evaluating the expression, every defined role name transforms
    into "true" and undefined role name transforms into "false"
    
    Example:
    
    <configuration xmlns:role="http://www.sitecore.net/xmlconfig/role/">
      <sitecore>
        <contentSearch>
          ...
          <index id="sitecore_web_index" role:require="ContentManagement OR ContentDelivery">
            ...
    
    In this example, when roles are specified as "ContentManagement|ContentDelivery", the transformed expression will be "(true AND !false) OR false".

### 3.  Modified configuration files

    The POC is shipped with modified stock configuration files to make Sitecore
    pre-configured to serve each of these configuration roles. 
    
    There are a number of out-of-box roles that attached configuration files 
    are aware of.       

    - Standalone
    
      Defines Standalone role that is the same as Sitecore pre-configured out of box. 
      It allows only single-server set up.
    
    - ContentManagement 
      
      Defines Content Management (CM) role that allows editors to use editing
      applications like Content Editor, Page Editor etc.
      
    - Reporting
      
      Defines xDB Reporting (Rep) role that fetches reporting data from various 
      data sources to use in Sitecore reporting applications. It can be enabled
      on the same instance with other roles or on a dedicated Sitecore instance.
      
    - Processing
      
      Defines xDB Processing (Proc) role. It can be enabled on the same instance 
      with other roles or on a dedicated Sitecore instance.
      
    - ContentDelivery
      
      Defines Content Delivery (CD) role that assumes current Sitecore instance
      is accessed only by end-users and Sitecore administrators. It cannot be 
      enabled on the same instance with other roles. 

## Examples

    EXAMPLE 1
    Here is an example of Sitecore solution with single Sitecore instance.

    - SRV-01: Standalone

    EXAMPLE 2
    Here is an example of Sitecore solution with 2 Sitecore instances: 
    one is multipurpose, another is delivery only - both serve front-end users.

    - SRV-01: ContentManagement|Processing|Reporting
    - SRV-02: ContentDelivery

    EXAMPLE 3
    Here is an example of Sitecore solution with 5 Sitecore instances,
    one content-management only, one processing and reporting and 3 delivery.

    - SRV-01: ContentManagement
    - SRV-02: Processing|Reporting
    - SRV-03: ContentDelivery
    - SRV-04: ContentDelivery
    - SRV-05: ContentDelivery
