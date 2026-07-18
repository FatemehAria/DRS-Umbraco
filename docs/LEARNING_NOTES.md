# Learning Notes

## 2026-07-13 - Request Flow in Umbraco

### Goal

Understand how a request goes from IIS to ASP.NET Core, then to Umbraco, and finally renders a Razor template.

### Big Picture

Browser request -> IIS -> ASP.NET Core -> Program.cs -> Umbraco -> Razor View -> HTML response

### Program.cs

Program.cs is the entry point of the ASP.NET Core application.

It does three main things:

1. Registers services:
   - Controllers
   - Elementor submission service

2. Configures Umbraco:
   - Backoffice
   - Website
   - Composers

3. Builds and runs the request pipeline:
   - Boots Umbraco
   - Enables Umbraco middleware/endpoints
   - Maps API controllers
   - Runs the application

Important idea:

`builder.Services...` is for registering things before the app is built.

`app.Use...` and `app.Map...` are for defining how requests are handled after the app is built.

In this project:

- CMS pages go through Umbraco website routing.
- Form submissions go through ASP.NET Core controllers.

### Backoffice in Program.cs

`AddBackOffice`, `UseBackOffice`, and `UseBackOfficeEndpoints` enable the Umbraco admin panel at `/umbraco`.

The public website uses:

- `AddWebsite`
- `UseWebsite`
- `UseWebsiteEndpoints`

The admin panel is not public access to content management; it still requires authentication.

Important deployment note:

Umbraco backoffice authentication may require browser Web Crypto APIs. These APIs are available only in secure contexts such as HTTPS or localhost. Therefore `/umbraco` may work on `localhost` but fail on an HTTP LAN IP address like `http://SERVER-IP:8080/umbraco`.

For demo:

- show the public site through `http://SERVER-IP:8080`
- show backoffice through `http://localhost:8080/umbraco` on the server, or configure HTTPS

### Master.cshtml

`Master.cshtml` is the shared layout for the public website.

It contains the parts that are common across pages:

- HTML document structure
- SEO metadata
- CSS reference
- Header
- Navigation
- Footer
- JavaScript reference

Each page template provides its own content, and that content is inserted into the layout through:

```csharp
@RenderBody()
```

Important idea:

Model represents the current Umbraco content node being rendered.

Examples:

On /products/, Model is the Products content node.
On /contact-us/, Model is the Contact page content node.
On a news article page, Model is that specific article node.

SEO fields are read from the current page using:

```
Model.Value<string>("seoTitle")
Model.Value<string>("seoDescription")
```

If SEO fields are empty, the layout falls back to other fields like:

pageTitle
title
summary
introText
Model.Name

Master.cshtml should be changed carefully because it affects almost every page.

### Open Graph

Open Graph meta tags control how a page looks when its link is shared in social platforms or messaging apps.

Common tags:

- `og:title`
- `og:description`
- `og:image`
- `og:url`
- `og:type`

In this project, Open Graph values are generated in `Master.cshtml` from the current Umbraco page fields.

### Canonical URL

Canonical URL tells search engines the official URL of a page.

It helps avoid duplicate-content confusion when the same page can be reached through multiple URLs.

Example:

```html
<link rel="canonical" href="https://example.com/products/" />
```

In this project, the canonical URL is built from the current request scheme, host, and the current Umbraco page URL.

### RenderBody

@RenderBody() is the place inside Master.cshtml where the content of each specific page template is inserted.

The layout provides shared parts like header, footer, CSS, JS, and SEO tags.

The page template provides the unique content.

### HomePage.cshtml

`HomePage.cshtml` is the template for the Home content node.

It uses:

```csharp
Layout = "Master.cshtml";
```

So the Home page content is inserted into Master.cshtml through @RenderBody().

The page reads its title from Umbraco:

```
Model.Value<string>("pageTitle")
```

Then passes it to the layout:

```
ViewData["PageTitle"] = pageTitle;
```

The Home page has two dynamic sections:

