using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    [Route("api/[controller]")]
    public class DesignationController(IGenericRepository<Designation> repo) : GenericCrudController<Designation>(repo);
}