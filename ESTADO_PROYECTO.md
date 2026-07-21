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

También hay una sección `EstadisticasApi` en `appsettings.json` con un placeholder:

```json
"EstadisticasApi": { "BaseUrl": "http://IP_DE_ALEXIS:PUERTO" }
```

Todavía no se usa en el código (no hay ningún `HttpClient` llamando a esta URL). El día de la integración con el backend de Alexis, ahí va la IP real de su servicio en la red, para que en el futuro el backend pueda consultar directamente la hora de los partidos en vez de confiar en el valor que manda el frontend.

El `DbContext` (`Data/AppDbContext.cs`) se conecta con `UseMySql` y `ServerVersion.AutoDetect`, para que detecte automáticamente que el motor es MariaDB. La primera migración (`InicialCreacion`) ya se aplicó y creó la base `utngolcoin` y las 4 tablas.

### Modelo de datos

Todas las tablas son registros planos: se referencian por Id pero no usan relaciones maestro-detalle de EF (sin propiedades de navegación), para mantenerlo simple.

- **Billeteras**: `Id`, `UsuarioId` (referencia lógica al usuario, que vive en el backend de Estadísticas de Alexis, no es FK local), `Saldo` (decimal 18,2), `FechaCreacion`.
- **Transacciones**: `Id`, `BilleteraId`, `Tipo` (BIENVENIDA, PREDICCION, PREMIO, BONO_DIARIO), `Monto` (decimal 18,2), `SaldoResultante` (decimal 18,2), `Referencia` (texto opcional), `Fecha`. Es un ledger: solo se inserta, nunca se edita ni se borra.
- **Predicciones**: `Id`, `UsuarioId`, `PartidoId` (referencia lógica a un partido en Estadísticas), `FechaInicioPartido` (hora de inicio del partido en UTC, usada para el cierre de apuestas RF17 y guardada como referencia/auditoría de con qué hora se validó la apuesta), `Pronostico` (LOCAL, EMPATE, VISITANTE), `Monto` (decimal 18,2), `Cuota` (decimal 9,2), `Estado` (PENDIENTE, GANADA, PERDIDA), `Fecha`.
- **BonosDiarios**: `Id`, `UsuarioId`, `Fecha` (tipo fecha sin hora). Tiene un índice único en (`UsuarioId`, `Fecha`) para que la base impida directamente que se otorgue más de un bono al mismo usuario el mismo día.

## Endpoints (para Fer - frontend)

### `POST /api/billeteras`
Crea la billetera de un usuario nuevo y le acredita el bono de bienvenida de 10 UTNGolCoin (queda registrado como transacción tipo `BIENVENIDA` en el ledger). El usuario en sí no se crea acá: solo su billetera, referenciada por `UsuarioId`.

- **Body (JSON):**
  ```json
  { "usuarioId": 501 }
  ```
- **201 Created** - billetera creada:
  ```json
  { "id": 1, "usuarioId": 501, "saldo": 10.00, "fechaCreacion": "2026-07-21T22:43:34Z" }
  ```
- **400 Bad Request** - si `usuarioId` falta o es <= 0.
- **409 Conflict** - si ese usuario ya tiene una billetera creada.

### `GET /api/billeteras/{usuarioId}`
Devuelve el saldo y los datos de la billetera de un usuario.

- **200 OK:**
  ```json
  { "id": 1, "usuarioId": 501, "saldo": 10.00, "fechaCreacion": "2026-07-21T22:43:34Z" }
  ```
- **404 Not Found** - si ese usuario todavía no tiene billetera creada.

### `POST /api/predicciones`
Crea una apuesta 1X2 sobre un partido (el partido vive en el backend de Estadísticas de Alexis, acá solo se guarda su `PartidoId`). Descuenta el monto de la billetera, registra la transacción `PREDICCION` (con monto negativo) y crea la predicción en estado `PENDIENTE`.

- **Body (JSON):**
  ```json
  { "usuarioId": 501, "partidoId": 1001, "fechaInicioPartido": "2026-08-01T20:00:00Z", "pronostico": "LOCAL", "monto": 4 }
  ```
  `pronostico` acepta `LOCAL`, `EMPATE` o `VISITANTE` (no distingue mayúsculas/minúsculas). **`fechaInicioPartido` debe mandarse en UTC, en formato ISO 8601 con la "Z" al final** (ej. `2026-08-01T20:00:00Z`), no en hora local de Argentina. Si Fer manda la hora local, tiene que convertirla a UTC antes (restarle las horas de diferencia) para que la validación de cierre compare correctamente contra la hora del servidor.
