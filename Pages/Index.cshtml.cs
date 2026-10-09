using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace WebStorageSample.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public string ImageBase64 { get; private set; }
        public string ImageContentType { get; private set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return Page();
            }

            string endpoint = Environment.GetEnvironmentVariable(Const.ENDPOINT_ENV_KEY);
            string containerName = Const.CONTAINER_NAME;

            // GetCredential() salta la Managed Identity en local, asi no se cuelga 2 minutos buscando IMDS
            var containerClient = new BlobContainerClient(
                new Uri(new Uri(endpoint), containerName),
                StorageHelper.GetCredential());

            await containerClient.CreateIfNotExistsAsync();

            // Path.GetFileName evita rutas raras en el nombre del blob
            string blobName = Path.GetFileName(image.FileName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Subir imagen
            using (var stream = image.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            // Leer nuevamente la imagen desde Blob
            var response = await blobClient.DownloadStreamingAsync();

            using (var memoryStream = new MemoryStream())
            {
                await response.Value.Content.CopyToAsync(memoryStream);
                ImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            ImageContentType = image.ContentType;

            return Page();
        }
    }
}