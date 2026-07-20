## Backoffice User Access

Created a limited user group for non-technical content management.

### User Group

- Name: Content Editor

### Allowed Sections

- Content
- Media

### Hidden / Restricted Sections

- Settings
- Users
- Document Types
- Templates
- Data Types

### Document Permissions

Enabled:

- Read
- Create
- Update
- Publish
- Unpublish

Disabled for safety:

- Delete
- Move
- Copy
- Rollback
- Public Access
- Notifications
- Create Document Blueprint

### Property Value Permissions

Enabled:

- UI Read
- UI Write

### Test Result

A test user was created and confirmed to only see:

- Content
- Media

The test user was able to:

- Edit Home Page content
- Edit Site Settings content
- Upload/select Media
- Save and Publish content

The test user could not access technical configuration sections.

## Customer Portal

A customer portal page was added to make the website more useful for existing clients.

### Page

- Customer Portal Page
- URL: `/customer-portal/`

### Purpose

The page acts as a gateway to customer-facing systems and services, such as:

- CRM / support ticket system
- Documentation and user guides
- Demo or consultation request
- Other customer systems

### Editable Structure

The portal cards are managed from Umbraco using a Block List.

Each card includes:

- Card title
- Card description
- Button text
- URL
- Open in new tab option

### Navigation

The Customer Portal link was added to:

- Header navigation
- Footer quick links
