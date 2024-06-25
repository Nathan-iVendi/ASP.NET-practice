using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace CityInfo.API.Controllers
{
    [Route("api/files")]
    [Authorize]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider; // Provider to determine MIME (Multipurpose Internet Mail Extentions) types based on file extensions.

        // Constructor for the FilesController, injecting the FileExtensionContentTypeProvider dependency.
        public FilesController(FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
        {
            _fileExtensionContentTypeProvider = fileExtensionContentTypeProvider
                ?? throw new System.ArgumentNullException(nameof(fileExtensionContentTypeProvider));
        }

        // Retrieves a file based on the provided file ID.
        [HttpGet("{fileID}")]
        public ActionResult GetFile(string fileID)
        {
            // In a real-world application, logic would be needed to determine the actual file path.
            var pathToFile = "getting-started-with-rest-slides.pdf";

            // Checks if the file exists at the specified path.
            if (!System.IO.File.Exists(pathToFile))
            {
                return NotFound();
            }

            // Attempts to determine the content type based on the file extension.
            // If the content type cannot be determined, defaults to "application/octet-stream".
            if (!_fileExtensionContentTypeProvider.TryGetContentType(pathToFile, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            // Reads the file into a byte array.
            var bytes = System.IO.File.ReadAllBytes(pathToFile);

            // Returns the file as a binary download, including the MIME type and file name.
            return File(bytes, contentType, Path.GetFileName(pathToFile));
        }

        // Handles file uploads, specifically for PDF files.
        [HttpPost]
        public async Task<ActionResult> CreateFile(IFormFile file)
        {
            // Validates the input file. It must be a PDF, non-empty, and not larger than 20 MB. Lowers the risk of a DDOs attack.
            if (file.Length == 0 || file.Length > 20971520 || file.ContentType != "application/pdf")
            {
                // Returns a 400 Bad Request response if the file does not meet the criteria.
                return BadRequest("No file or an invalid one has been inputted.");
            }

            // Creates a unique file path for the uploaded file.
            // Uses a GUID to generate a unique file name and avoids using the original file name for security reasons.
            var path = Path.Combine(Directory.GetCurrentDirectory(),
                $"uploaded_file_{Guid.NewGuid()}.pdf");

            // Writes the uploaded file to the specified path.
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream); // Asynchronously copies the file content to the stream.
            }

            // Returns a 200 OK response with a success message after the file is uploaded.
            return Ok("Your file has been successfully uploaded.");
        }
    }
}