1. Latest news:

- Finds all content nodes with alias newsArticle
- Sorts by publishDate
- Takes the latest 3

2. Home products:

- Finds all content nodes with alias productItem
- Sorts by Umbraco sort order
- Takes the first 3

### Important idea:

Not every part of the Home page is CMS-managed.

- Dynamic from Umbraco:

Products
News
Page title

- Static in Razor:

Hero
Capabilities
Client logos
Testimonials
Some form text

The demo form is submitted by JavaScript using the form id:

```
<form id="consultRequestForm">
```

The hidden formName tells the backend which form type this submission belongs to.

### Form Submit Flow in sama.js

The form is submitted with JavaScript instead of normal HTML form submission.

The form is found by its id:

```html
<form id="consultRequestForm"></form>
```

JavaScript listens to the submit event and prevents the default browser reload:

```
event.preventDefault();
```

Then it builds a JSON payload from form fields using FormData.

Important field names:

- formName
- fullName
- mobile
- requestType
- message

The payload is sent to:

```
POST /api/consult-requests
```

If the API fails, an error message is shown.

Important idea:

The form depends on consistency between:

1. HTML field names
2. JavaScript payload
3. C# backend model

### ConsultRequestsController

`ConsultRequestsController` receives form submissions from the frontend.

The frontend sends:

```text
POST /api/consult-requests
```

The controller route is:

```
[Route("api/consult-requests")]
```

The Create action handles POST requests:

```
[HttpPost]
public async Task<IActionResult> Create(...)
```

The JSON request body is converted into a C# model using:

```
[FromBody] ConsultRequestCreateModel model
```

The controller does not save directly to SQL Server. Instead, it calls:

```
IElementorSubmissionService.CreateConsultRequestAsync(...)
```

Important idea:

Controller responsibilities:

- Receive HTTP request
- Validate input
- Collect request metadata like referer, user agent, and IP
- Call the service
- Return HTTP response

Service responsibilities:

Handle the actual form persistence logic
Work with SQL Server

### Controller vs Service

A controller should not contain database insert logic directly.

Controller responsibilities:

- Receive HTTP requests
- Validate input
- Read request metadata
- Call the appropriate service
- Return HTTP responses

Service responsibilities:

- Handle business/persistence logic
- Work with SQL Server
- Manage transactions
- Insert or update data

Important idea:

Separating controller and service keeps the code cleaner and easier to maintain.

SQL Injection is prevented by using parameterized SQL queries, not simply by moving SQL code into a service.

### ConsultRequestCreateModel

`ConsultRequestCreateModel` is the input model for the consult request API.

It represents the JSON body sent from the frontend to:

```text
POST /api/consult-requests
```

Important fields:

- FullName
- Mobile
- RequestType
- Message
- FormName

Validation is done using DataAnnotations:

```
[Required]
[StringLength(100)]
```

mportant idea:

Frontend validation is not enough.

Even if HTML inputs have required, the backend must validate the model because users can call the API directly.

The controller checks model validity through ModelState.

If the model is invalid, the request should not reach the service or database.

### IElementorSubmissionService

`IElementorSubmissionService` is an interface.

An interface is a contract. It defines what a service must do, but not how it does it.

This interface says that any implementation must provide:

```csharp
Task<decimal> CreateConsultRequestAsync(...)
```

The controller depends on the interface instead of the concrete class.

Important idea:

The controller should not care how form submissions are saved.

It only calls the contract:

```
IElementorSubmissionService
```

The real implementation is connected in Program.cs:

```
builder.Services.AddScoped<IElementorSubmissionService, ElementorSubmissionService>();
```

### Interface vs Implementation

An interface defines the contract.

It specifies:

- Method name
- Parameters
- Return type

It does not specify the implementation details.

In this project:

`IElementorSubmissionService` defines what the service must do.

`ElementorSubmissionService` defines how the service actually does it.

### ElementorSubmissionService

`ElementorSubmissionService` is the real implementation of `IElementorSubmissionService`.

