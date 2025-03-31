using ITAM.DataContext;
using ITAM.Models;

namespace ITAM.Services.ImageService
{
    public class ImageService
    {
        private readonly AppDbContext _context;
        private readonly string _eSignatureDirectory = @"C:\ITAM\e-signature\signature";


        public ImageService(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to handle image upload logic
        private async Task<string> UploadImageAsync(int entityId, IFormFile imageFile, string baseDirectory, string entityType)
        {
            try
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Invalid file type. Only images are allowed.");
                }

                object entity;
                if (entityType == "asset")
                {
                    entity = await _context.Assets.FindAsync(entityId);
                    if (entity == null) throw new KeyNotFoundException("Asset not found.");
                }
                else if (entityType == "computer")
                {
                    entity = await _context.computers.FindAsync(entityId);
                    if (entity == null) throw new KeyNotFoundException("Computer not found.");
                }
                else if (entityType == "component")
                {
                    entity = await _context.computer_components.FindAsync(entityId);
                    if (entity == null) throw new KeyNotFoundException("Computer Component not found.");
                }
                else
                {
                    throw new ArgumentException("Invalid entity type.");
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);

                string directoryPath = Path.Combine(baseDirectory, entityId.ToString());
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string filePath = Path.Combine(directoryPath, uniqueFileName).Replace("\\", "/");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                if (entityType == "asset")
                {
                    var asset = (Asset)entity;
                    asset.asset_image = filePath;
                    _context.Assets.Update(asset);
                }
                else if (entityType == "computer")
                {
                    var computer = (Computer)entity;
                    computer.asset_image = filePath;
                    _context.computers.Update(computer);
                }
                else if (entityType == "component")
                {
                    var component = (ComputerComponents)entity;
                    component.component_image = filePath;
                    _context.computer_components.Update(component);
                }

                await _context.SaveChangesAsync();
                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading {entityType} image: {ex.Message}");
            }
        }

        // Upload asset image
        public async Task<string> UploadAssetImageAsync(int assetId, IFormFile assetImage)
        {
            return await UploadImageAsync(assetId, assetImage, @"C:\ITAM\assets\asset-images", "asset");
        }

        // Upload computer image
        public async Task<string> UploadComputerImageAsync(int computerId, IFormFile computerImage)
        {
            return await UploadImageAsync(computerId, computerImage, @"C:\ITAM\assets\computer-images", "computer");
        }

        //Upload component image
        public async Task<string> UploadComponentImageAsync(int componentId, IFormFile componentImage)
        {
            return await UploadImageAsync(componentId, componentImage, @"C:\ITAM\assets\components-images ", "component");
        }

        // Get asset image by filename
        public async Task<string> GetAssetImageByFilenameAsync(string filename)
        {
            try
            {
                string baseDirectory = @"C:\ITAM\assets\asset-images";
                var directories = Directory.GetDirectories(baseDirectory);
                string filePath = null;

                foreach (var directory in directories)
                {
                    var potentialPath = Path.Combine(directory, filename);
                    if (File.Exists(potentialPath))
                    {
                        filePath = potentialPath;
                        break;
                    }
                }

                if (filePath == null)
                {
                    Console.WriteLine($"File not found. Searched in all subdirectories of: {baseDirectory}");
                    Console.WriteLine($"Filename searched for: {filename}");
                    Console.WriteLine($"Available directories: {string.Join(", ", directories)}");

                    throw new FileNotFoundException($"Asset image '{filename}' not found in any asset directory");
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error details: {ex}");
                throw new Exception($"Error fetching asset image: {ex.Message}", ex);
            }
        }

        // Get computer image by filename
        public async Task<string> GetComputerImageByFilenameAsync(string filename)
        {
            try
            {
                string baseDirectory = @"C:\ITAM\assets\computer-images";

                var directories = Directory.GetDirectories(baseDirectory);
                string filePath = null;

                var rootPath = Path.Combine(baseDirectory, filename);
                if (File.Exists(rootPath))
                {
                    filePath = rootPath;
                }
                else
                {
                    foreach (var directory in directories)
                    {
                        var potentialPath = Path.Combine(directory, filename);
                        if (File.Exists(potentialPath))
                        {
                            filePath = potentialPath;
                            break;
                        }
                    }
                }

                if (filePath == null)
                {
                    Console.WriteLine($"File not found. Searched in base directory and all subdirectories of: {baseDirectory}");
                    Console.WriteLine($"Filename searched for: {filename}");
                    Console.WriteLine($"Available directories: {string.Join(", ", directories)}");

                    throw new FileNotFoundException($"Computer image '{filename}' not found in any directory");
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error details: {ex}");
                throw new Exception($"Error fetching computer image: {ex.Message}", ex);
            }
        }

        //get componone image by filename
        public async Task<string> GetComponentImageByFilenameAsync(string filename)
        {
            try
            {
                string baseDirectory = @"C:\ITAM\assets\components-images".TrimEnd(); // Removed trailing space

                // Create directory if it doesn't exist
                if (!Directory.Exists(baseDirectory))
                {
                    Directory.CreateDirectory(baseDirectory);
                }

                var directories = Directory.GetDirectories(baseDirectory);
                string filePath = null;
                var rootPath = Path.Combine(baseDirectory, filename);

                if (File.Exists(rootPath))
                {
                    filePath = rootPath;
                }
                else
                {
                    foreach (var directory in directories)
                    {
                        var potentialPath = Path.Combine(directory, filename);
                        if (File.Exists(potentialPath))
                        {
                            filePath = potentialPath;
                            break;
                        }
                    }
                }

                if (filePath == null)
                {
                    Console.WriteLine($"File not found. Searched in base directory and all subdirectories of: {baseDirectory}");
                    Console.WriteLine($"Filename searched for: {filename}");
                    Console.WriteLine($"Available directories: {string.Join(", ", directories)}");
                    throw new FileNotFoundException($"Component image '{filename}' not found in any directory"); // Changed to Component
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error details: {ex}");
                throw new Exception($"Error fetching component image: {ex.Message}", ex); // Changed to component
            }
        }

        // Get e-signature image by filename
        public async Task<string> GetESignatureImageByFilenameAsync(string filename)
        {
            string filePath = Path.Combine(_eSignatureDirectory, filename);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                throw new FileNotFoundException($"E-Signature image '{filename}' not found in {_eSignatureDirectory}.");
            }

            return await Task.FromResult(filePath);
        }

    }
}
