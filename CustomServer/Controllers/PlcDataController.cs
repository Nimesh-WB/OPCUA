using CustomServer.Model;
using CustomServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

        public class PlcDataController : ControllerBase
        {
            private readonly OpcUaServerService _opcUaServerService;

            public PlcDataController(OpcUaServerService opcUaServerService)
            {
                _opcUaServerService = opcUaServerService;
            }

            [HttpPost]
            public IActionResult Post([FromBody] PlcDataModel data)
            {
                return Ok();
            }
        }
    }
