# Estado del proyecto - UTNGolCoin API

Backend del proyecto universitario UTN GolMundial 2026: servicio de moneda virtual y apuestas del Mundial 2026.

## Stack

- ASP.NET Core Web API, .NET 9, con controladores (no minimal APIs).
- Base de datos: MariaDB (127.0.0.1:3307, base `utngolcoin`), acceso con EF Core + Pomelo.EntityFrameworkCore.MySql.
- Se conecta con el frontend público (Fer) y con el backend de Estadísticas en Jakarta EE (Alexis), que llama al webhook `POST /api/utngolcoin/liquidacion`.

## Base de datos

Cadena de conexión (`appsettings.json`, clave `ConnectionStrings:DefaultConnection`):

```
server=127.0.0.1;port=3307;database=utngolcoin;user=root;password=postgres
```

> Nota: la contraseña está en texto plano en `appsettings.json` porque es un entorno académico de desarrollo local. Mejora futura: moverla a variables de entorno o a `dotnet user-secrets` antes de cualquier despliegue real.

El `DbContext` (`Data/AppDbContext.cs`) se conecta con `UseMySql` y `ServerVersion.AutoDetect`, para que detecte automáticamente que el motor es MariaDB. La primera migración (`InicialCreacion`) ya se aplicó y creó la base `utngolcoin` y las 4 tablas.

### Modelo de datos

Todas las tablas son registros planos: se referencian por Id pero no usan relaciones maestro-detalle de EF (sin propiedades de navegación), para mantenerlo simple.

- **Billeteras**: `Id`, `UsuarioId` (referencia lógica al usuario, que vive en el backend de Estadísticas de Alexis, no es FK local), `Saldo` (decimal 18,2), `FechaCreacion`.
- **Transacciones**: `Id`, `BilleteraId`, `Tipo` (BIENVENIDA, PREDICCION, PREMIO, BONO_DIARIO), `Monto` (decimal 18,2), `SaldoResultante` (decimal 18,2), `Referencia` (texto opcional), `Fecha`. Es un ledger: solo se inserta, nunca se edita ni se borra.
- **Predicciones**: `Id`, `UsuarioId`, `PartidoId` (referencia lógica a un partido en Estadísticas), `Pronostico` (LOCAL, EMPATE, VISITANTE), `Monto` (decimal 18,2), `Cuota` (decimal 9,2), `Estado` (PENDIENTE, GANADA, PERDIDA), `Fecha`.
- **BonosDiarios**: `Id`, `UsuarioId`, `Fecha` (tipo fecha sin hora). Tiene un índice único en (`UsuarioId`, `Fecha`) para que la base impida directamente que se otorgue más de un bono al mismo usuario el mismo día.

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

### Sesión 2 - Base de datos (hecha)
- Instalados Pomelo.EntityFrameworkCore.MySql (9.0.0) y Microsoft.EntityFrameworkCore.Design (9.0.0).
- Creadas las 4 entidades en `Models/`: `Billetera`, `Transaccion`, `Prediccion`, `BonoDiario`.
- Creado `Data/AppDbContext.cs` con los 4 `DbSet` y precisión decimal(18,2) / decimal(9,2) para los montos y cuotas.
- Configurada la cadena de conexión a MariaDB y registrado el DbContext en `Program.cs` con `ServerVersion.AutoDetect`.
- Migración `InicialCreacion` creada y aplicada: la base `utngolcoin` y las 4 tablas ya existen en MariaDB (127.0.0.1:3307).

## Pendiente

- Modelado de entidades adicionales si hicieran falta (partidos, catálogo de usuarios local, etc.).
- Lógica de negocio de apuestas y liquidación (servicios en `Services/`).
- Endpoint `POST /api/utngolcoin/liquidacion` para el webhook de Alexis.
