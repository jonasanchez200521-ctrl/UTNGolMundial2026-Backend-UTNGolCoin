# UTNGolMundial2026-Backend (UTNGolCoin)

Backend de moneda virtual y apuestas para el proyecto integrador **UTN GolMundial 2026**. Gestiona billeteras, apuestas 1X2 sobre partidos, liquidación de premios, bono de bienvenida, bono antibancarrota, ranking de usuarios y reportes administrativos.

## Stack

- ASP.NET Core Web API, .NET 9 (controladores).
- MariaDB (127.0.0.1:3307) vía EF Core + Pomelo.EntityFrameworkCore.MySql.
- Swagger / Swashbuckle como documentación interactiva (página de inicio del servicio).

## Cómo correr el proyecto

```
cd UTNGolCoin.Api/UTNGolCoin.Api
dotnet restore
dotnet ef database update   # crea la base y las tablas en MariaDB
dotnet run
```

Al abrir la URL del servicio (por defecto `http://localhost:5253`), la página de inicio ya es Swagger, con todos los endpoints documentados y probables desde ahí.

## Documentación completa

Ver **[ESTADO_PROYECTO.md](ESTADO_PROYECTO.md)** para el detalle completo: catálogo de todos los endpoints con ejemplos de request/response, modelo de datos, reglas de negocio, integración en red (para consumir la API desde otra laptop), contrato del webhook de liquidación, y estado de avance del proyecto.