- **201 Created** - predicción creada:
  ```json
  { "id": 1, "usuarioId": 501, "partidoId": 1001, "fechaInicioPartido": "2026-08-01T20:00:00Z", "pronostico": "LOCAL", "monto": 4.00, "cuota": 2.00, "estado": "PENDIENTE", "fecha": "2026-07-21T23:10:48Z" }
  ```
- **400 Bad Request** - si `usuarioId`/`partidoId` faltan o son <= 0, si el monto es <= 0, si el pronóstico no es LOCAL/EMPATE/VISITANTE, si `fechaInicioPartido` ya pasó (mensaje "Las apuestas para este partido ya cerraron", RF17), o si el saldo es insuficiente para el monto pedido.
- **404 Not Found** - si el usuario no tiene billetera creada todavía.
- **409 Conflict** - si el usuario ya tiene una predicción para ese mismo partido.

**Cierre de apuestas por hora (RF17):** no se puede apostar a un partido cuya hora de inicio ya pasó (o es exactamente ahora). Por ahora se confía en el `fechaInicioPartido` que manda el frontend, porque el partido en sí vive en el backend de Estadísticas de Alexis y todavía no hay integración directa con ese servicio. El día que esa integración exista, esta validación se puede reforzar consultando la hora real del partido en la API de Alexis (ver sección `EstadisticasApi` más abajo) en vez de confiar en el dato del frontend.

**Cuotas fijas** (constante `CuotasPorPronostico` en `Services/PrediccionService.cs`, fácil de cambiar): LOCAL = 2.0, EMPATE = 3.0, VISITANTE = 2.5. El proyecto no exige cuotas dinámicas, así que se guarda esta cuota fija en la predicción para usarla más adelante al pagar el premio.

### `GET /api/predicciones/usuario/{usuarioId}` (RF22)
Devuelve todas las predicciones de un usuario (más recientes primero), con su estado actual. Ya traía todos los campos que Fer necesita para mostrar el detalle de cada apuesta, así que no hizo falta cambiarla en la Sesión 8; solo se revisó y se confirmó que cumple RF22 tal cual estaba.

- **200 OK:**
  ```json
  [{ "id": 1, "usuarioId": 501, "partidoId": 1001, "fechaInicioPartido": "2026-08-01T20:00:00Z", "pronostico": "LOCAL", "monto": 4.00, "cuota": 2.00, "estado": "PENDIENTE", "fecha": "2026-07-21T23:10:48Z" }]
  ```
  `estado` puede ser `PENDIENTE`, `GANADA` o `PERDIDA`. Si el usuario no tiene predicciones, devuelve una lista vacía `[]` (no es un error).

### `GET /api/ranking` (RF21)
Devuelve la tabla de clasificación pública de usuarios, para que Fer la muestre en el frontend. Incluye a todo usuario que tenga billetera, aunque no haya apostado todavía (con 0 aciertos).

- **Parámetro opcional:** `?top=N` para limitar a los primeros N puestos. Sin el parámetro, devuelve a todos los usuarios.
- **200 OK:**
  ```json
  [
    { "usuarioId": 900, "saldo": 14.00, "aciertos": 1, "totalPredicciones": 1 },
    { "usuarioId": 902, "saldo": 11.00, "aciertos": 1, "totalPredicciones": 2 },
    { "usuarioId": 100, "saldo": 5.00, "aciertos": 0, "totalPredicciones": 2 }
  ]
  ```
- **400 Bad Request** - si `top` es <= 0.

**Criterio de orden usado:** primero por `saldo` descendente (es la métrica principal del juego: cuánto UTNGolCoin tiene acumulado cada usuario), y como desempate, por `aciertos` (predicciones GANADA) descendente, para distinguir entre usuarios que quedaron con el mismo saldo pero acertaron más pronósticos.

### `POST /api/utngolcoin/liquidacion` - contrato con Alexis (Estadísticas)

