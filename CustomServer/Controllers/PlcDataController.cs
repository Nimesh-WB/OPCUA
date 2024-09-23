using CustomServer.Model;
using CustomServer.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CustomServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlcDataController : ControllerBase
    {
        private readonly OpcUaServerService _opcUaServerService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TestService _testService;

        public PlcDataController(OpcUaServerService opcUaServerService, IHttpClientFactory httpClientFactory,TestService testService)
        {
            _opcUaServerService = opcUaServerService;
            _httpClientFactory = httpClientFactory;
            _testService = testService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PlcDataModel data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Tag) || data.Value == null)
            {
                return BadRequest("Invalid PLC Data provided.");
            }

            try
            {
                    await _opcUaServerService.StartServer();
                //await _testService.StartServer();
                var response = await SendMqttMessage(data.Tag, data.Value);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"Failed to publish to MQTT: {response.ReasonPhrase}");
                }
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error starting OPC UA server: {ex.Message}");
            }
        }

        private async Task<HttpResponseMessage> SendMqttMessage(string topic, string message)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"http://localhost:5079/api/mqtt/publish";

            var postData = new Dictionary<string, string>
            {
                { "tag", topic },
                { "value", message }
            };

            var json = JsonConvert.SerializeObject(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            Console.WriteLine(content);
            return await client.PostAsync(url, content);
        }
    }
}
