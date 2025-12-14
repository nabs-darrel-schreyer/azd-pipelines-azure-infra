# azd-pipelines-azure-infra



# Setup

Create a solution using the the Aspire Starter App template in Visual Studio.

1. Open Visual Studio and select "Create a new project".
1. Search for "Aspire Starter App" in the project template search box.
1. Select the "Aspire Starter App" template and click "Next".
1. Configure your project details (name, location, etc.) and click "Create".

Here is the official documentation for setting up an Aspire Starter App to use with Azure Developer CLI:
[Aspire Starter App Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azd/aca-deployment-github-actions)

NOTE: As of today, there is an `Alpha` warning on this feature.

I am documenting the steps I am taking in order to understand the process and confirm that everything works as documented.

# Step 1: Initialize the Template

On the `azd init` command step, after selecting the `Scan current directory` option, I received the following warning:

```
  Limited mode Warning: Your Aspire project is delegating the services' host infrastructure to azd.
  This mode is limited. You will not be able to manage the host infrastructure from your AppHost. You'll need to use `azd infra gen` to customize the Azure Container Environment and/or Azure Container Apps  See more: https://learn.microsoft.com/dotnet/aspire/azure/configure-aca-environments
```

I cancelled and exited the process so that I could control the environment from Aspire.

# Step 2: Aspire Controlled Infrastructure

At this point you should get yourself familiar with the Aspire documentation. See [Aspire Docs](https://aspire.dev).

Make sure you have the latest version. Things are moving fast a the moment so get in the habit of checking your version.
```bash
aspire --version
```

In order to set up the infrastructure controlled by Aspire, you need to follow the Aspire documentation.

The documentation you are after is in the `Integrations` section in the docs.

I will be setting up my application using [Azure Container Apps (ACA)](https://aspire.dev/integrations/cloud/azure/configure-container-apps/) as the hosting environment.

Use the `aspire add [<integration>] [options]` command to add the integration to your Aspire project. See [Add Integrations](https://aspire.dev/reference/cli/commands/aspire-add/).

To set up Aspire controlled infrastructure, follow these steps:

1. Run the following command: `aspire add Aspire.Hosting.Azure.AppContainers`. This will add the NuGet packages and configuration files needed for ACA.

```text
🗄  Created settings file at '.aspire/settings.json'.
✔  The package Aspire.Hosting.Azure.AppContainers::13.0.2 was added successfully.
```

2. Open the `AppHost.cs` in the `*.AppHost` project. Add the following using statement at the top of the file:

```csharp
var aca = builder.AddAzureContainerAppEnvironment("aca-env");
```

3. Run the init command again: `aspire init`. This time you won't get a warning.

```text
Detected services:

  .NET (Aspire)
  Detected in: C:\Dev\nabs-darrel-schreyer\azd-pipelines-azure-infra\src\AzdPipelinesAzureInfra.AppHost\AzdPipelinesAzureInfra.AppHost.csproj

azd will generate the files necessary to host your app on Azure.
```

I think it would have been nice if it confirmed how it will be hosted. But you can check that for yourself.

Provide the environment name when prompted. I used `testing-azd-pipelines`. See the file `.aspire/config.json` for confirmation.

> NOTE: At this point you can deploy the infrastructure directly to Azure from your local machine. This is great for ephemeral environment testing. However, in this example I want to deploy from GitHub Actions using Azure Developer CLI.

