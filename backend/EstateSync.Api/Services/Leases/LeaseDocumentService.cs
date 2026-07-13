using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EstateSync.Api.Interfaces.Services.Leases;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Models;

namespace EstateSync.Api.Services.Leases;

public class LeaseDocumentService : ILeaseDocumentService
{
    private readonly IFileStorageService _fileStorage;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LeaseDocumentService> _logger;

    // ── Design tokens ────────────────────────────────────────────────────────
    private const string Navy     = "#1B2A4A";
    private const string NavyLight = "#2C3E6B";
    private const string SectionBg = "#EEF2F9";
    private const string RowAlt    = "#F7F9FC";
    private const string Border    = "#C4D0E6";
    private const string TextMuted = "#6B7280";

    private static readonly string[] DefaultClauses =
    [
        "The Tenant shall use the premises solely for residential purposes and shall not use or permit the use of the premises for any commercial, illegal, or immoral activity.",
        "The Tenant shall keep the premises clean, sanitary, and in good condition and, upon termination of this Lease, shall return the premises in the same condition as received, ordinary wear and tear excepted.",
        "The Tenant shall promptly notify the Landlord of any damage, defect, or dangerous condition in or about the premises and shall not attempt repairs without prior written consent of the Landlord.",
        "The Tenant shall not make any alterations, additions, or improvements to the premises without the prior written consent of the Landlord. Any approved alterations shall become the property of the Landlord unless otherwise agreed in writing.",
        "The Tenant shall comply with all applicable laws, regulations, and ordinances regarding the use and occupancy of the premises and shall not disturb the quiet enjoyment of neighbouring tenants or residents."
    ];

    public LeaseDocumentService(
        IFileStorageService fileStorage,
        IWebHostEnvironment env,
        ILogger<LeaseDocumentService> logger)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _env         = env         ?? throw new ArgumentNullException(nameof(env));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<string> GenerateAgreementPdfAsync(Lease lease, CancellationToken ct = default)
    {
        var pdfBytes = QuestPDF.Fluent.Document.Create(doc =>
        {
            doc.Page(page =>
            {
                ApplyPageDefaults(page);
                page.Header().Element(c => BuildPageHeader(c, lease, signed: false));
                page.Content().Element(c => BuildBody(c, lease, tenantSig: null, ownerSig: null, signed: false));
                page.Footer().Element(c => BuildPageFooter(c, "DRAFT — Awaiting Signatures", signed: false));
            });
        }).GeneratePdf();

        var url = await _fileStorage.SaveBytesAsync(pdfBytes, $"leases/{lease.LeaseId}", "agreement.pdf", ct);
        _logger.LogInformation("Agreement PDF generated for lease {LeaseId}", lease.LeaseId);
        return url;
    }

    public async Task<string> GenerateSignedAgreementPdfAsync(Lease lease, CancellationToken ct = default)
    {
        var tenantSig = LoadSignatureImage(lease.TenantSignatureImageUrl);
        var ownerSig  = LoadSignatureImage(lease.OwnerSignatureImageUrl);

        var pdfBytes = QuestPDF.Fluent.Document.Create(doc =>
        {
            doc.Page(page =>
            {
                ApplyPageDefaults(page);
                page.Header().Element(c => BuildPageHeader(c, lease, signed: true));
                page.Content().Element(c => BuildBody(c, lease, tenantSig, ownerSig, signed: true));
                page.Footer().Element(c => BuildPageFooter(c, null, signed: true));
            });
        }).GeneratePdf();

        var url = await _fileStorage.SaveBytesAsync(pdfBytes, $"leases/{lease.LeaseId}", "signed-agreement.pdf", ct);
        _logger.LogInformation("Signed agreement PDF generated for lease {LeaseId}", lease.LeaseId);
        return url;
    }

    // ── Page setup ────────────────────────────────────────────────────────────

    private static void ApplyPageDefaults(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(45);
        page.MarginVertical(35);
        page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Helvetica").FontColor("#1F2937"));
    }

    // ── Page header ───────────────────────────────────────────────────────────

