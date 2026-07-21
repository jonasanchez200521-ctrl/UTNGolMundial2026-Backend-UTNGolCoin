# Estado del proyecto - UTNGolCoin API

Backend del proyecto universitario UTN GolMundial 2026: servicio de moneda virtual y apuestas del Mundial 2026.

## Stack

- ASP.NET Core Web API, .NET 9, con controladores (no minimal APIs).
- Base de datos: MariaDB (127.0.0.1:3307, base `utngolcoin`) - se configura en la Sesión 2 con EF Core + Pomelo.
- Se conecta con el frontend público (Fer) y con el backend de Estadísticas en Jakarta EE (Alexis), que llama al webhook `POST /api/utngolcoin/liquidacion`.

## Estructura de carpetas (dentro de `UTNGolCoin.Api/UTNGolCoin.Api`)

- `Controllers/` - Controladores de la API.
- `Models/` - Entidades del dominio.
- `Services/` - DTOs y lógica de negocio.
- `Data/` - DbContext y acceso a datos.

## Progreso por sesión

### Sesión 1 - Andamiaje
- Estructura de carpetas por capas.
- Eliminado el ejemplo WeatherForecast.
- Swagger (Swashbuckle) configurado como página de inicio en `/`.
- Kestrel escuchando en `0.0.0.0` (puertos 5253 http / 7069 https) para acceso desde otras laptops en la red.
- CORS abierto (AllowAnyOrigin/Header/Method) para desarrollo.
- Endpoint de prueba `GET /api/health`.

### Ajuste posterior
- Renombre de carpetas para simplificar la explicación en la defensa: `Domain` -> `Models`, `Infrastructure` -> `Data`, `Application` -> `Services`.

## Pendiente

- Sesión 2: configurar MariaDB, instalar Pomelo.EntityFrameworkCore.MySql y el DbContext.
- Modelado de entidades (usuarios, apuestas, partidos, saldo de moneda virtual).
- Lógica de negocio de apuestas y liquidación.
- Endpoint `POST /api/utngolcoin/liquidacion` para el webhook de Alexis.
