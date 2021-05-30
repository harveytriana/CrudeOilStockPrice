// ==================================
// BlazorSpread.net
// ===================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace CrudeOilStockPrice.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploaderController : ControllerBase
    {
        // ...in production this utility must be under authorization
        [HttpPost]
        public async Task<bool> Post(IFormFile file)
        {
            try {
                var filePath = Startup.DATA_PATH + file.FileName;
                using (var stream = new FileStream(filePath, FileMode.Create)) {
                    await file.CopyToAsync(stream);
                }
                return true;
            }
            catch { }
            return false;
        }
    }
}
