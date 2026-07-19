using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string TestDocumentNotice = "TEST DOCUMENT - NOT A REAL ID OR LEGAL RECORD";
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
};

var command = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ?? "help";
var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
var manifestPath = GetOption(args, "--manifest") ?? Path.Combine(AppContext.BaseDirectory, "seed-manifest.json");
var statePath = GetOption(args, "--state") ?? Path.Combine(Environment.CurrentDirectory, "tools", "production-seeder", "state.json");
var generatedRoot = GetOption(args, "--generated") ?? Path.Combine(Environment.CurrentDirectory, "tools", "production-seeder", "generated");

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"Manifest not found: {manifestPath}");
    return 2;
}

var manifest = JsonSerializer.Deserialize<SeedManifest>(await File.ReadAllTextAsync(manifestPath), json)
    ?? throw new InvalidOperationException("Seed manifest is empty or invalid.");
var validationErrors = ManifestValidator.Validate(manifest);
if (validationErrors.Count > 0)
{
    Console.Error.WriteLine("Manifest validation failed:");
    foreach (var error in validationErrors) Console.Error.WriteLine($"  - {error}");
    return 2;
}

if (command.Equals("validate", StringComparison.OrdinalIgnoreCase))
{
    PrintPlan(manifest);
    Console.WriteLine("Manifest validation passed.");
    return 0;
}

