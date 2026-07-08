using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Exceptions;

namespace EstateSync.Api.Common;

public static class FileValidation
{
    /// <summary>Throws BadRequestException unless the upload is a non-empty PDF.</summary>
    public static void EnsurePdf(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw new BadRequestException("No file was uploaded.");

        var isPdf = file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new BadRequestException("Only PDF documents are allowed.");
    }
}
