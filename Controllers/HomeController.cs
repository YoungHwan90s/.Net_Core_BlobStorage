using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using _NetCoreBlob.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace _NetCoreBlob.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly BlobServiceClient _blobService;

    public HomeController(ILogger<HomeController> logger, BlobServiceClient blobService)
    {
        _logger = logger;
        _blobService = blobService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Image()
    {
        return View();
    }

    [HttpPost]
    public async Task<string> Upload(IFormFile file)
    {
        // 반환 URL
        string blobUrl = "";

        // 저장 될 컨테이너 저장소 이름
        string containerName  = "web";

        // 고유 이름 생성
        //var blobName = $"{Guid.NewGuid().ToString()}.jpg";

        // 로컬 파일 이름으로 지정
        string blobName = file.FileName;

        try
        {
            var containerClient = _blobService.GetBlobContainerClient(containerName);

            // 1. 컨테이너 없을 경우 생성 - PublicAccessType.Blob(Blob에 대한 읽기 전용 액세스 허용)
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // 2. 이미지 Blob에 업로드하기 전에 같은 이름의 파일이 있는지 확인하고 삭제
            var blobClient = containerClient.GetBlobClient(blobName);
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteIfExistsAsync();
            }

            // 3.이미지 처리 및 리사이징
            using (var image = SixLabors.ImageSharp.Image.Load(file.OpenReadStream()))
            {
                // 4. 이미지 파일의 크기를 확인
                var fileSize = file.Length;
                // 4.1 목표 파일 크기 (2MB)
                int targetFileSize = 2 * 1024 * 1024;
                // 4.2 초기 압축 품질 설정
                int compressionQuality = 100;

                // 5. 2MB 보다 크면 리사이징 작업
                if (fileSize > targetFileSize)
                {
                    // 5.1 이미지 리사이징
                    //image.Mutate(x => x.Resize(500, 500)); // 너비 500px, 높이 500px로 리사이징

                    while (fileSize > targetFileSize)
                    {
                        // 5.2 품질 단계 5씩 감소
                        compressionQuality -= 5;

                        // 5.3 메모리 스트림에 이미지 저장
                        using (var compressedStream = new MemoryStream())
                        {
                            image.Save(compressedStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
                            {
                                Quality = compressionQuality
                            });

                            compressedStream.Position = 0;

                            // 압축된 이미지 파일 크기 확인
                            fileSize = compressedStream.Length;

                            // 파일 크기가 목표 크기보다 작은 경우에만 업로드
                            if (fileSize <= targetFileSize)
                            {
                                await containerClient.UploadBlobAsync(blobName, compressedStream);
                            }
                        }
                    }
                }
                else
                {
                    await containerClient.UploadBlobAsync(blobName, file.OpenReadStream());
                }
            }

            // 반환 이미지 URL
            blobUrl = blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        return blobUrl;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