**Esta es la ruta exacta que Alexis debe llamar** cuando su backend registra el resultado final de un partido. El mismo endpoint también sirve para disparar la liquidación **manualmente desde Swagger** durante la exposición, si Alexis no está conectado: llamarlo a mano con el `partidoId` y el `resultado` funciona exactamente igual que si lo llamara su sistema.

- **Body (JSON) que espera de Alexis:**
  ```json
  { "partidoId": 1001, "resultado": "LOCAL" }
  ```
  - `partidoId` (número, obligatorio): el mismo id de partido que se usó al crear las predicciones.
  - `resultado` (texto, obligatorio): `LOCAL`, `EMPATE` o `VISITANTE` (no distingue mayúsculas/minúsculas). Es el resultado final del partido, no el pronóstico de nadie.
  - Alexis puede mandar campos extra en el mismo body (ej. `fase`, `grupo`, según su documento) **sin que rompan la petición**: el backend los ignora automáticamente porque no están declarados en el DTO.
- **200 OK - resumen de la liquidación:**
  ```json
  { "partidoId": 1001, "liquidadas": 3, "ganadas": 1, "perdidas": 2, "totalPagado": 8.00 }
  ```
  - `liquidadas`: cuántas predicciones PENDIENTES de ese partido se procesaron en esta llamada.
  - `ganadas` / `perdidas`: cuántas de esas pasaron a GANADA o PERDIDA.
  - `totalPagado`: suma de todos los premios pagados (monto × cuota) en esta llamada.
- **400 Bad Request** - si `partidoId` falta o es <= 0, o si `resultado` no es LOCAL/EMPATE/VISITANTE.
- Si el partido no tiene ninguna predicción PENDIENTE (porque nadie apostó, o porque ya se liquidó antes), **no es un error**: responde `200 OK` con `liquidadas: 0`.

**Qué hace al liquidar:** busca todas las predicciones PENDIENTES de ese `partidoId`. Las que coinciden con el `resultado` pasan a GANADA, se les paga `monto × cuota` (sumado a su billetera) y se registra una transacción tipo `PREMIO`. Las que no coinciden pasan a PERDIDA sin ningún pago (el monto ya se había descontado al apostar). Todo se guarda en una misma transacción de base de datos.

**Idempotencia:** el endpoint solo toca predicciones en estado PENDIENTE. Una vez liquidadas, quedan en GANADA/PERDIDA, así que si Alexis llama dos veces por el mismo partido (por sus reintentos configurados), o si lo llamás vos manualmente de nuevo, la segunda llamada no encuentra pendientes y responde `liquidadas: 0` sin pagar de nuevo. Probado manualmente: la segunda llamada al mismo partido no modifica el saldo.

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

### Sesión 3 - Billetera y bono de bienvenida (hecha)
- Creado `Services/BilleteraService.cs`: crea la billetera de un usuario con saldo inicial de 10 y registra la transacción `BIENVENIDA` en el ledger (billetera + transacción se guardan dentro de una misma transacción de base de datos); también consulta la billetera por `UsuarioId`.
- Creados los DTOs `Services/Dtos/CrearBilleteraRequest.cs` y `Services/Dtos/BilleteraResponse.cs` (los controladores no exponen las entidades de EF directamente).
- Creado `Controllers/BilleterasController.cs` con `POST /api/billeteras` y `GET /api/billeteras/{usuarioId}` (documentados arriba).
- Probado manualmente: crear billetera da saldo 10 y genera la transacción BIENVENIDA, consultar saldo funciona, y crear la misma billetera dos veces devuelve 409.

### Sesión 4 - Apuestas / predicciones (hecha)
- Creado `Services/PrediccionService.cs`: valida que el usuario tenga billetera (404 si no), que el monto sea mayor a 0, que el pronóstico sea válido, que el saldo alcance, y que no exista ya una predicción del usuario para ese partido (409). Si todo es válido, descuenta el saldo, registra la transacción `PREDICCION` (monto negativo, `Referencia` = id de la predicción) y crea la predicción en `PENDIENTE`, todo dentro de una misma transacción de base de datos.
- Cuotas fijas por pronóstico: LOCAL 2.0, EMPATE 3.0, VISITANTE 2.5 (constante fácil de cambiar en el servicio).
- Creados los DTOs `CrearPrediccionRequest` y `PrediccionResponse`.
- Creado `Controllers/PrediccionesController.cs` con `POST /api/predicciones` y `GET /api/predicciones/usuario/{usuarioId}` (documentados arriba).
- Probado manualmente: apuesta válida descuenta saldo y crea transacción + predicción; apostar más del saldo da 400; apostar dos veces al mismo partido da 409; usuario sin billetera da 404; pronóstico o monto inválido dan 400.