    private static void BuildPageHeader(IContainer container, Lease lease, bool signed)
    {
        container.Column(col =>
        {
            // Navy banner
            col.Item()
                .Background(Navy)
                .Padding(16)
                .Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item()
                            .Text(signed ? "RESIDENTIAL LEASE AGREEMENT" : "RESIDENTIAL LEASE AGREEMENT")
                            .FontSize(16).Bold().FontColor(Colors.White);
                        left.Item().PaddingTop(2)
                            .Text(signed ? "EXECUTED COPY" : "DRAFT — PENDING SIGNATURES")
                            .FontSize(8).FontColor("#A8BCD8");
                    });

                    row.RelativeItem().AlignRight().Column(right =>
                    {
                        right.Item()
                            .Text($"Ref: {lease.LeaseId.ToString()[..8].ToUpperInvariant()}")
                            .FontSize(9).FontColor("#A8BCD8");
                        right.Item().PaddingTop(2)
                            .Text($"Dated: {lease.StartDate:dd MMMM yyyy}")
                            .FontSize(9).FontColor("#A8BCD8");
                    });
                });

            col.Item().Height(4).Background(NavyLight);
        });
    }

    // ── Body ─────────────────────────────────────────────────────────────────

    private static void BuildBody(IContainer container, Lease lease,
        byte[]? tenantSig, byte[]? ownerSig, bool signed)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().PaddingTop(10).Element(c => BuildIntroText(c, lease));
            col.Item().Element(c => BuildPartiesSection(c, lease));
            col.Item().Element(c => BuildPropertySection(c, lease));
            col.Item().Element(c => BuildLeaseTermsSection(c, lease));
            col.Item().Element(c => BuildTermsAndConditions(c, lease));
            col.Item().Element(c => BuildSignatureSection(c, lease, tenantSig, ownerSig, signed));
        });
    }

    // ── Section 0 — Intro sentence ────────────────────────────────────────────

    private static void BuildIntroText(IContainer container, Lease lease)
    {
        var owner  = lease.Property?.Owner?.FullName  ?? "the Landlord";
        var tenant = lease.Tenant?.FullName ?? "the Tenant";

        container
            .Background(SectionBg)
            .Border(1).BorderColor(Border)
            .Padding(10)
            .Text(text =>
            {
                text.Span("This Residential Lease Agreement (\"Agreement\") is entered into as of ")
                    .FontSize(10);
                text.Span($"{lease.StartDate:dd MMMM yyyy}").Bold();
                text.Span(" between ");
                text.Span(owner).Bold();
                text.Span(" (hereinafter \"Landlord\") and ");
                text.Span(tenant).Bold();
                text.Span(" (hereinafter \"Tenant\"), collectively referred to as the \"Parties\".");
            });
    }

    // ── Section 1 — Parties ───────────────────────────────────────────────────

    private static void BuildPartiesSection(IContainer container, Lease lease)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "1.  PARTIES TO THIS AGREEMENT"));
            col.Item()
                .Border(1).BorderColor(Border)
                .Row(row =>
                {
                    // Landlord
                    row.RelativeItem()
                        .BorderRight(1).BorderColor(Border)
                        .Padding(10)
                        .Column(c =>
                        {
                            c.Item().Text("LANDLORD (Owner)").Bold().FontSize(9).FontColor(Navy);
                            c.Item().PaddingTop(6).Element(x =>
                                PartyField(x, "Name",  lease.Property?.Owner?.FullName  ?? "—"));
                            c.Item().Element(x =>
                                PartyField(x, "Email", lease.Property?.Owner?.Email     ?? "—"));
                            c.Item().Element(x =>
                                PartyField(x, "Phone", lease.Property?.Owner?.PhoneNumber ?? "—"));
                        });

                    // Tenant
                    row.RelativeItem()
                        .Padding(10)
                        .Column(c =>
                        {
                            c.Item().Text("TENANT").Bold().FontSize(9).FontColor(Navy);
                            c.Item().PaddingTop(6).Element(x =>
                                PartyField(x, "Name",  lease.Tenant?.FullName     ?? "—"));
                            c.Item().Element(x =>
                                PartyField(x, "Email", lease.Tenant?.Email        ?? "—"));
                            c.Item().Element(x =>
                                PartyField(x, "Phone", lease.Tenant?.PhoneNumber  ?? "—"));
                        });
                });
        });
    }

    private static void PartyField(IContainer c, string label, string value)
    {
        c.PaddingVertical(2).Row(row =>
        {
            row.ConstantItem(44).Text(label + ":").FontSize(9).FontColor(TextMuted);
            row.RelativeItem().Text(value).FontSize(10);
        });
    }

    // ── Section 2 — Property ─────────────────────────────────────────────────

    private static void BuildPropertySection(IContainer container, Lease lease)
    {
        var prop = lease.Property;
        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "2.  PROPERTY DESCRIPTION"));
            col.Item()
                .Border(1).BorderColor(Border)
                .Background(RowAlt)
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text(prop?.Title ?? "—").Bold().FontSize(11);
                    c.Item().PaddingTop(4).Text(prop?.AddressLine ?? "—").FontColor(TextMuted);
                    c.Item().PaddingTop(6).Row(row =>
                    {
                        PropertyChip(row, "Type",      prop?.PropertyType.ToString() ?? "—");
                        PropertyChip(row, "Bedrooms",  prop?.Bedrooms.ToString()     ?? "—");
                        PropertyChip(row, "Bathrooms", prop?.Bathrooms.ToString()    ?? "—");
                    });
                });
        });
    }

    private static void PropertyChip(RowDescriptor row, string label, string value)
    {
        row.AutoItem()
            .PaddingRight(12)
            .Row(r =>
            {
                r.AutoItem().Text(label + ": ").FontSize(9).FontColor(TextMuted);
                r.AutoItem().Text(value).FontSize(9).Bold();
            });
    }

    // ── Section 3 — Lease Terms ───────────────────────────────────────────────

    private static void BuildLeaseTermsSection(IContainer container, Lease lease)
    {
        var totalMonths = (int)Math.Round((lease.EndDate - lease.StartDate).TotalDays / 30.44);

        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "3.  LEASE TERMS & FINANCIAL DETAILS"));
            col.Item().Border(1).BorderColor(Border).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(3);
                });

                TermsRow(table, "Lease Period",
                    $"{lease.StartDate:dd MMMM yyyy} – {lease.EndDate:dd MMMM yyyy}  ({totalMonths} months)",
                    shaded: true);
                TermsRow(table, "Monthly Rent",
                    $"₹ {lease.MonthlyRent:N0}",
                    shaded: false);
                TermsRow(table, "Security Deposit",
                    $"₹ {lease.SecurityDeposit:N0}  (refundable on vacating)",
                    shaded: true);
                TermsRow(table, "Payment Frequency",
                    lease.PaymentFrequency.ToString(),
                    shaded: false);
                TermsRow(table, "Payment Due Date",
                    "1st of each billing period",
                    shaded: true);

                if (lease.NoticePeriodDays.HasValue)
                    TermsRow(table, "Notice Period",
                        $"{lease.NoticePeriodDays} days written notice required",
                        shaded: false);

                if (lease.PenaltyPercentage.HasValue)
                    TermsRow(table, "Late Payment Penalty",
                        $"{lease.PenaltyPercentage}% of outstanding amount per month",
                        shaded: lease.NoticePeriodDays.HasValue ? true : false);
            });
        });
    }

    private static void TermsRow(TableDescriptor table, string term, string value, bool shaded)
    {
        string bg = shaded ? RowAlt : Colors.White;

        table.Cell()
            .Background(bg)
            .BorderBottom(1).BorderColor(Border)
            .Padding(8)
            .Text(term).Bold().FontSize(9).FontColor(Navy);

        table.Cell()
            .Background(bg)
            .BorderBottom(1).BorderColor(Border)
            .Padding(8)
            .Text(value).FontSize(10);
    }

    // ── Section 4 — Terms & Conditions ───────────────────────────────────────

    private static void BuildTermsAndConditions(IContainer container, Lease lease)
    {
        var rawText = lease.AgreementText;
        string[] clauses;

        if (!string.IsNullOrWhiteSpace(rawText))
        {
            clauses = rawText
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().TrimEnd('.'))
                .Where(l => l.Length > 0)
                .ToArray();

            if (clauses.Length == 1 && clauses[0].Contains(". "))
                clauses = clauses[0].Split(". ", StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            clauses = DefaultClauses;
        }

        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "4.  TERMS & CONDITIONS"));
            col.Item().Border(1).BorderColor(Border).Padding(12).Column(c =>
            {
                c.Spacing(8);
                for (int i = 0; i < clauses.Length; i++)
                {
                    var idx = i;
                    c.Item().Row(row =>
                    {
                        row.ConstantItem(22)
                            .Text($"{idx + 1}.")
                            .Bold().FontSize(10).FontColor(Navy);
                        row.RelativeItem()
                            .Text(clauses[idx].TrimEnd('.') + ".")
                            .FontSize(10);
                    });
                }
            });
        });
    }

    // ── Section 5 — Signatures ────────────────────────────────────────────────

    private static void BuildSignatureSection(IContainer container, Lease lease,
        byte[]? tenantSig, byte[]? ownerSig, bool signed)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "5.  SIGNATURES"));

            col.Item().PaddingVertical(6)
                .Text("IN WITNESS WHEREOF, the Parties have read, understood, and agreed to all terms of this Lease Agreement and have affixed their signatures on the dates indicated below.")
                .FontSize(9).FontColor(TextMuted).Italic();

            col.Item().Row(row =>
            {
                // Tenant signature box
                row.RelativeItem().Border(1).BorderColor(Border).Padding(12).Column(c =>
                {
                    c.Item().Text("TENANT").Bold().FontSize(9).FontColor(Navy);
                    c.Item().PaddingTop(8).Element(x => SignatureImageOrPlaceholder(x, tenantSig, signed));
                    c.Item().PaddingTop(6).LineHorizontal(1).LineColor(Border);
                    c.Item().PaddingTop(4).Text(lease.TenantSignature ?? "").Bold().FontSize(11);
                    c.Item().PaddingTop(4).Element(x => SignatureMeta(x,
                        label: "Signed",
                        value: lease.TenantSignedAt.HasValue
                            ? $"{lease.TenantSignedAt:dd MMM yyyy, HH:mm} UTC"
                            : "—"));
                    c.Item().Element(x => SignatureMeta(x, "IP Address", lease.TenantSignatureIp ?? "—"));
                });

                row.ConstantItem(16);

                // Owner signature box
                row.RelativeItem().Border(1).BorderColor(Border).Padding(12).Column(c =>
                {
                    c.Item().Text("LANDLORD (OWNER)").Bold().FontSize(9).FontColor(Navy);
                    c.Item().PaddingTop(8).Element(x => SignatureImageOrPlaceholder(x, ownerSig, signed));
                    c.Item().PaddingTop(6).LineHorizontal(1).LineColor(Border);
                    c.Item().PaddingTop(4).Text(lease.OwnerSignature ?? "").Bold().FontSize(11);
                    c.Item().PaddingTop(4).Element(x => SignatureMeta(x,
                        label: "Signed",
                        value: lease.OwnerSignedAt.HasValue
                            ? $"{lease.OwnerSignedAt:dd MMM yyyy, HH:mm} UTC"
                            : "—"));
                    c.Item().Element(x => SignatureMeta(x, "IP Address", lease.OwnerSignatureIp ?? "—"));
                });
            });
        });
    }

    private static void SignatureImageOrPlaceholder(IContainer container, byte[]? sigBytes, bool signed)
    {
        if (sigBytes != null)
        {
            container.Height(80).Image(sigBytes).FitArea();
        }
        else
        {
            container
                .Height(80)
                .Background(SectionBg)
                .Border(1).BorderColor(Border)
                .AlignCenter().AlignMiddle()
                .Text(signed ? "Signature Not Available" : "Awaiting Signature")
                .FontSize(9).FontColor(TextMuted).Italic();
        }
    }

    private static void SignatureMeta(IContainer container, string label, string value)
    {
        container.PaddingTop(2).Row(row =>
        {
            row.ConstantItem(60).Text(label + ":").FontSize(8).FontColor(TextMuted);
            row.RelativeItem().Text(value).FontSize(8);
        });
    }

    // ── Page footer ───────────────────────────────────────────────────────────

    private static void BuildPageFooter(IContainer container, string? draftText, bool signed)
    {
        container.Column(col =>
        {
            col.Item().Height(1).Background(Border);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .Text(signed
                        ? "Digitally signed via EstateSync"
                        : draftText ?? string.Empty)
                    .FontSize(7).FontColor(TextMuted);

                row.AutoItem()
                    .Text(text =>
                    {
                        text.Span("Page ").FontSize(7).FontColor(TextMuted);
                        text.CurrentPageNumber().FontSize(7).FontColor(TextMuted);
                        text.Span(" of ").FontSize(7).FontColor(TextMuted);
                        text.TotalPages().FontSize(7).FontColor(TextMuted);
                    });
            });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SectionHeader(IContainer container, string title)
    {
        container
            .Background(SectionBg)
            .BorderLeft(3).BorderColor(Navy)
            .PaddingVertical(6).PaddingHorizontal(10)
            .Text(title).Bold().FontSize(10).FontColor(Navy);
    }

    private byte[]? LoadSignatureImage(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;
        try
        {
            // imageUrl is like "/uploads/leases/{id}/tenant-sig.png"
            // Resolve to physical path using content root
            var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physical = Path.Combine(_env.ContentRootPath, relative);
            if (File.Exists(physical))
                return File.ReadAllBytes(physical);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load signature image from {Url}", imageUrl);
        }
        return null;
    }
}
