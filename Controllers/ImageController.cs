using ITAM.Services.ImageService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ImageService _imageService;

        public ImageController(ImageService imageService)
        {
            _imageService = imageService;
        }

    
        // Upload Asset Image
        [HttpPost("upload-asset/{assetId}")]
        public async Task<IActionResult> UploadAssetImage(int assetId, IFormFile assetImage)
        {
            if (assetImage == null || assetImage.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                var filePath = await _imageService.UploadAssetImageAsync(assetId, assetImage);
                return Ok(new { message = "Asset image uploaded successfully.", filePath });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error uploading asset image.", error = ex.Message });
            }
        }

        // Upload Computer Image
        [HttpPost("upload-computer/{computerId}")]
        public async Task<IActionResult> UploadComputerImage(int computerId, IFormFile computerImage)
        {
            if (computerImage == null || computerImage.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                var filePath = await _imageService.UploadComputerImageAsync(computerId, computerImage);
                return Ok(new { message = "Computer image uploaded successfully.", filePath });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error uploading computer image.", error = ex.Message });
            }
        }

       
        //Upload Computer Component Image
        [HttpPost("upload-components/{componentId}")]
        public async Task<IActionResult> UploadComponentImage(int componentId, IFormFile componentImage)
        {
            if (componentImage == null || componentImage.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            try
            {
                var filePath = await _imageService.UploadComponentImageAsync(componentId, componentImage);
                return Ok(new { message = "Component image uploaded successfully", filePath });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error uploading component image.", error = ex.Message });
            }
        }

   
        // Get Asset Image by Filename
        [HttpGet("asset/{filename}")]
        public IActionResult GetAssetImage(string filename)
        {
            return GetImageResponse(() => _imageService.GetAssetImageByFilenameAsync(filename));
        }

    
        // Get Computer Image by Filename
        [HttpGet("computer/{filename}")]
        public IActionResult GetComputerImage(string filename)
        {
            return GetImageResponse(() => _imageService.GetComputerImageByFilenameAsync(filename));
        }

  
        //Get Component image by filename
        [HttpGet("component/{filename}")]
        public IActionResult GetComponentImage(string filename)
        {
            return GetImageResponse(() => _imageService.GetComponentImageByFilenameAsync(filename));
        }

      
        // Get E-Signature image by filename
        [HttpGet("esignature/{filename}")]
        public IActionResult GetESignatureImage(string filename)
        {
            return GetImageResponse(() => _imageService.GetESignatureImageByFilenameAsync(filename));
        }

        private IActionResult GetImageResponse(Func<Task<string>> getImagePathFunc)
        {
            try
            {
                var filePath = getImagePathFunc().Result;

                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                string contentType = fileExtension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                var image = System.IO.File.OpenRead(filePath);
                return File(image, contentType);
            }
            catch (FileNotFoundException fnfEx)
            {
                return NotFound(new { message = "Image not found.", error = fnfEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving image.", error = ex.Message });
            }
        }
    }
}
