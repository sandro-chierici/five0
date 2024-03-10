using Microsoft.AspNetCore.Mvc;

namespace ResourceManager.Api.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<ResourcesController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ResourcesController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ResourcesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ResourcesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
