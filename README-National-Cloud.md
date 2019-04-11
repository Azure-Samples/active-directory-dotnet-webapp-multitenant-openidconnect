---
services: active-directory
platforms: dotnet
author: negoe 
level: 200
client: .NET Web App (MVC)
service: Microsoft Graph
endpoint: AAD v1.0
---


# Build a multi-tenant SaaS web application using Microsoft identity platform & OpenID Connect

[![Build status](https://identitydivision.visualstudio.com/IDDP/_apis/build/status/AAD%20Samples/.NET%20client%20samples/CI%20of%20Azure-Samples%20--%20active-directory-dotnet-webapp-multitenant-openidconnect)](https://identitydivision.visualstudio.com/IDDP/_build/latest?definitionId=729)

## About this sample

This sample shows how to build an ASP.NET MVC web application that uses [OpenID Connect](https://docs.microsoft.com/en-us/azure/active-directory/develop/v1-protocols-openid-connect-code) to sign in users from any Azure Active Directory tenant in any national cloud using the [ASP.Net OpenID Connect OWIN middleware](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet) and the [Active Directory Authentication Library (ADAL) for .NET](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet). 

It also introduces developers to the concept of 

- Microsoft National cloud environments
- Multi-tenant Azure Active Directory application

[!Note] If you want to run this sample in Microsoft worldwide Cloud, navigate to the [README.md](./README.md).

### Microsoft National cloud environments

National clouds (aka Sovereign clouds) are physically isolated instances of Azure. These regions of Azure are designed to make sure that data residency, sovereignty, and compliance requirements are honored within geographical boundaries.

In addition to the public cloud​, Azure Active Directory is deployed in the following National clouds:  

- Azure US Government
- Azure China 21Vianet
- Azure Germany

### Multi-tenant Azure Active Directory application

When it comes to developing apps, developers can choose to configure their app to be either single-tenant or multi-tenant during app registration in the [Azure portal](https://portal.azure.com).

- `Single-tenant` is for building line of business apps for users within your organization. These apps are only available to users that are in the same tenant as the app registration.

- `Multi-tenant` Multi-tenant is for apps built by independent software vendors for any organization within a specific national cloud. These apps are available to users in any tenant where the app is provisioned by an administrator or consented by a user.

For more information about apps and tenancy, see [Tenancy in Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/develop/single-and-multi-tenant-apps)

For more information about how the protocols work in this scenario and other scenarios, see the [Authentication Scenarios for Azure AD](https://azure.microsoft.com/documentation/articles/active-directory-authentication-scenarios/) document.

![Overview](./ReadmeFiles/topology.png)

## Scenario

This sample demonstrates a multi-tenant .NET Web App (MVC) application signing in users from multiple Azure AD tenants within a National cloud calling Microsoft Graph.

1. The web app allows the tenant admins to sign up (and provision this app in their tenant) by signing them in using the [ASP.Net OpenID Connect OWIN middleware](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet).
2. Once an admin consents to the application's requested permissions, the users of these tenants can then sign in themselves and create a Todo list for themselves.
3. The application also uses the [Active Directory Authentication Library (ADAL) for .NET](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet) library to obtain a JWT access token from Azure Active Directory (Azure AD) for Microsoft Graph.
4. The access token is used as a bearer token to authenticate the user when calling Microsoft Graph to fetch the signed-in user's details.

>[!Note] Azure Government applications can use Azure AD Government identities, but can also use Azure AD Public identities to authenticate to an application hosted in Azure Government.
A multi-tenant application **will not** be accessible using Azure AD Public identities. To know more about choosing identity authority go to [choose identity authority in Azure Government](https://docs.microsoft.com/en-us/azure/azure-government/documentation-government-plan-identity#choosing-your-identity-authority). 

## How to run this sample

To run this sample, you'll need:

- [Visual Studio 2017](https://aka.ms/vsdownload)
- An Azure Active Directory (Azure AD) tenant in that National cloud. (https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/)
- A user account in your Azure AD tenant. This sample will not work with a Microsoft account (formerly Windows Live account). Therefore, if you signed in to the [Azure portal](https://portal.azure.com) with a Microsoft account and have never created a user account in your directory before, you need to do that now.

### Step 1:  Clone or download this repository

From your shell or command line:

```Shell
git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-multitenant-openidconnect.git`
```

or download and extract the repository .zip file.

> Given that the name of the sample is pretty long, and so are the name of the referenced NuGet packages, you might want to clone it in a folder close to the root of your hard drive, to avoid file size limitations on Windows.


If you are using the automation provided via Powershell to create your app, you need to change the [Configure.ps1](./AppCreationScripts/Configure.ps1) and [Cleanup.ps1](./AppCreationScripts/Cleanup.ps1) as instructed below to append the `-AzureEnvironmentName` parameter. The details on this parameter and its possible values are listed in [Connect-AzureAD](https://docs.microsoft.com/en-us/powershell/module/azuread/connect-azuread?view=azureadps-2.0).

 ```Powershell
 Connect-AzureAD -TenantId $tenantId -AzureEnvironmentName AzureUSGovernment
 ```

### Step 2:  Register the sample application with your Azure Active Directory tenant

1. Sign in to the [US Government Azure portal](https://portal.azure.us)
page select **New registration**.
    
    For registering your app in other National Clouds go to [App Registration endpoints](https://docs.microsoft.com/en-us/azure/active-directory/develop/authentication-national-cloud#app-registration-endpoints) of the National Cloud of your choice, using either a work or school account.
   
   > Note: Azure Germany doesn't support *App registrations (Preview)* experience.
1. When the **Register an application page** appears, enter your application's registration information:
   - In the **Name** section, enter a meaningful application name that will be displayed to users of the app, for example `TodoListWebApp_MT`.
   - In the **Supported account types** section, select **Accounts in any organizational directory**.
   - In the **Redirect URI** section, select **Web** in the combo-box and enter the following redirect URIs.
       - `https://localhost:44302/`
       - `https://localhost:44302/Onboarding/ProcessCode`
1. Select **Register** to create the application.
1. On the app **Overview** page, find the **Application (client) ID** value and record it for later. You'll need it to configure the Visual Studio configuration file for this project.
1. In the list of pages for the app, select **Authentication**.
   - In the **Advanced settings** section set **Logout URL** to `https://localhost:44302/Account/EndSession`
   - In the **Advanced settings** | **Implicit grant** section, check  **ID tokens** as this sample requires the [Implicit grant flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-implicit-grant-flow) to be enabled to
   sign-in the user, and call an API.
1. Select **Save**.
1. The new customer onboarding process implemented by the sample requires the application to perform an OAuth2 request, which in turn requires to associate a key to the app in your tenant. From the **Certificates & secrets** page, in the **Client secrets** section, choose **New client secret**:

   - Type a key description (of instance `app secret`),
   - Select a key duration of either **In 1 year**, **In 2 years**, or **Never Expires**.
   - When you press the **Add** button, the key value will be displayed, copy, and save the value in a safe location.
   - You'll need this key later to configure the project in Visual Studio. This key value will not be displayed again, nor retrievable by any other means,
     so record it as soon as it is visible from the Azure portal.
1. In the list of pages for the app, select **API permissions**
   - Click the **Add a permission** button and then,
   - Ensure that the **Microsoft APIs** tab is selected
   - In the *Commonly used Microsoft APIs* section, click on **Microsoft Graph**
   - In the **Delegated permissions** section, ensure that the right permissions are checked: **User.Read**, **User.Read.All**. Use the search box if necessary.
   - Select the **Add permissions** button

### Step 3: Configure the sample to use your Azure AD tenant

In the steps below, "ClientID" is the same as "Application ID" or "AppId".

Open the solution dotnet-webapp-multitenant-oidc.sln in Visual Studio to configure the project (no s).

#### Configure the service project

1. Open the `TodoListWebApp\Web.Config` file
2. Find the app key `ida:ClientId` and replace the existing value with the application ID (clientId) of the `TodoListWebApp_MT` application copied from the Azure portal.
3. Find the app key `ida:ClientSecret` and replace the existing value with the key you saved during the creation of the `TodoListWebApp_MT` app, in the Azure portal.
4. Find the app key `ida:RedirectUri` and replace the existing value with the base address of the TodoListWebApp_MT project (by default `https://localhost:44302/`).
5. Find the app key `ida:AADInstance` and replace the existing value with the corresponding [Azure AD endpoint](https://docs.microsoft.com/en-us/azure/active-directory/develop/authentication-national-cloud#azure-ad-authentication-endpoints) for the national cloud you want to target.
6. Find the app key `ida:GraphAPIEndpoint` and replace the existing value with the corresponding [Microsoft Graph endpoint](https://docs.microsoft.com/en-us/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints) for the  national cloud you want to target.


### Step 5: Run the sample

Clean the solution, rebuild the solution, and run it. The sample implements two distinct tasks: the onboarding of a new customer and regular sign in & use of the application.

#### Sign up

- When running the app for the first time, you'd need to sign-in as an administrator first. Click the `Sign Up` link on the top bar.

![Sign Up link](./ReadmeFiles/Sign-Up.JPG)

- You will be presented with a form that simulates an onboarding process. Check the checkbox and  Click the `SignUp` button.

![Sign Up form](./ReadmeFiles/Sign-Up-Admin.JPG)

 - Now you are going through the [admin consent](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-convert-app-to-be-multi-tenant#admin-consent) flow. In this flow, the app gets provisioned for all the users in one organization. You'll be transferred to the Azure AD portal. Sign in as the administrator and you'd be presented with the following screen to consent on behalf of all users.

![Admin Consent](./ReadmeFiles/Admin_consent.JPG)

- Click `Accept` to provision a [service principal](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals#service-principal-object) of this app in the tenant.
You will be transported back to the app, where your registration will be finalized.

- If the app is not provisioned in your tenant by the tenant administrator using the steps laid out above, the sign-up process will result in the following error.

![Need admin approval](./ReadmeFiles/NeedAdminApproval.JPG)

- This step uses the `prompt=admin_consent' option provided in the [OAuth 2.0 authorization code grant](https://docs.microsoft.com/en-us/azure/active-directory/develop/v1-protocols-oauth-code#request-an-authorization-code) to provide the administrator an option to consent for the entire tenant.

- You can also use the [admin consent endpoint](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent#using-the-admin-consent-endpoint) to provision the app in the chosen tenant.

#### Sign in

- Once you signed up, you can either click on the `Todo` tab or the `sign in` link to gain access to the application. 
- If you are selecting **sign in the same session in which you signed up, you will automatically sign in with the same account you used for signing up.
- If you are signing in during a new session, you will be presented with Azure AD's credentials prompt: sign in using an account compatible with the sign-up option you chose earlier (the exact same account if you used user consent, any user form the same tenant if you used admin consent).

- If you try to sign-in before the tenant administrator has provisioned the app in the tenant using the `Sign up` link above, you will see the following error.

![AADSTS700016, App not found](./ReadmeFiles/AADSTS700016.JPG)

> To learn more about the code,  go to [About the code](./README.md#about-the-code) 

## Community Help and Support

Use [Stack Overflow](http://stackoverflow.com/questions/tagged/adal) to get support from the community.
Ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.
Make sure that your questions or comments are tagged with [`adal` `msal` `dotnet`].

If you find a bug in the sample, raise the issue on [GitHub Issues](../../issues).

To provide a recommendation, visit the following [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contributing

If you'd like to contribute to this sample, see [CONTRIBUTING.MD](/CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## More information

For more information, see the following links:

- [Microsoft identity platform (Azure Active Directory for developers)](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Understanding Azure AD application consent experiences](https://docs.microsoft.com/en-us/azure/active-directory/develop/application-consent-experience)
- [How to: Sign in any Azure Active Directory user using the multi-tenant application pattern](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-convert-app-to-be-multi-tenant)
- [Understand user and admin consent](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-convert-app-to-be-multi-tenant#understand-user-and-admin-consent)
- [Recommended pattern to acquire a token](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token#recommended-pattern-to-acquire-a-token)
- [Customizing Token cache serialization](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Token-cache-serialization)
- [National Clouds](https://docs.microsoft.com/en-us/azure/active-directory/develop/authentication-national-cloud)
- [ADAL.NET's conceptual documentation](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki)

For more information about how OAuth 2.0 protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).