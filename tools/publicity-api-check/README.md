# Publicity API check

End-to-end check of `api/Publicity` against a LOCAL API on the local `spicone_dev` database
(never the live API). It creates real rows, so run it only locally.

```
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
dotnet run --project SpicAPI --urls http://localhost:5131
python tools/publicity-api-check/publicity_api_check.py --base http://localhost:5131
```

Needs local QA users `qa.rmd`, `qa.rmd2` (a second HQ in region 1), `qa.rm`, `qa.smd`, `qa.smm`,
`qa.mdadmin` (role 16), `qa.mdhod` (role 17), each with an Employeelogins row, at least one
active product, and `PublicityRequest,PublicityReview` on their designation's RoleAccess.
