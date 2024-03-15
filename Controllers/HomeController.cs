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

        var containerClient = _blobService.GetBlobContainerClient(containerName);

        // 컨테이너가 존재하지 않는 경우에 컨테이너 생성 시도
        if (!await containerClient.ExistsAsync())
        {
            // 컨테이너 생성
            // PublicAccessType.Blob - Blob에 대한 읽기 전용 액세스 허용
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        }

        // 고유 이름 생성
        //var blobName = $"{Guid.NewGuid().ToString()}.jpg";

        // 로컬 파일 이름으로 지정
        string blobName = file.FileName;

        try
        {
            // 이미지 Blob에 업로드하기 전에 같은 이름의 파일이 있는지 확인하고 삭제
            var blobClient = containerClient.GetBlobClient(blobName);
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteIfExistsAsync();
            }
            
            // 이미지 Blob에 업로드
            // 아래 작업이 실패하면 error를 던지기 때문에 catch문으로 이동 됨
            await containerClient.UploadBlobAsync(blobName, file.OpenReadStream());

            // 반환 이미지 URL
            blobUrl = blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        // // 업로드 성공 여부에 따라 적절한 응답 반환
        // if (uploadSuccessful)
        // {
        //     return Ok("이미지 업로드 성공");
        // }
        // else
        // {
        //     return BadRequest("이미지 업로드 실패");
        // }

        return blobUrl;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