### Sesión 5 - Cierre de apuestas por hora del partido, RF17 (hecha)
- Agregado el campo `FechaInicioPartido` a la entidad `Prediccion` (migración `AgregarFechaInicioPartidoAPredicciones`) para guardar con qué hora se validó cada apuesta.
- `POST /api/predicciones` ahora recibe también `fechaInicioPartido` (UTC, ISO 8601 con "Z").
- Nueva validación en `PrediccionService`, agregada en el orden: monto > 0 -> pronóstico válido -> partido no cerrado por hora (`fechaInicioPartido` no puede ser anterior o igual a la hora actual del servidor) -> billetera existe -> saldo suficiente -> no hay apuesta duplicada.
- Agregada la sección `EstadisticasApi` en `appsettings.json` (placeholder de `BaseUrl`, todavía sin usar en código) para cuando exista integración directa con el backend de Alexis.
- Probado manualmente: partido con fecha futura permite apostar normalmente; partido con fecha pasada se rechaza con 400 y el mensaje "Las apuestas para este partido ya cerraron"; se confirmó que las validaciones de billetera/saldo/duplicado de la Sesión 4 siguen funcionando igual.

### Sesión 6 - Liquidación de premios, RF12 y RF19 (hecha)
- Creado `Services/LiquidacionService.cs`: busca las predicciones PENDIENTES de un partido, paga `monto × cuota` a las que coinciden con el resultado (GANADA, con transacción tipo `PREMIO`) y marca sin pago a las que no (PERDIDA). Todo dentro de una misma transacción de base de datos.
- Creado `Controllers/LiquidacionController.cs` con la ruta exacta `POST /api/utngolcoin/liquidacion` (documentado como contrato con Alexis más arriba). El mismo endpoint sirve para disparo manual desde Swagger en la demo.
- Idempotencia por diseño: solo se procesan predicciones PENDIENTES, así que llamar dos veces al mismo partido no vuelve a pagar (segunda llamada devuelve `liquidadas: 0`).
- Probado manualmente el flujo completo: apuesta -> liquidar con resultado ganador (sube el saldo, transacción PREMIO, estado GANADA) -> apuesta que pierde -> liquidar (sin pago, estado PERDIDA) -> volver a liquidar el mismo partido (0 liquidadas, saldo sin cambios) -> resultado inválido (400) -> partido sin apuestas (200, 0 liquidadas) -> campos extra tipo `fase`/`grupo` no rompen la petición.

### Sesión 8 - Ranking y consulta de apuestas, RF21 y RF22 (hecha)
- Creado `Services/RankingService.cs`: junta cada billetera con sus predicciones (aciertos = GANADA, total = todas), ordenado por saldo descendente y, como desempate, por aciertos descendente.
- Creado `Controllers/RankingController.cs` con `GET /api/ranking` (parámetro opcional `?top=N`, documentado arriba).
- Revisado `GET /api/predicciones/usuario/{usuarioId}` (de la Sesión 4): ya devolvía `partidoId`, `pronostico`, `monto`, `cuota`, `estado` y `fecha`, así que cumple RF22 sin cambios.
- Probado manualmente con dos usuarios: apuestas ganadas y perdidas repartidas entre ambos, liquidadas, y se confirmó que el ranking los ordena bien por saldo y que el desempate por aciertos funciona entre usuarios con el mismo saldo.

## Pendiente

- Modelado de entidades adicionales si hicieran falta (partidos, catálogo de usuarios local, etc.).
- Endpoint para bono diario (usa la tabla `BonosDiarios` ya creada).
- Integración real con la API de Estadísticas de Alexis (usar `EstadisticasApi:BaseUrl` para consultar la hora de los partidos en vez de confiar en el frontend).