var supportedNetworkCommands = new[] { "prepare", "tenants", "owners", "status", "properties" };
if (!supportedNetworkCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("EstateSync production test-data seeder");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  validate                 Validate and summarize the manifest without network calls");
    Console.WriteLine("  prepare [--dry-run]      Register owners and prepare/submit their synthetic KYC");
    Console.WriteLine("  tenants [--dry-run]      Register tenants and prepare/submit their synthetic KYC");
    Console.WriteLine("  owners [--dry-run]       Verify KYC approval, grant Owner role, create Stripe test links");
    Console.WriteLine("  status [--dry-run]       Check KYC and Stripe owner-verification status");
    Console.WriteLine("  properties [--dry-run]   Create properties, upload assets, and submit for approval");
    Console.WriteLine();
    Console.WriteLine("Options: --manifest PATH --state PATH --generated PATH");
    return command.Equals("help", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
}

PrintPlan(manifest);
if (dryRun)
{
    Console.WriteLine();
    Console.WriteLine("DRY RUN: no users, files, or remote resources were created.");
    if (!command.Equals("tenants", StringComparison.OrdinalIgnoreCase))
        foreach (var owner in manifest.Owners)
            Console.WriteLine(command.ToLowerInvariant() switch
            {
                "prepare" => $"  Would prepare {owner.Email}: register/login, 4 KYC PDFs, submit for Admin review",
                "owners" => $"  Would activate {owner.Email}: verify KYC approval, grant Owner role, create Stripe test onboarding link",
                "properties" => $"  Would seed {owner.Properties.Count} properties for {owner.Email}, including 6 images and 1 ownership PDF each",
                _ => $"  Would check KYC and Stripe status for {owner.Email}"
            });
    if (command.Equals("tenants", StringComparison.OrdinalIgnoreCase))
        foreach (var tenant in manifest.Tenants)
            Console.WriteLine($"  Would prepare tenant {tenant.Email}: register/login, 4 KYC PDFs, submit for Admin review");
    return 0;
}

var apiUrl = RequireEnvironment("ESTATESYNC_API_URL").TrimEnd('/');
if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
{
    Console.Error.WriteLine("ESTATESYNC_API_URL must be an absolute HTTPS URL.");
    return 2;
}

var password = RequireEnvironment("ESTATESYNC_SEED_PASSWORD");
if (password.Length < 12)
{
    Console.Error.WriteLine("ESTATESYNC_SEED_PASSWORD must contain at least 12 characters.");
    return 2;
}

Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
Directory.CreateDirectory(generatedRoot);
var state = await SeederState.LoadAsync(statePath, json);

using var http = new HttpClient
{
    BaseAddress = new Uri(apiUrl + "/"),
    Timeout = TimeSpan.FromSeconds(90)
};
http.DefaultRequestHeaders.UserAgent.ParseAdd("EstateSync-ProductionSeeder/1.0");

var client = new EstateSyncClient(http, json);
var waitingForEmailVerification = new List<string>();
var submittedForApproval = new List<string>();

if (command.Equals("tenants", StringComparison.OrdinalIgnoreCase))
{
    var waitingForVerification = new List<string>();
    foreach (var tenant in manifest.Tenants)
    {
        Console.WriteLine();
        Console.WriteLine($"[{tenant.Key}] {tenant.Email}");
        var tenantState = state.GetOrAddTenant(tenant.Key, tenant.Email);
        if (!tenantState.RegistrationAttempted)
        {
            var registered = await client.RegisterAsync(tenant.FullName, tenant.Email, tenant.PhoneNumber, password);
            tenantState.RegistrationAttempted = true;
            tenantState.RegisteredBySeeder = registered;
            await state.SaveAsync(statePath, json);
            Console.WriteLine(registered ? "  Registered." : "  Account already exists; continuing safely.");
        }

        var token = await client.TryLoginAsync(tenant.Email, password);
        if (token is null)
        {
            waitingForVerification.Add(tenant.Email);
            Console.WriteLine("  Login unavailable. Verify this email, then rerun tenants.");
            continue;
        }
        client.SetBearerToken(token);
        var active = (await client.GetMyKycAsync())
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault(item => item.Status is "Draft" or "Pending" or "Approved");
        if (active?.Status is "Pending" or "Approved")
        {
            tenantState.KycId = active.KycVerificationId;
            tenantState.KycStatus = active.Status;
            await state.SaveAsync(statePath, json);
            Console.WriteLine($"  KYC already {active.Status}; skipped.");
            continue;
        }

        var kyc = active ?? await client.CreateKycDraftAsync();
        tenantState.KycId = kyc.KycVerificationId;
        var existingTypes = kyc.Documents.Select(document => document.DocumentType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var documentType in new[] { "AadhaarFront", "AadhaarBack", "PanCard", "AddressProof" })
        {
            if (existingTypes.Contains(documentType)) continue;
            var outputPath = Path.Combine(generatedRoot, "documents", tenant.Key, $"TEST-{documentType}.pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllBytesAsync(outputPath, SyntheticPdf.Create(
                TestDocumentNotice, $"EstateSync synthetic {documentType}",
                $"Seed tenant: {tenant.Key}", $"Manifest: {manifest.Marker}"));
            await client.UploadKycDocumentAsync(kyc.KycVerificationId, documentType, outputPath);
            Console.WriteLine($"  {documentType}: uploaded.");
        }
        await client.SubmitKycAsync(kyc.KycVerificationId, tenant.AadhaarNumber, tenant.PanNumber);
        tenantState.KycStatus = "Pending";
        await state.SaveAsync(statePath, json);
        submittedForApproval.Add(tenant.Email);
        Console.WriteLine("  KYC submitted for manual Admin approval.");
    }

    Console.WriteLine();
    if (waitingForVerification.Count > 0)
    {
        Console.WriteLine("Verify these tenant emails and rerun the same command:");
        foreach (var email in waitingForVerification) Console.WriteLine($"  - {email}");
    }
    if (submittedForApproval.Count > 0)
    {
        Console.WriteLine("Approve these tenant KYC requests in the Admin UI:");
        foreach (var email in submittedForApproval) Console.WriteLine($"  - {email}");
    }
    return waitingForVerification.Count > 0 ? 3 : 0;
}

