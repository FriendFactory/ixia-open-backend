# Server splitting

## Reasoning

There are two group of users: admin and regular users.
They have very different requirements for accessing data:
conditions under which data should be visible, access checks, set of fields etc.

Also there are very different requirements to code quality and performance:
the API for clients should have the highest quality and speed,
meanwhile in admin stuff API some non-critical bugs could be acceptable, and some waiting for response could be ok.

But currently access to such data is performed via single service.
That cause a lot of issues in code quality: complex unified code that works with generic entities,
a lot of conditions to separate endpoints or branches for different permissions, etc.

## Solution overview

- Separate main service to two services: client and admin
- (Possible) merge UserAPI server to client and admin service (I'm sure there is no reason to have user service as microservice)
- Reuse existing code for generic access entities (OData) and entity modification in admin service
- Write fresh code for client service and cover it with unit tests
- Get rid of caching for admin service and improve caching for client service
- Apply global security check for user permission in admin service (to don't forget to do that in each endpoint/service)
- Develop and add high-quality logging (in particular to CloudWatch) and create alerts that allows to early catch errors in app

## Steps to implement

The solution should be implemented iteratively and incrementaly.
Application must remain working due the migration to new system.

The idea of how to do that is to have main service running until the end of migration.
The client app will switch to new client service endpoints one by one, final step would be main service deletion.

Due the migration process we will have 3 services:

- main (that's used by client)
- assetmanager (used by CMS and asset migration)
- client (that's new service would be used by client after migration completed)

On migration completed the main service will be removed.
Assetmanager service will contains only admin stuff.

### Preparation steps

- Copy a code from main server to assetmanager.
  Currently main and assetmanager services shares codebase, that should be changed
  to allow changing of assetmanaer service without breaking the main service.
- Add global permission check for assetmanager service (to disable accidental using by client)

### Feature migrations steps

- Implement feature in client service, cover it with unit tests
- Remove feature from assetmanager service (main service remains intact)
- Update bridge and client
- Update load test
- QA and regression for client and CMS (to ensure we don't delete API required for CMS)

### Final steps

- Remove main service, regression
- Update asset migration if needed (only config update would be needed)
- Test asset migration

## Technical things to achieve and improve

- Get rid of caching for assetmanager service (since no performance requirements for assetmanager service no reason to complicate code)
- Get rid of our patched protobuf library: since we will have no recursive structures in client service no reason to add those extra checks)
- For client service: use protobuf to store data in cache
- Use single db context and entities for all microservices
