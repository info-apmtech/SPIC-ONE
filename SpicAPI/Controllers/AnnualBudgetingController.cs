using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    // MD Portal - Annual Budgeting master. Plain generic CRUD, same shape as
    // ZoneController/FinancialYearController: GetAll/GetAllWithInactive/GetById,
    // Create, Update (Patch), ChangeStatus (activate/deactivate) and Delete are
    // all inherited from GenericCrudController<T> - no page-specific business
    // logic is required.
    [Route("api/[controller]")]
    public class AnnualBudgetingController(IGenericRepository<AnnualBudgeting> repo) : GenericCrudController<AnnualBudgeting>(repo);
}
