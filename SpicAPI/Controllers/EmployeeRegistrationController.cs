using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Route("api/[controller]")]
    public class EmployeeRegistrationController(IGenericRepository<EmployeeInformation> repo) : GenericCrudController<EmployeeInformation>(repo);
    [Route("api/[controller]")]
    public class EmployeeloginController(IGenericRepository<Employeelogin> repo) : GenericCrudController<Employeelogin>(repo);

}
