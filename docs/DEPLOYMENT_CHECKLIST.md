# Deployment Checklist

This checklist describes the planned production deployment process for the DRS Umbraco CMS Pilot on Windows Server and IIS.

## 1. Pre-deployment Checks

Before deploying:

- Make sure the latest code is committed to Git.
- Make sure sensitive files are not committed.
- Make sure the project builds successfully.
- Make sure the website works locally.
- Back up the SQL Server database.
- Back up uploaded media files.
- Confirm the production domain.
- Confirm the production database connection string.
- Confirm SSL certificate availability.

Run locally:

```bash
dotnet build
```

Run the website locally:

```bash
cd DrsUmbraco.Cms
dotnet run
```

Test:

```text
/
 /products/
 /about-us/
 /contact-us/
 /recruitment/
 /page-that-does-not-exist
```

## 2. Files That Must Not Be Committed

The following files must stay out of Git:

```text
DrsUmbraco.Cms/appsettings.json
DrsUmbraco.Cms/appsettings.Development.json
DrsUmbraco.Cms/appsettings.Production.json
DrsUmbraco.Cms/appsettings.*.json
```

Only this safe sample file should be committed:

```text
DrsUmbraco.Cms/appsettings.example.json
```

Check before commit:

```bash
git status
```

## 3. Server Requirements

Production server requirements:

- Windows Server
- IIS installed
- .NET Hosting Bundle installed
- SQL Server access
- SSL certificate
- Write permission for required Umbraco folders

The .NET Hosting Bundle must be installed on the IIS server so ASP.NET Core applications can run behind IIS.

If IIS was installed after the Hosting Bundle, repair or reinstall the Hosting Bundle.

## 4. Production Configuration

Create a production config file on the server:

```text
appsettings.Production.json
```

This file should exist on the server only and should not be committed to Git.

Example structure:

```json
{
  "ConnectionStrings": {
    "umbracoDbDSN": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;",
    "umbracoDbDSN_ProviderName": "Microsoft.Data.SqlClient"
  },
  "Umbraco": {
    "CMS": {
      "Content": {
        "Error404Collection": [
          {
            "Culture": "default",
            "ContentKey": "YOUR-404-PAGE-CONTENT-KEY"
          }
        ]
      },
      "WebRouting": {
        "TrySkipIisCustomErrors": true
      }
    }
  }
}
```

Important:

- Replace `YOUR_SERVER`.
- Replace `YOUR_DATABASE`.
- Replace `YOUR_USER`.
- Replace `YOUR_PASSWORD`.
- Replace `YOUR-404-PAGE-CONTENT-KEY`.

## 5. Database Backup

Before deployment, create a full backup of the SQL Server database.

Backup should include:

- Umbraco tables
- WordPress legacy tables used by the pilot
- Elementor form submission tables

Important legacy tables:

```text
wp_e_submissions
wp_e_submissions_values
wp_e_submissions_actions_log
```

## 6. Media Backup

Back up uploaded media and static assets.

Important folders:

```text
DrsUmbraco.Cms/wwwroot/media
DrsUmbraco.Cms/wwwroot/assets
DrsUmbraco.Cms/wwwroot/css
DrsUmbraco.Cms/wwwroot/js
```

## 7. Publish the Application

From the project root:

```bash
cd C:\Projects\DrsUmbracoPilot
dotnet publish .\DrsUmbraco.Cms\DrsUmbraco.Cms.csproj -c Release -o .\publish\drs-umbraco
```

The publish output will be created here:

```text
C:\Projects\DrsUmbracoPilot\publish\drs-umbraco
```

## 8. Copy Files to IIS

Suggested IIS physical path:

```text
C:\inetpub\drs-umbraco
```

Copy the contents of:

```text
C:\Projects\DrsUmbracoPilot\publish\drs-umbraco
```

to:

```text
C:\inetpub\drs-umbraco
```

Do not copy development-only configuration files to production.

## 9. IIS Setup

In IIS Manager:

1. Create a new website.
2. Set Site name.
3. Set Physical path:

```text
C:\inetpub\drs-umbraco
```

4. Set binding:

```text
http / port 80 / domain
https / port 443 / domain
```

5. Assign SSL certificate for HTTPS.
6. Use a dedicated Application Pool.
7. Set the Application Pool identity.
8. Make sure the Application Pool has required permissions.

## 10. Folder Permissions

The IIS Application Pool identity should have the required read/write permissions for Umbraco runtime files, logs, cache, and media uploads.

Important folders:

```text
C:\inetpub\drs-umbraco
C:\inetpub\drs-umbraco\wwwroot
C:\inetpub\drs-umbraco\wwwroot\media
C:\inetpub\drs-umbraco\umbraco
```

Minimum practical permissions for the pilot:

```text
Read
Write
Modify
```

Apply permissions only to the required site folder, not the whole server.

## 11. First Production Test

After deployment, test these URLs:

```text
/
 /products/
 /about-us/
 /contact-us/
 /recruitment/
 /page-that-does-not-exist
 /umbraco
```

Check:

- Homepage loads.
- CSS and JS load.
- Images load.
- Header and mobile menu work.
- Contact form submits.
- Recruitment form submits.
- 404 page returns the custom page.
- Backoffice login works.

## 12. Form Submission Test

Submit a test form from:

```text
/contact-us/
```

Then verify SQL Server tables:

```sql
SELECT TOP 20
    s.id,
    s.form_name,
    s.referer,
    s.created_at,
    MAX(CASE WHEN v.[key] = 'fullname' THEN v.[value] END) AS FullName,
    MAX(CASE WHEN v.[key] = 'mobile' THEN v.[value] END) AS Mobile,
    MAX(CASE WHEN v.[key] = 'request_type' THEN v.[value] END) AS RequestType,
    MAX(CASE WHEN v.[key] = 'message' THEN v.[value] END) AS Message
FROM dbo.wp_e_submissions s
INNER JOIN dbo.wp_e_submissions_values v
    ON v.submission_id = s.id
GROUP BY
    s.id,
    s.form_name,
    s.referer,
    s.created_at
ORDER BY s.id DESC;
```

## 13. Post-deployment Checks

After deployment:

- Check browser console.
- Check network errors.
- Check mobile layout.
- Check form submission.
- Check 404 status code.
- Check IIS logs.
- Check Umbraco logs.
- Check SQL Server connection.
- Confirm HTTPS works.
- Confirm HTTP redirects to HTTPS if required.

## 14. Known Future Improvements

Planned improvements:

- Separate APIs for each form type.
- Dedicated contact message table.
- Dedicated job application table.
- Resume upload support.
- SEO metadata fields.
- Open Graph metadata.
- Sitemap and robots.txt.
- Further CSS component refactoring.
- Automated deployment pipeline.

## Common IIS Issues Fixed During Deployment

- Installed .NET Hosting Bundle to register AspNetCoreModuleV2.
- Changed the IIS application pool to No Managed Code.
- Ensured the site uses the correct application pool.
- Fixed SQL Server connection string for production.
- Verified Umbraco logs under `umbraco/Logs`.