It saves form submissions into the legacy Elementor/WordPress tables.

Main responsibilities:

- Read the SQL Server connection string from configuration
- Open a SQL connection
- Start a transaction
- Insert the main submission row into `wp_e_submissions`
- Insert form field values into `wp_e_submissions_values`
- Commit if everything succeeds
- Rollback if anything fails

Important idea:

A transaction keeps related database operations consistent.

The form submission needs multiple inserts. If one insert succeeds and another fails, the database could become inconsistent.

So the service uses:

```csharp
BeginTransactionAsync()
CommitAsync()
RollbackAsync()
```

SQL Injection is prevented by using parameterized SQL queries like:

```
@Value
```

instead of concatenating user input into SQL strings.

The controller handles HTTP.

The service handles persistence.

### Form Metadata Mapping

The form submission service now maps each `formName` to its Elementor metadata.

Before this refactor, only `formName` changed, but values like `post_id`, `main_meta_id`, `element_id`, and `edit_post_id` were hard-coded for the consult form.

Now the service uses a form definition object.

Important idea:

When different form types are saved into legacy Elementor tables, each form may need its own metadata.

Examples:

- `consult_form`
- `job_interest_form`
- `contact_form`
- `product_demo_form`

This makes the service easier to extend later.

### SQL Result of Form Submission

Each form submission is saved in two levels.

`wp_e_submissions` stores the main submission record.

Example fields:

- `id`
- `form_name`
- `post_id`
- `main_meta_id`
- `element_id`
- `meta`
- `created_at`

`wp_e_submissions_values` stores the submitted form fields as key/value rows.

Example:

```text
submission_id = 9 | key = fullname     | value = تست شغل
submission_id = 9 | key = mobile       | value = 09120000000
submission_id = 9 | key = request_type | value = فرصت شغلی - تست
submission_id = 9 | key = message      | value = تست mapping فرم شغلی

Important idea:

wp_e_submissions.id connects to wp_e_submissions_values.submission_id.

The main table stores the submission header.

The values table stores the form field details.
```

### ProductListPage.cshtml

`ProductListPage.cshtml` is the template for the Products list page.

In this template:

`Model` represents the Products content node.

The page reads its own fields from Umbraco:

```csharp
Model.Value<string>("pageTitle")
Model.Value<string>("introText")
```
It reads products from child content nodes:

```
Model.Children()
    .Where(x => x.ContentType.Alias == "productItem")
    .OrderBy(x => x.SortOrder)
```
Important idea:

Model.Children() reads only the direct children of the current page.

This is different from searching the whole site with:

```
Umbraco.ContentAtRoot()
    .SelectMany(x => x.DescendantsOrSelf())
```

Each product card uses:

- product.Value<string>("title")
- product.Value<string>("summary")
- product.Value<MediaWithCrops>("icon")
- product.Url()

If no products exist, the page shows an empty state instead of rendering a broken layout.

### ProductItem.cshtml

`ProductItem.cshtml` is the template for a single product detail page.

In this template:

`Model` represents the current product content node.

The page reads product fields from Umbraco:

```csharp
Model.Value<string>("title")
Model.Value<string>("summary")
Model.Value<MediaWithCrops>("icon")
Model.Value("body")
```

The product list URL is read from the parent node:

```
Model.Parent()?.Url() ?? "/products/"
```

Important idea:

ProductListPage.cshtml displays a list of products.

ProductItem.cshtml displays one specific product.

The product demo form uses:

```
<input type="hidden" name="formName" value="product_demo_form" />
```

This tells the backend that the submission came from a product demo form.

### NewsListPage.cshtml

`NewsListPage.cshtml` is the template for the news listing page.

In this template, `Model` represents the News list content node.

The page reads its own fields:

```csharp
Model.Value<string>("pageTitle")
Model.Value<string>("introText")
```

It reads news articles from child content nodes:

```
Model.Children()
    .Where(x => x.ContentType.Alias == "newsArticle")
```

