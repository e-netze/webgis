# layout: Layout
## home: Home
## login: Login
## logout: Logout
## language: Language
## back: Back

# home: Home
## title: WebGIS API
## intro: The interface for interactive WebGIS maps: REST services, JavaScript API and management of API clients and portal pages.
## subscriber-title: Subscriber Login
## subscriber-description: Manage API clients and portal pages
## rest-title: REST API
## rest-description: Explore services, maps and queries via the REST interface
## jsapi-title: JavaScript API
## jsapi-description: JavaScript API reference (documentation)

# forbidden: Forbidden
## title: Access denied
## text: The signed-in user is not allowed to access this page.
## username: Username
## roles: Roles
## claims: Claims
## logout: Logout

# unknown-subscriber: Unknown subscriber
## title: Unknown subscriber
## text: The subscriber id is unknown. Check the subscriber id in application-security.json and map it to an existing subscriber.

# rest-login: REST Login
## title: REST Login
## subtitle: Sign in with the credentials of an API client
## client-id: Client Id
## client-secret: Client Secret
## submit: Login

# error: Error
## title: WebGIS API Error
## subtitle: An error occurred while processing the request:
## details: Details
## home: Go to home page

# list: Lists
## actions: Actions
## edit: Edit

# form: Forms
## cancel: Cancel
## save: Save
## create: Create
## update: Update
## step-general: General
## step-security: Security
## step-creation: Creation
## created: Created (UTC)

# subscriber: Subscriber
## title: Subscriber
## greeting: Hello, {0}
## section-admin: Administration
## admin-subscribers-title: Manage subscribers
## admin-subscribers-description: View and edit API subscribers
## section-database: Database
## setup-title: Setup
## setup-description: Set up database(s) or check their state
## migrate-title: Migration
## migrate-description: Migrate database(s)
## section-security: Security
## security-report-title: Security report
## security-report-description: Review security-relevant settings
## section-api-cache: API cache
## cache-clear-title: Clear cache
## api-cache-clear-description: Clear the API cache
## gc-title: Garbage collection
## gc-description: Force the garbage collector
## section-portal-cache: Portal cache
## portal-cache-clear-description: Clear the portal and user cache
## section-portal-diagnostics: Portal diagnostics
## usercache-title: User cache
## usercache-description: List users in the user cache
## userkeys-title: Current user keys
## userkeys-description: Show the HMAC keys and roles of the current user
## instance-info-title: Instance info
## instance-info-description: Instance information including request headers
## section-tools: Tools
## clients-title: API clients
## clients-description: Manage your API clients
## portals-title: Portal pages
## portals-description: Manage your portal pages

# admin-subscribers: Manage subscribers
## title: Manage subscribers
## username: Username
## name: Name
## empty: There are no subscribers.

# clients: API clients
## title: API clients
## name: Name
## referer: HTTP referer
## rest: REST
## mapbuilder: MapBuilder
## new: New API client
## empty: There are no API clients yet.

# client-edit: Edit API client
## title-new: New API client
## title-update: Edit API client
## client-name: Client name
## client-referer: Client HTTP referer
## client-id: Client id
## client-secret: Client secret

# portals: Portal pages
## title: Portal pages
## id: Portal id
## name: Name
## open: Open portal
## new: New portal page
## empty: There are no portal pages yet.

# portal-edit: Edit portal page
## title-new: New portal page
## title-update: Edit portal page
## id: Portal id
## name: Portal name
## description: Description
## banner: Banner image
## users: Authorized users
## map-authors: Authorized map authors
## content-authors: Authorized content authors
## step-meta: Meta
## meta-tags: HTML meta tags
## owner: Owner

# subscriber-form: Subscriber form
## username: Username
## first-name: First name
## last-name: Last name
## email: Email
## current-password: Current password
## password: New password
## password-repeat: Repeat password

# register: Register
## title: Register new subscriber
## step: Subscriber
## submit: Register
## password-hint: At least {0} characters

# subscriber-edit: Edit subscriber
## title: Edit subscriber
## settings: Settings
## save-settings: Save settings
## password: Change password
## save-password: Change password
## mfa-reset: Reset two-factor authentication
## mfa-reset-confirm: Reset two-factor authentication for this subscriber? All sessions of the subscriber will be terminated.

# security-report: Security report
## title: Security report
## intro: Shows the security-relevant settings of the api.config, their risk and an assessment for internet and intranet installations. The recommendations correspond to the table "Overview of all security-relevant keys" in the security annex of the admin documentation. Secrets and connection strings are only shown as set / not set.
## docs-link: Open security annex
## docs-url: https://docs.webgiscloud.com/en/webgis/annex/security.html
## login-disabled: The subscriber login is disabled (allow-subscriber-login). Settings that only affect the login are shown as not relevant.
## no-credentials: No secure endpoint credentials are configured (security:secure-endpoint-url-password or security:secure-endpoint-basicauth-*). This page is therefore only reachable directly on the server (localhost).
## internet: Internet
## intranet: Intranet
## status-ok: OK
## status-warning: Deviation (medium)
## status-critical: Deviation (high)
## status-hint: Hint
## status-na: Not relevant
## category-optout: Compatibility opt-out
## category-feature: Feature enablement
## category-tuning: Fine tuning
## risk-high: High
## risk-medium: Medium
## risk-low: Low
## risk-info: Info
## only-findings: Show deviations only
## settings: Settings
## col-key: Key
## col-value: Current value
## col-default: Default
## col-category: Category
## col-risk: Risk
## col-docs: Docs
## set: set
## not-set: not set
## default: Default
## annex: Annex
## subscribers: Subscribers
## subscribers-error: The subscriber list could not be read (see log).
## subscribers-summary: {0} subscribers, {1} of them with two-factor authentication (2FA). Only user names are shown.
## col-user: User
## col-role: Role
## col-mfa: 2FA
## col-mfa-required: 2FA required
## col-recovery: Remaining recovery codes
## role-admin: Administrator
## role-subscriber: Subscriber
## mfa-enabled: Enabled
## mfa-disabled: Not enabled
## mfa-na: Not available
## yes: yes
## no: no