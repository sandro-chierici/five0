using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ResourceManager.Business.Contracts;

namespace ResourceManager.Api.V1;

[Route("api/v1/[Controller]")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery, IDatabaseCommand dbCommand) : ControllerBase
{
    [Route("actions/createdb")]
    [HttpGet]
    public async Task<ActionResult> EnsureDbCreated()
    {
        await dbCommand.EnsureDBCreated();

        return Ok();
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> GetAll()
    {
        var resp = await dbQuery.GetResourcesAsync(_ => true);
        return Ok(new JsonResult(resp.Value?.ToList()));
    }

    [HttpGet("{id:long}")]
    public async Task<string> GetById(long id)
    {
        var qr = await dbQuery.GetResourcesAsync(res => res.Id == id);

        return (qr.Value?.FirstOrDefault()?.Name ?? "") ;
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