News items are sorted from newest to oldest:

```
.OrderByDescending(x => x.Value<DateTime?>("publishDate") ?? x.CreateDate)
```

Important idea:

Products are sorted by manual CMS order.

News articles are sorted by publish date.

Each news card links to the article detail page using:

```
item.Url()
```

### NewsArticle.cshtml

`NewsArticle.cshtml` is the template for a single news article.

In this template, `Model` represents the current article content node.

The article fields are read from Umbraco:

```csharp
Model.Value<string>("title")
Model.Value<string>("summary")
Model.Value<DateTime?>("publishDate")
Model.Value<MediaWithCrops>("coverImage")
Model.Value("body")
```

The back link is generated from the parent node:

```
Model.Parent()?.Url()
```

Important idea:

NewsListPage.cshtml displays a list of articles.

NewsArticle.cshtml displays one specific article.

The list page uses article fields to build cards.

The article page uses the same fields to render the full article.

### ContactPage.cshtml

`ContactPage.cshtml` is the template for the contact page.

In this template, `Model` represents the Contact content node.

The page reads contact fields from Umbraco:

```csharp
Model.Value<string>("pageTitle")
Model.Value<string>("introText")
Model.Value<string>("address")
Model.Value<string>("phone")
Model.Value<string>("workingHours")
```

If contact fields are empty, the template shows fallback values.

The contact form uses the same JavaScript form flow as the consult form:

```
<form id="consultRequestForm">
```

The hidden form name identifies this form as a contact form:

```
<input type="hidden" name="formName" value="contact_form" />
```

The form also has a custom success message:

```
data-success-message="..."
```

Important idea:

Multiple forms can share the same JavaScript and backend endpoint if their field names are consistent.

### RecruitmentPage.cshtml

`RecruitmentPage.cshtml` is the template for the recruitment listing page.

In this template, `Model` represents the Recruitment content node.

The page reads its own fields:

```csharp
Model.Value<string>("pageTitle")
Model.Value<string>("introText")
```

It reads job openings from child content nodes:

```
Model.Children()
    .Where(x => x.ContentType.Alias == "jobOpening")
    .OrderBy(x => x.SortOrder)
```

Each job card reads fields from a jobOpening node:

- title
- department
- location
- employmentType
- summary

Each card links to the job detail page using:

```
job.Url()
```

Important idea:

The Recruitment page is a listing page.

Each Job Opening is a detail page.

### SEO, Sitemap, robots.txt, and 404

The project includes basic production-readiness features for public website publishing.

SEO metadata is handled in `Master.cshtml`.

Each page can provide:

- `seoTitle`
- `seoDescription`
- `seoImage`

If SEO fields are empty, the layout falls back to page fields like:

- `pageTitle`
- `title`
- `Model.Name`

The sitemap is generated by `SitemapController` at:

```text
/sitemap.xml
```

It reads published Umbraco pages and excludes the 404 page.

robots.txt is stored in:

```
wwwroot/robots.txt
```

It allows public crawling but blocks paths like:

/umbraco/
/api/

The custom 404 page is configured through Umbraco using the page content key.

Important idea:

SEO helps describe individual pages.

Sitemap helps search engines discover pages.

robots.txt guides crawlers.

404 improves the user experience when a page is not found.

---

مسیر کلی تا اینجا:

```

User fills form
↓
HTML form in Razor template
↓
sama.js catches submit
↓
event.preventDefault()
↓
FormData reads inputs
↓
JSON payload is built
↓
fetch POST /api/consult-requests
↓
ASP.NET Core routing
↓
ConsultRequestsController.Create()
↓
JSON body → ConsultRequestCreateModel
↓
Model validation
↓
IElementorSubmissionService
↓
ElementorSubmissionService
↓
SQL connection opens
↓
SQL transaction starts
↓
INSERT wp_e_submissions
↓
INSERT wp_e_submissions_values
↓
Commit
↓
Controller returns success JSON
↓
sama.js shows success message

```
