# Final QA Checklist

Final quality checklist for the DRS Umbraco CMS Pilot.

## 1. Main Pages

Check these pages manually:

- `/`
- `/products/`
- Product detail pages
- `/news/`
- News article pages
- `/about-us/`
- `/contact-us/`
- `/recruitment/`
- Job detail pages
- Custom 404 page
- `/sitemap.xml`
- `/robots.txt`

## 2. Layout

Check:

- Header displays correctly.
- Footer displays correctly.
- Logo is visible.
- Navigation links work.
- Active pages do not return 404.
- Mobile menu opens and closes correctly.

## 3. Responsive Testing

Test these widths:

- 390px
- 430px
- 768px
- 1024px
- Desktop width

Check:

- Header does not break.
- Hero sections are readable.
- Cards stack correctly.
- Forms fit the screen.
- Footer remains clean.
- No horizontal scroll.

## 4. Forms

Test:

- Home demo form
- Contact form
- Product demo form
- Job interest form

Verify in SQL Server:

- `consult_form`
- `contact_form`
- `product_demo_form`
- `job_interest_form`

## 5. SEO

Check page source for important pages:

- `<title>`
- `<meta name="description">`
- `<link rel="canonical">`
- Open Graph tags
- Twitter card tags

For 404 page:

- `noindex,follow`

## 6. Sitemap and Robots

Check:

- `/sitemap.xml` loads.
- `/robots.txt` loads.
- Sitemap includes public pages.
- Sitemap does not include `/umbraco/`.
- Sitemap does not include `/api/`.
- Sitemap does not include the 404 page.

## 7. Browser Console

Check browser console on main pages.

There should be no critical red errors.

## 8. Git Safety

Before pushing:

```bash
git status
```

Make sure these files are not committed:

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
appsettings.*.json
```

Only this sample config should be committed:

```text
appsettings.example.json
```

## 9. Build

Run:

```bash
dotnet build
```

The build should complete successfully.

## 10. Final Notes

Known future improvements:

- Separate API endpoints for each form type
- Resume upload support
- More SEO fields if needed
- Production IIS deployment
- Further CSS refactoring