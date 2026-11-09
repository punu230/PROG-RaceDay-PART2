using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;

namespace RACE_day.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ImagesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var connectionString = _configuration["AzureStorage"];
            var containerName = "event-images";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            var imageUrl = blobClient.Uri.ToString();
            return Ok(new { ImageUrl = imageUrl });
        }
    }
}