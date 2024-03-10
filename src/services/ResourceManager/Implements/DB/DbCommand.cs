using Microsoft.EntityFrameworkCore;
using ResourceManager.Business.Contracts;
using ResourceManager.Business.DataModel;
using System.Linq.Expressions;

namespace ResourceManager.Implements.DB
{
    public class DbService : IDatabaseQuery
    {
        private ResourceContext _context;
        public DbService(ResourceContext resourceContext)
        {
            _context = resourceContext;
        }

        public async ValueTask<QueryResponse<List<Resource>>> GetResourcesAsync(Expression<Func<Resource, bool>> filter)
        {
            try
            {
                var data = await _context.Resources
                    .Where(filter)
                    .Take(1000) // security limit
                    .ToListAsync();

                return new() { Value = data };
            }
            catch (Exception ex) 
            {
                return new() { Error = new Error(ex.Message, ErrorCodes.GenericError) };
            }
        }
    }
}
