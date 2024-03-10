using Microsoft.AspNetCore.Mvc;

namespace ResourceManager.Api.V1;

[Route("api/v1/resources")]
[ApiController]
public class ResourcesController : ControllerBase
{
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    [HttpGet("{id:long}")]
    public string GetById(long id)
    {
        return "value";
    }

    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    [HttpPut("{id:long}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    [HttpDelete("{id:long}")]
    public void Delete(long id)
    {
    }
}
