# IIS Deployment Notes

Notes from deploying the DRS Umbraco CMS Pilot to IIS.

## Final Result

The site was successfully deployed and served through IIS.

Local IIS URL:

```text
http://localhost:8080
```

# IIS Deployment Notes

1. Published the ASP.NET Core / Umbraco project.
2. Copied publish output to IIS folder.
3. Created an IIS website.
4. Created/selected the correct Application Pool.
5. Installed .NET Hosting Bundle.
6. Fixed the App Pool configuration.
7. Fixed SQL Server connection string.
8. Verified Umbraco boot and website pages.

# Important IIS Settings

Application Pool:

```
.NET CLR Version: No Managed Code
Managed Pipeline Mode: Integrated
Enable 32-Bit Applications: False
Identity: ApplicationPoolIdentity
```

The IIS site must use the correct Application Pool.

# Issues Faced

1. HTTP Error 500.19

Cause:

IIS could not understand the ASP.NET Core module in web.config.

Reason:

AspNetCoreModuleV2 was not registered.

Fix:

Installed the .NET Hosting Bundle and restarted IIS.

2. HTTP Error 500.30

Cause:

ASP.NET Core app failed to start through IIS.

Fixes checked:

- App Pool configuration
- Correct Application Pool assigned to the site
- Permissions
- stdout logs
- Hosting model

3. Umbraco Boot Failed

Cause:

Umbraco could not connect to SQL Server.

Log showed:

Configured database is reporting as not being available.
Failed to detected SqlServer version.

Fix:

Corrected the production connection string and SQL Server accessibility.

# Useful Commands

- Publish:

```
cmd
dotnet publish .\DrsUmbraco.Cms\DrsUmbraco.Cms.csproj -c Release -o .\publish\drs-umbraco
```

- Copy to IIS folder:

```
robocopy C:\Projects\DrsUmbracoPilot\publish\drs-umbraco C:\inetpub\drs /MIR
```

- Restart IIS:

```
iisreset
```

- Check ASP.NET Core IIS module:

```
powershell
C:\Windows\System32\inetsrv\appcmd.exe list modules | findstr /I AspNetCore
```

- Run app directly:

```
cd C:\inetpub\drs
dotnet .\DrsUmbraco.Cms.dll
```

# Final Demo Checklist

- Home page opens.
- Product pages open.
- Contact page opens.
- Recruitment pages open.
- Backoffice opens.
- Forms submit successfully.
- Data is inserted into SQL Server.
- Sitemap works.
- Robots.txt works.
- Custom 404 works.