if (command.Equals("properties", StringComparison.OrdinalIgnoreCase))
{
    var archiveRoot = Path.Combine(Environment.CurrentDirectory, "tools", "production-seeder", "archive");
    var archiveImages = Directory.Exists(archiveRoot)
        ? Directory.GetFiles(archiveRoot).Where(IsSupportedImage).Order(StringComparer.Ordinal).ToList()
        : [];
    if (archiveImages.Count != manifest.ImagesPerProperty)
        throw new InvalidOperationException($"Expected exactly {manifest.ImagesPerProperty} images in {archiveRoot}, found {archiveImages.Count}.");

    var submitted = 0;
    foreach (var owner in manifest.Owners)
    {
        Console.WriteLine();
        Console.WriteLine($"[{owner.Key}] {owner.Email}");
        var ownerState = state.GetOrAddOwner(owner.Key, owner.Email);
        var token = await client.TryLoginAsync(owner.Email, password)
            ?? throw new InvalidOperationException($"Could not log in {owner.Email}.");
        client.SetBearerToken(token);
        var profile = await client.GetProfileAsync();
        if (!profile.IsOwnerVerified)
            throw new InvalidOperationException($"Stripe onboarding is incomplete for {owner.Email}.");

        var existing = await client.GetMyPropertiesAsync();
        foreach (var property in owner.Properties)
        {
            var propertyState = ownerState.GetOrAddProperty(property.Key);
            var remote = propertyState.PropertyId is { } savedId
                ? existing.FirstOrDefault(item => item.PropertyId == savedId)
                : existing.SingleOrDefault(item => item.Title.Equals(property.Title, StringComparison.OrdinalIgnoreCase));

            if (remote is null)
            {
                remote = await client.CreatePropertyAsync(property);
                propertyState.PropertyId = remote.PropertyId;
                await state.SaveAsync(statePath, json);
                Console.WriteLine($"  {property.Key}: created.");
            }
            else if (propertyState.PropertyId is null)
            {
                propertyState.PropertyId = remote.PropertyId;
                await state.SaveAsync(statePath, json);
                Console.WriteLine($"  {property.Key}: recovered existing property.");
            }

            if (remote.Status != "Draft")
            {
                Console.WriteLine($"  {property.Key}: already {remote.Status}; skipped uploads.");
                continue;
            }

            await client.UpdateAmenitiesAsync(remote.PropertyId, property.Amenities);
            if (remote.Images.Count == 0)
            {
                await client.UploadPropertyImagesAsync(remote.PropertyId, archiveImages);
                Console.WriteLine($"  {property.Key}: {archiveImages.Count} images uploaded.");
            }
            else if (remote.Images.Count != manifest.ImagesPerProperty)
            {
                throw new InvalidOperationException(
                    $"{property.Key} has {remote.Images.Count} images; expected 0 or {manifest.ImagesPerProperty}. Manual review is required.");
            }

            var documents = await client.GetPropertyDocumentsAsync(remote.PropertyId);
            if (!documents.Any(document => document.DocumentType.Equals("OwnershipCertificate", StringComparison.OrdinalIgnoreCase)))
            {
                var outputPath = Path.Combine(generatedRoot, "documents", owner.Key, property.Key, "TEST-OwnershipCertificate.pdf");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await File.WriteAllBytesAsync(outputPath, SyntheticPdf.Create(
                    TestDocumentNotice,
                    "Synthetic ownership certificate",
                    $"Property: {property.Title}",
                    $"Seed owner: {owner.Key}",
                    $"Manifest: {manifest.Marker}"));
                await client.UploadPropertyDocumentAsync(remote.PropertyId, outputPath);
                Console.WriteLine($"  {property.Key}: ownership PDF uploaded.");
            }

            await client.SubmitPropertyAsync(remote.PropertyId);
            propertyState.Status = "PendingApproval";
            await state.SaveAsync(statePath, json);
            submitted++;
            Console.WriteLine($"  {property.Key}: submitted for Admin approval.");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Property phase finished. Newly submitted: {submitted}.");
    return 0;
}

if (command.Equals("status", StringComparison.OrdinalIgnoreCase))
{
    var incomplete = 0;
    foreach (var owner in manifest.Owners)
    {
        var token = await client.TryLoginAsync(owner.Email, password)
            ?? throw new InvalidOperationException($"Could not log in {owner.Email}.");
        client.SetBearerToken(token);
        var profile = await client.GetProfileAsync();
        var kyc = (await client.GetMyKycAsync()).OrderByDescending(item => item.CreatedAt).FirstOrDefault();
        Console.WriteLine($"{owner.Email}: KYC={kyc?.Status ?? "missing"}, StripeOwnerVerified={profile.IsOwnerVerified}");
        if (!profile.IsOwnerVerified) incomplete++;
    }
    return incomplete == 0 ? 0 : 5;
}

if (command.Equals("owners", StringComparison.OrdinalIgnoreCase))
{
    var notApproved = new List<string>();
    var onboardingLinks = new List<(string Email, string Url)>();

    foreach (var owner in manifest.Owners)
    {
        Console.WriteLine();
        Console.WriteLine($"[{owner.Key}] {owner.Email}");
        var ownerState = state.GetOrAddOwner(owner.Key, owner.Email);
        var token = await client.TryLoginAsync(owner.Email, password)
            ?? throw new InvalidOperationException($"Could not log in {owner.Email}.");
        client.SetBearerToken(token);

        var kyc = (await client.GetMyKycAsync())
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();
        if (kyc?.Status != "Approved")
        {
            notApproved.Add($"{owner.Email} ({kyc?.Status ?? "missing"})");
            Console.WriteLine($"  KYC is {kyc?.Status ?? "missing"}; skipped.");
            continue;
        }

        var profile = await client.GetProfileAsync();
        if (profile.IsOwnerVerified)
        {
            ownerState.KycId = kyc.KycVerificationId;
            ownerState.KycStatus = kyc.Status;
            ownerState.OwnerRoleGranted = true;
            ownerState.StripeAccountCreated = true;
            await state.SaveAsync(statePath, json);
            Console.WriteLine("  Stripe onboarding already complete; skipped.");
            continue;
        }

        ownerState.KycId = kyc.KycVerificationId;
        ownerState.KycStatus = kyc.Status;
        await client.BecomeOwnerAsync(owner.OwnerType);

        // The original JWT predates the role grant. Log in again to receive an Owner claim.
        token = await client.TryLoginAsync(owner.Email, password)
            ?? throw new InvalidOperationException($"Could not refresh the login for {owner.Email}.");
        client.SetBearerToken(token);
        var roles = EstateSyncClient.ReadRoles(token);
        if (!roles.Contains("Owner", StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Owner role was not present after activating {owner.Email}.");

        var link = await client.CreateOwnerOnboardingLinkAsync();
        ownerState.OwnerRoleGranted = true;
        ownerState.StripeAccountCreated = true;
        await state.SaveAsync(statePath, json);
        onboardingLinks.Add((owner.Email, link));
        Console.WriteLine("  Owner role active; Stripe test onboarding link created.");
    }

    Console.WriteLine();
    if (notApproved.Count > 0)
    {
        Console.WriteLine("These accounts were not approved and were skipped:");
        foreach (var item in notApproved) Console.WriteLine($"  - {item}");
    }
    if (onboardingLinks.Count > 0)
    {
        Console.WriteLine("Complete each single-use Stripe test onboarding link now (links are not saved to disk):");
        foreach (var item in onboardingLinks)
        {
            Console.WriteLine($"  {item.Email}");
            Console.WriteLine($"  {item.Url}");
        }
    }
    return notApproved.Count > 0 ? 4 : 0;
}

foreach (var owner in manifest.Owners)
{
    Console.WriteLine();
    Console.WriteLine($"[{owner.Key}] {owner.Email}");

    var ownerState = state.GetOrAddOwner(owner.Key, owner.Email);
    if (!ownerState.RegistrationAttempted)
    {
        var registered = await client.RegisterAsync(owner.FullName, owner.Email, owner.PhoneNumber, password);
        ownerState.RegistrationAttempted = true;
        ownerState.RegisteredBySeeder = registered;
        await state.SaveAsync(statePath, json);
        Console.WriteLine(registered ? "  Registered." : "  Account already exists; continuing safely.");
    }

    var token = await client.TryLoginAsync(owner.Email, password);
    if (token is null)
    {
        waitingForEmailVerification.Add(owner.Email);
        Console.WriteLine("  Login unavailable. Verify this email, then rerun prepare.");
        continue;
    }

    ownerState.LoginVerified = true;
    client.SetBearerToken(token);

    var requests = await client.GetMyKycAsync();
    var active = requests
        .OrderByDescending(item => item.CreatedAt)
        .FirstOrDefault(item => item.Status is "Draft" or "Pending" or "Approved");

    if (active?.Status is "Pending" or "Approved")
    {
        ownerState.KycId = active.KycVerificationId;
        ownerState.KycStatus = active.Status;
        await state.SaveAsync(statePath, json);
        Console.WriteLine($"  KYC already {active.Status}; no uploads performed.");
        continue;
    }

    var kyc = active ?? await client.CreateKycDraftAsync();
    ownerState.KycId = kyc.KycVerificationId;
    ownerState.KycStatus = "Draft";
    await state.SaveAsync(statePath, json);

    var existingTypes = kyc.Documents.Select(document => document.DocumentType)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var documentType in new[] { "AadhaarFront", "AadhaarBack", "PanCard", "AddressProof" })
    {
        if (existingTypes.Contains(documentType))
        {
            Console.WriteLine($"  {documentType}: already uploaded.");
            continue;
        }

        var outputPath = Path.Combine(generatedRoot, "documents", owner.Key, $"TEST-{documentType}.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var pdf = SyntheticPdf.Create(
            TestDocumentNotice,
            $"EstateSync synthetic {documentType}",
            $"Seed owner: {owner.Key}",
            $"Manifest: {manifest.Marker}");
        await File.WriteAllBytesAsync(outputPath, pdf);
        await client.UploadKycDocumentAsync(kyc.KycVerificationId, documentType, outputPath);
        Console.WriteLine($"  {documentType}: uploaded.");
    }

    await client.SubmitKycAsync(kyc.KycVerificationId, owner.AadhaarNumber, owner.PanNumber);
    ownerState.KycStatus = "Pending";
    await state.SaveAsync(statePath, json);
    submittedForApproval.Add(owner.Email);
    Console.WriteLine("  KYC submitted for manual Admin approval.");
}

Console.WriteLine();
Console.WriteLine("Prepare phase finished.");
if (waitingForEmailVerification.Count > 0)
{
    Console.WriteLine("Verify these email addresses and rerun the same command:");
    foreach (var email in waitingForEmailVerification) Console.WriteLine($"  - {email}");
}
if (submittedForApproval.Count > 0)
{
    Console.WriteLine("Approve these KYC requests in the Admin UI before the owners phase:");
    foreach (var email in submittedForApproval) Console.WriteLine($"  - {email}");
}
return waitingForEmailVerification.Count > 0 ? 3 : 0;

static string? GetOption(string[] arguments, string name)
{
    var index = Array.FindIndex(arguments, item => item.Equals(name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}

static bool IsSupportedImage(string path) =>
    new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

static string RequireEnvironment(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Required environment variable {name} is not set.");
    return value;
}

static void PrintPlan(SeedManifest manifest)
{
    var propertyCount = manifest.Owners.Sum(owner => owner.Properties.Count);
    Console.WriteLine($"Manifest: {manifest.Marker}");
    Console.WriteLine($"Owners: {manifest.Owners.Count}");
    Console.WriteLine($"Properties: {propertyCount}");
    Console.WriteLine($"Planned property images: {propertyCount * manifest.ImagesPerProperty}");
}

sealed class EstateSyncClient(HttpClient http, JsonSerializerOptions json)
{
    public void SetBearerToken(string token) =>
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public async Task<bool> RegisterAsync(string fullName, string email, string phoneNumber, string password)
    {
        var response = await http.PostAsJsonAsync("api/v1/auth/register", new
        {
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            Password = password
        }, json);

        if (response.StatusCode == HttpStatusCode.Conflict) return false;
        await EnsureSuccessAsync(response, "register owner");
        return true;
    }

    public async Task<string?> TryLoginAsync(string email, string password)
    {
        using var response = await http.PostAsJsonAsync("api/v1/auth/login", new { email, password }, json);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return null;
        await EnsureSuccessAsync(response, "log in owner");
        var envelope = await ReadAsync<ApiEnvelope<AuthData>>(response);
        return envelope.Data?.AccessToken ?? throw new InvalidOperationException("Login response did not contain an access token.");
    }

    public async Task<List<KycData>> GetMyKycAsync()
    {
        using var response = await http.GetAsync("api/v1/kyc/me");
        await EnsureSuccessAsync(response, "get KYC requests");
        return (await ReadAsync<ApiEnvelope<List<KycData>>>(response)).Data ?? [];
    }

    public async Task<UserProfileData> GetProfileAsync()
    {
        using var response = await http.GetAsync("api/v1/users/me/profile");
        await EnsureSuccessAsync(response, "get user profile");
        return (await ReadAsync<ApiEnvelope<UserProfileData>>(response)).Data
            ?? throw new InvalidOperationException("Profile response contained no data.");
    }

    public async Task<List<PropertyData>> GetMyPropertiesAsync()
    {
        using var response = await http.GetAsync("api/v1/properties/me");
        await EnsureSuccessAsync(response, "get owner properties");
        return (await ReadAsync<ApiEnvelope<List<PropertyData>>>(response)).Data ?? [];
    }

    public async Task<PropertyData> CreatePropertyAsync(SeedProperty property)
    {
        using var response = await http.PostAsJsonAsync("api/v1/properties", new
        {
            property.Title,
            property.Description,
            property.PropertyType,
            property.State,
            property.City,
            property.Pincode,
            property.AddressLine,
            property.Latitude,
            property.Longitude,
            property.LandArea,
            property.BuildupArea,
            property.MonthlyRent,
            property.SecurityDeposit,
            property.Bedrooms,
            property.Bathrooms
        }, json);
        await EnsureSuccessAsync(response, $"create property {property.Key}");
        return (await ReadAsync<ApiEnvelope<PropertyData>>(response)).Data
            ?? throw new InvalidOperationException("Create-property response contained no data.");
    }

    public async Task UpdateAmenitiesAsync(Guid propertyId, List<string> amenities)
    {
        using var response = await http.PutAsJsonAsync($"api/v1/properties/{propertyId}/amenities", new
        {
            amenityNames = amenities
        }, json);
        await EnsureSuccessAsync(response, "update property amenities");
    }

    public async Task UploadPropertyImagesAsync(Guid propertyId, List<string> paths)
    {
        using var form = new MultipartFormDataContent();
        var streams = new List<Stream>();
        var contents = new List<StreamContent>();
        try
        {
            foreach (var path in paths)
            {
                var stream = File.OpenRead(path);
                streams.Add(stream);
                var file = new StreamContent(stream);
                contents.Add(file);
                file.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
                form.Add(file, "files", Path.GetFileName(path));
            }
            using var response = await http.PostAsync($"api/v1/properties/{propertyId}/images", form);
            await EnsureSuccessAsync(response, "upload property images");
        }
        finally
        {
            foreach (var content in contents) content.Dispose();
            foreach (var stream in streams) await stream.DisposeAsync();
        }
    }

    public async Task<List<DocumentData>> GetPropertyDocumentsAsync(Guid propertyId)
    {
        using var response = await http.GetAsync($"api/v1/properties/{propertyId}/documents");
        await EnsureSuccessAsync(response, "get property documents");
        return (await ReadAsync<ApiEnvelope<List<DocumentData>>>(response)).Data ?? [];
    }

    public async Task UploadPropertyDocumentAsync(Guid propertyId, string path)
    {
        await using var stream = File.OpenRead(path);
        using var form = new MultipartFormDataContent();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", Path.GetFileName(path));
        form.Add(new StringContent("OwnershipCertificate"), "documentType");
        using var response = await http.PostAsync($"api/v1/properties/{propertyId}/documents", form);
        await EnsureSuccessAsync(response, "upload property ownership document");
    }

    public async Task SubmitPropertyAsync(Guid propertyId)
    {
        using var response = await http.PutAsync($"api/v1/properties/{propertyId}/submit", null);
        await EnsureSuccessAsync(response, "submit property for approval");
    }

    public async Task<KycData> CreateKycDraftAsync()
    {
        using var response = await http.PostAsync("api/v1/kyc/draft", null);
        await EnsureSuccessAsync(response, "create KYC draft");
        return (await ReadAsync<ApiEnvelope<KycData>>(response)).Data
            ?? throw new InvalidOperationException("KYC draft response contained no data.");
    }

    public async Task UploadKycDocumentAsync(Guid kycId, string documentType, string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        using var form = new MultipartFormDataContent();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", Path.GetFileName(filePath));
        form.Add(new StringContent(documentType), "documentType");
        using var response = await http.PutAsync($"api/v1/kyc/{kycId}/documents", form);
        await EnsureSuccessAsync(response, $"upload {documentType}");
    }

    public async Task SubmitKycAsync(Guid kycId, string aadhaarNumber, string panNumber)
    {
        using var response = await http.PostAsJsonAsync($"api/v1/kyc/{kycId}/submit", new
        {
            aadhaarNumber,
            panNumber
        }, json);
        await EnsureSuccessAsync(response, "submit KYC");
    }

    public async Task BecomeOwnerAsync(string ownerType)
    {
        using var response = await http.PostAsJsonAsync("api/v1/users/me/become-owner", new
        {
            ownerType
        }, json);
        await EnsureSuccessAsync(response, "grant Owner role");
    }

    public async Task<string> CreateOwnerOnboardingLinkAsync()
    {
        using var response = await http.PostAsync("api/v1/payments/owner-onboard", null);
        await EnsureSuccessAsync(response, "create Stripe onboarding link");
        var envelope = await ReadAsync<ApiEnvelope<UrlData>>(response);
        return envelope.Data?.Url
            ?? throw new InvalidOperationException("Stripe onboarding response contained no URL.");
    }

    public static List<string> ReadRoles(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return [];
        var encoded = parts[1].Replace('-', '+').Replace('_', '/');
        encoded = encoded.PadRight(encoded.Length + ((4 - encoded.Length % 4) % 4), '=');
        using var payload = JsonDocument.Parse(Convert.FromBase64String(encoded));
        var roleNames = new[]
        {
            "role",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        foreach (var roleName in roleNames)
        {
            if (!payload.RootElement.TryGetProperty(roleName, out var element)) continue;
            return element.ValueKind == JsonValueKind.Array
                ? element.EnumerateArray().Select(value => value.GetString()).OfType<string>().ToList()
                : element.GetString() is { } role ? [role] : [];
        }
        return [];
    }

    private async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(json)
        ?? throw new InvalidOperationException("API returned an empty response.");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        if (body.Length > 800) body = body[..800];
        throw new InvalidOperationException(
            $"Failed to {operation}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }
}

static class SyntheticPdf
{
    public static byte[] Create(params string[] lines)
    {
        static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var text = new StringBuilder("BT\n/F1 16 Tf\n72 740 Td\n");
        foreach (var line in lines)
        {
            text.Append('(').Append(Escape(line)).Append(") Tj\n0 -28 Td\n");
        }
        text.Append("ET\n");
        var streamText = text.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(streamText)} >>\nstream\n{streamText}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        using var output = new MemoryStream();
        void Write(string value) => output.Write(Encoding.ASCII.GetBytes(value));
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xref = output.Position;
        Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:D10} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }
}

static class ManifestValidator
{
    public static List<string> Validate(SeedManifest manifest)
    {
        var errors = new List<string>();
        if (manifest.Version != 1) errors.Add("Unsupported manifest version.");
        if (string.IsNullOrWhiteSpace(manifest.Marker)) errors.Add("Marker is required.");
        if (manifest.ImagesPerProperty != 6) errors.Add("imagesPerProperty must be 6 for this seed set.");
        if (manifest.Owners.Count == 0) errors.Add("At least one owner is required.");
        if (manifest.Tenants.Count != 3) errors.Add("Exactly three tenants are required for this seed set.");

        var ownerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var propertyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var owner in manifest.Owners)
        {
            if (!ownerKeys.Add(owner.Key)) errors.Add($"Duplicate owner key: {owner.Key}");
            if (!emails.Add(owner.Email)) errors.Add($"Duplicate owner email: {owner.Email}");
            if (!owner.Email.StartsWith("arvindvikramtest+", StringComparison.OrdinalIgnoreCase) ||
                !owner.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                errors.Add($"Owner email is outside the approved alias pattern: {owner.Email}");
            if (owner.PhoneNumber.Length != 10 || !owner.PhoneNumber.All(char.IsDigit))
                errors.Add($"Invalid phone number for {owner.Key}.");
            if (owner.AadhaarNumber.Length != 12 || !owner.AadhaarNumber.All(char.IsDigit))
                errors.Add($"Invalid synthetic Aadhaar format for {owner.Key}.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(owner.PanNumber, "^[A-Z]{5}[0-9]{4}[A-Z]$"))
                errors.Add($"Invalid synthetic PAN format for {owner.Key}.");
            if (owner.Properties.Count == 0) errors.Add($"No properties configured for {owner.Key}.");

            foreach (var property in owner.Properties)
            {
                if (!propertyKeys.Add(property.Key)) errors.Add($"Duplicate property key: {property.Key}");
                if (property.Description.Length is < 20 or > 2000) errors.Add($"Invalid description length: {property.Key}");
                if (property.Title.Length is < 5 or > 100) errors.Add($"Invalid title length: {property.Key}");
                if (property.Pincode.Length != 6 || property.Pincode[0] == '0' || !property.Pincode.All(char.IsDigit))
                    errors.Add($"Invalid pincode: {property.Key}");
                if (property.Bedrooms < 1 || property.Bathrooms < 1 || property.MonthlyRent <= 0 || property.SecurityDeposit < 0)
                    errors.Add($"Invalid numeric property data: {property.Key}");
            }
        }
        foreach (var tenant in manifest.Tenants)
        {
            if (!ownerKeys.Add(tenant.Key)) errors.Add($"Duplicate person key: {tenant.Key}");
            if (!emails.Add(tenant.Email)) errors.Add($"Duplicate email: {tenant.Email}");
            if (!tenant.Email.StartsWith("arvindvikramtest+seed-tenant-", StringComparison.OrdinalIgnoreCase) ||
                !tenant.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                errors.Add($"Tenant email is outside the approved alias pattern: {tenant.Email}");
            if (tenant.PhoneNumber.Length != 10 || !tenant.PhoneNumber.All(char.IsDigit))
                errors.Add($"Invalid phone number for {tenant.Key}.");
            if (tenant.AadhaarNumber.Length != 12 || !tenant.AadhaarNumber.All(char.IsDigit))
                errors.Add($"Invalid synthetic Aadhaar format for {tenant.Key}.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(tenant.PanNumber, "^[A-Z]{5}[0-9]{4}[A-Z]$"))
                errors.Add($"Invalid synthetic PAN format for {tenant.Key}.");
        }
        return errors;
    }
}

sealed class SeederState
{
    public int Version { get; set; } = 1;
    public Dictionary<string, OwnerState> Owners { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, TenantState> Tenants { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public OwnerState GetOrAddOwner(string key, string email)
    {
        if (Owners.TryGetValue(key, out var value))
        {
            if (!value.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"State email mismatch for owner key {key}.");
            return value;
        }
        value = new OwnerState { Email = email };
        Owners.Add(key, value);
        return value;
    }

    public TenantState GetOrAddTenant(string key, string email)
    {
        if (Tenants.TryGetValue(key, out var value))
        {
            if (!value.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"State email mismatch for tenant key {key}.");
            return value;
        }
        value = new TenantState { Email = email };
        Tenants.Add(key, value);
        return value;
    }

    public static async Task<SeederState> LoadAsync(string path, JsonSerializerOptions json) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<SeederState>(await File.ReadAllTextAsync(path), json)
                ?? throw new InvalidOperationException("Seeder state is invalid.")
            : new SeederState();

    public async Task SaveAsync(string path, JsonSerializerOptions json)
    {
        var temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(this, json));
        File.Move(temporaryPath, path, true);
    }
}

sealed class TenantState
{
    public required string Email { get; set; }
    public bool RegistrationAttempted { get; set; }
    public bool RegisteredBySeeder { get; set; }
    public Guid? KycId { get; set; }
    public string? KycStatus { get; set; }
}

sealed class OwnerState
{
    public required string Email { get; set; }
    public bool RegistrationAttempted { get; set; }
    public bool RegisteredBySeeder { get; set; }
    public bool LoginVerified { get; set; }
    public Guid? KycId { get; set; }
    public string? KycStatus { get; set; }
    public bool OwnerRoleGranted { get; set; }
    public bool StripeAccountCreated { get; set; }
    public Dictionary<string, PropertyState> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public PropertyState GetOrAddProperty(string key)
    {
        if (Properties.TryGetValue(key, out var value)) return value;
        value = new PropertyState();
        Properties.Add(key, value);
        return value;
    }
}

sealed class PropertyState
{
    public Guid? PropertyId { get; set; }
    public string? Status { get; set; }
}

sealed record ApiEnvelope<T>(bool Success, T? Data, string? Message);
sealed record AuthData(Guid UserId, string FullName, string AccessToken, string Email, List<string> Roles);
sealed record UrlData(string Url);
sealed record UserProfileData(bool IsKycVerified, bool IsOwnerVerified);
sealed record PropertyData(Guid PropertyId, string Title, string Status, List<string> Amenities, List<PropertyImageData> Images);
sealed record PropertyImageData(Guid ImageId, string Image, bool IsPrimary, int DisplayOrder);
sealed record KycData(Guid KycVerificationId, string Status, DateTime CreatedAt, List<DocumentData> Documents);
sealed record DocumentData(Guid DocumentId, string DocumentType, string OriginalFileName);
sealed record SeedManifest(int Version, string Marker, int ImagesPerProperty, List<SeedTenant> Tenants, List<SeedOwner> Owners);
sealed record SeedTenant(string Key, string FullName, string Email, string PhoneNumber, string AadhaarNumber, string PanNumber);
sealed record SeedOwner(
    string Key,
    string FullName,
    string Email,
    string PhoneNumber,
    string AadhaarNumber,
    string PanNumber,
    string OwnerType,
    List<SeedProperty> Properties);
sealed record SeedProperty(
    string Key,
    string Title,
    string Description,
    string PropertyType,
    string StateCode,
    string State,
    string City,
    string Pincode,
    string AddressLine,
    decimal Latitude,
    decimal Longitude,
    int Bedrooms,
    int Bathrooms,
    decimal LandArea,
    decimal BuildupArea,
    decimal MonthlyRent,
    decimal SecurityDeposit,
    List<string> Amenities,
    string ImageSet);
