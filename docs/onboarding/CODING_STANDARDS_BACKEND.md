# EstateSync — Backend Coding Standards

ASP.NET Core 8, EF Core with PostgreSQL, Stripe, QuestPDF. Project lives at `backend/EstateSync.Api/`.

Follow the pattern of an existing feature (Leases or Complaints are good references) whenever you're unsure — consistency matters more than cleverness.

## 1. Layering: Controller → Service → Repository

- Controllers only receive the request, read the user id from the JWT, call one service method, and return the response. No business logic, no repository access, no DbContext. The only exception is SystemController (dev-only seeding).
- Services own all business logic: state-transition checks, ownership and authorization checks, orchestration, and notifications. They depend on interfaces only — never on AppDbContext directly.
- Repositories contain only EF Core queries. No business rules, no service calls. GetById returns null when not found; the service decides whether that becomes a NotFoundException.
- Always verify the caller owns or is allowed to act on the entity before changing it. Throw ForbiddenException if not.

## 2. Saving data

- All repositories share one DbContext per request through the Unit of Work.
- Call SaveChangesAsync exactly once, at the end of a write operation. That single save is one database transaction, so atomicity comes for free.
- Never add manual transactions (BeginTransaction). The only accepted exception in the codebase is lease creation, which saves twice (lease, then PDF URL).

## 3. Errors

- Throw domain exceptions from services: NotFoundException, BadRequestException, ForbiddenException, ConflictException (all in the Exceptions folder).
- The GlobalExceptionHandler middleware turns them into proper HTTP responses. Never catch-and-return raw errors from a controller.
- Never swallow an exception silently. If you catch one, either translate it (for example a Postgres unique-violation into ConflictException) or rethrow it.

## 4. Response envelope (non-negotiable)

Every endpoint must wrap its result in ApiResponse or PagedResponse. A raw DTO or array compiles and even works in Postman, but the Angular client unwraps the "data" field on everything, so an unwrapped response shows up as a silently empty list in the UI. This has already caused a real bug once — do not repeat it.

## 5. Naming and file locations

- Controllers: FeatureController.cs in Controllers, route is api/v1/[controller].
- Service interfaces: IFeatureService.cs in Interfaces/Services/Feature; implementations in Services/Feature.
- Repository interfaces: IFeatureRepository.cs in Interfaces/Repositories.
- DTOs: ActionFeatureRequest.cs and FeatureResponse.cs in DTOs/Feature.
- Validators: DtoValidator.cs in Validators. Mappers: FeatureMapper.cs in Mappers, registered as Singleton.
- Entity configurations: EntityConfiguration.cs in Data/Configurations.
- Watch out: the route token is PascalCase, so RentalApplicationsController maps to /api/v1/RentalApplications, not kebab-case. Keep frontend URLs in sync.

## 6. Validation

Every request DTO gets a FluentValidation validator. Validators check shape and format only; business rules (state, ownership) belong in the service — don't duplicate them.

## 7. SOLID house rules

- One service per domain. If a service passes roughly 150 lines or injects more than 5 repositories, split it. PropertyService is known debt — do not add more dependencies to it.
- Add new behaviour with new services or validators, not by growing existing switch statements.
- Never add stub or unimplemented methods to an interface.
- Services never inject AppDbContext.

## 8. Enums and EF gotchas

- Enum values are append-only: add new members at the end, never renumber or reorder, because the frontend mirrors the numeric values.
- LeaseStatus is stored as a string in the database (HasConversion to string). Any raw SQL, index filter, or migration touching that column must use the names like 'Active', not numbers.
- A lease's ApplicationId has a partial unique index: only one live (not Expired/Terminated/Voided) lease per application. A violation surfaces as Postgres error 23505 and is translated to ConflictException.
- All DateTime values must be UTC. For dates coming from the client, set the kind to UTC explicitly — Postgres rejects unspecified kinds.

## 9. Cross-cutting services — use them, don't bypass them

- Files: always go through IFileStorageService; never touch the disk from a service (the lease PDF generator is the one exception).
- Notifications: every user-visible status change sends a notification through NotificationSender, which fans out to a database row, a SignalR push, and — for important types — an email. When adding a new NotificationType, decide whether it belongs in the email-worthy list.
- Email: never send SMTP inline. Enqueue a message on IEmailQueue; the background EmailDispatcher delivers it with retries. A mail failure must never fail a business operation.
- Audit: writes are logged automatically by a DbContext interceptor — nothing to do, just don't work around the DbContext.

## 10. Async

Every service and repository method is async and accepts a CancellationToken passed down from the controller. Background work uses BackgroundService with IServiceScopeFactory (see Services/Background) — never fire-and-forget tasks from a request.

## Pre-merge checklist

- Controller calls one service method — no repo, no DbContext, no logic
- Service checks ownership/authorization before acting
- Single SaveChangesAsync at the end of the write
- NotFoundException thrown when an entity is missing
- Validator exists for every new request DTO
- Response wrapped in ApiResponse / PagedResponse
- Notification sent for every status transition
- New enum values appended at the end
- dotnet build and dotnet test pass
