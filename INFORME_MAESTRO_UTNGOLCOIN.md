# INFORME MAESTRO - UTNGolCoin API

**Documento autosuficiente.** Si estás leyendo esto sin ningún otro contexto del proyecto (por ejemplo, una IA a la que Fer o Dayana le pasaron este archivo para construir su frontend), esto alcanza para entender el backend completo, sus reglas de negocio, su catálogo de endpoints con ejemplos reales, y cómo levantarlo. No hace falta abrir el repositorio ni leer el código para consumir esta API — pero si lo hacés, todo lo que dice este documento fue verificado contra ese código real y contra la API corriendo (ver nota de cierre al final).

---

## 1. Resumen ejecutivo

**UTNGolCoin** es el backend de moneda virtual y apuestas del proyecto integrador universitario **UTN GolMundial 2026**, un sistema para seguir el Mundial 2026 con una dinámica de apuestas con moneda ficticia (sin valor real).

El proyecto integrador completo tiene **4 componentes**, desarrollados por 4 personas distintas:

| Componente | Responsable | Tecnología | Rol |
|---|---|---|---|
| **UTNGolCoin (este backend)** | — | ASP.NET Core Web API, .NET 9 | Moneda virtual, billeteras, apuestas, liquidación de premios, ranking, reportes |
| Backend de Estadísticas | Alexis | Jakarta EE, Linux | Dueño de los usuarios y los partidos del Mundial; datos reales de resultados |
| Frontend público | Fer | C# | Interfaz para que los usuarios vean su saldo, aposten, vean el ranking |
| Frontend administrativo | Dayana | — | Panel para ver reportes y gestionar el sistema |

**Este backend NO tiene tablas de usuarios ni de partidos.** Esos existen en el sistema de Alexis. UTNGolCoin solo recibe un `usuarioId` y un `partidoId` como números de referencia (enteros) y trabaja con ellos, confiando en que quien se los manda (el frontend, o Alexis en el caso del webhook) los usa correctamente.

La comunicación entre Alexis y UTNGolCoin es **unidireccional**: Alexis le avisa a UTNGolCoin cuándo termina un partido y con qué resultado, llamando a un endpoint específico (webhook). UTNGolCoin nunca llama a Alexis. Ver sección 2.3 y sección 6.

---

## 2. Arquitectura

### 2.1 Stack técnico exacto

- **Framework:** ASP.NET Core Web API, **.NET 9**, con controladores tradicionales (no minimal APIs).
- **Base de datos:** **MariaDB**, host `127.0.0.1`, puerto **3307**, usuario `root`, password `postgres`, base de datos `utngolcoin`.
- **ORM:** Entity Framework Core, proveedor **Pomelo.EntityFrameworkCore.MySql** v9.0.0 (permite que EF Core hable con MariaDB/MySQL) + **Microsoft.EntityFrameworkCore.Design** v9.0.0 (necesario para generar y aplicar migraciones).
- **Documentación de API:** **Swashbuckle.AspNetCore** v10.2.3 (Swagger/OpenAPI). Swagger UI es la página de inicio del servicio (ruta `/`).
- **CORS:** política abierta (`AllowAnyOrigin`, `AllowAnyHeader`, `AllowAnyMethod`) — cualquier origen puede llamar a la API, pensado para desarrollo y demo en red local.

### 2.2 Estructura de carpetas real

Todo el código vive en `UTNGolCoin.Api/UTNGolCoin.Api/`:

- **`Models/`** - Las 4 entidades de datos que mapean 1:1 a las tablas de la base: `Billetera.cs`, `Transaccion.cs`, `Prediccion.cs`, `BonoDiario.cs`. Son clases planas (propiedades simples), sin relaciones de navegación entre ellas.
- **`Data/`** - `AppDbContext.cs`, el `DbContext` de EF Core: expone los 4 `DbSet` y configura la precisión decimal de los montos y el índice único de `BonosDiarios`.
- **`Services/`** - Toda la lógica de negocio, un servicio por área funcional: `BilleteraService`, `PrediccionService`, `LiquidacionService`, `BonoDiarioService`, `RankingService`, `ReporteService`, `TransaccionService`. Cada uno recibe el `AppDbContext` inyectado y expone métodos async.
- **`Services/Dtos/`** - Las clases de request/response que usan los controllers. Los controllers **nunca** exponen las entidades de `Models/` directamente al exterior; siempre pasan por un DTO. Ejemplos: `CrearBilleteraRequest`, `BilleteraResponse`, `PrediccionResponse`, etc.
- **`Controllers/`** - 8 controllers, uno por recurso/área: `BilleterasController`, `PrediccionesController`, `LiquidacionController`, `BonosController`, `RankingController`, `ReportesController`, `TransaccionesController`, `HealthController`. En total exponen **12 endpoints** (catálogo completo en la sección 5).
- **`Migrations/`** - Migraciones de EF Core que crean/modifican las tablas en MariaDB.

### 2.3 Cómo se comunica con los demás - Degradación controlada (RNF05)

**Verificado en el código fuente:** se buscó en todo el proyecto (`grep` de `HttpClient`, `IHttpClientFactory`, `AddHttpClient`) y **no existe ninguna llamada HTTP saliente** desde este backend hacia ningún otro servicio. UTNGolCoin nunca llama a Alexis, ni a los frontends de Fer o Dayana. Solo expone endpoints que otros consumen.

Esto significa que la relación es:
- **Alexis → UTNGolCoin:** Alexis llama al webhook `POST /api/utngolcoin/liquidacion` cuando termina un partido (ver sección 6). UTNGolCoin es pasivo, solo espera esa llamada.
- **Fer/Dayana → UTNGolCoin:** los frontends llaman a los demás endpoints para todo lo demás (billeteras, apuestas, ranking, reportes, etc.).
- **UTNGolCoin → nadie:** no hace ninguna llamada saliente.

**Consecuencia práctica de esto (degradación controlada, RNF05):** si el backend de Alexis se cae o no está disponible, **UTNGolCoin sigue funcionando al 100%** para todo lo que no dependa de un resultado de partido: crear billeteras, apostar (mientras el partido no haya cerrado por hora), consultar saldo, historial, ranking, reportes, y el bono antibancarrota. Lo único que queda en pausa es la liquidación de los partidos que Alexis todavía no informó — esas predicciones simplemente quedan en estado `PENDIENTE` indefinidamente, sin generar ningún error, timeout ni caída en cascada, porque UTNGolCoin nunca está esperando una respuesta de Alexis.

Hay un placeholder en `appsettings.json`, sección `EstadisticasApi:BaseUrl`, pensado para una futura integración opcional (consultar la hora real de un partido en vez de recibirla del frontend). **No se usa en ningún lugar del código todavía** — mientras siga así, la independencia entre servicios se mantiene intacta.

### 2.4 Configuración de red

- **Kestrel** (el servidor web de ASP.NET Core) escucha en **`0.0.0.0`** (todas las interfaces de red), no solo en `localhost`. Configurado en `appsettings.json`:
  ```json
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://0.0.0.0:5253" },
      "Https": { "Url": "https://0.0.0.0:7069" }
    }
  }
  ```
  Puertos: **5253 (HTTP)** y **7069 (HTTPS)**. Esta configuración de Kestrel tiene prioridad sobre `launchSettings.json`, así que aplica tanto corriendo con `dotnet run`/Visual Studio como publicando y corriendo el ejecutable directo.
- **CORS abierto** (ver 2.1) para que cualquier frontend, desde cualquier origen, pueda llamar sin bloqueos del navegador.
- **appsettings.json - sección `EstadisticasApi`:** placeholder para cuando exista integración directa con el backend de Alexis:
  ```json
  "EstadisticasApi": { "BaseUrl": "http://IP_DE_ALEXIS:PUERTO" }
  ```
  Ver sección 9 para más detalle de red.

---

## 3. Modelo de datos

Las 4 tablas son **registros planos**: se referencian entre sí por Id (enteros simples), pero **no usan relaciones maestro-detalle de Entity Framework** (no hay propiedades de navegación ni Foreign Keys reales entre `Predicciones` y `Billeteras`, por ejemplo). Es una decisión de diseño deliberada para mantenerlo simple y fácil de explicar en una defensa académica.

### `Billeteras`
| Campo | Tipo (C# / SQL) | Descripción |
|---|---|---|
| `Id` | `int` (PK, autoincremental) | |
| `UsuarioId` | `int` | Referencia lógica al usuario que vive en el backend de Alexis. **No es una FK real** — acá no hay tabla de usuarios. |
| `Saldo` | `decimal(18,2)` | Saldo actual en UTNGolCoin. |
| `FechaCreacion` | `datetime` | Cuándo se creó la billetera (UTC). |

### `Transacciones` (ledger inmutable)
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | `int` (PK) | |
| `BilleteraId` | `int` | A qué billetera pertenece el movimiento (esta sí es una referencia interna, a una tabla que existe en esta misma base). |
| `Tipo` | `string` | Uno de: `BIENVENIDA`, `PREDICCION`, `PREMIO`, `BONO_DIARIO`. |
| `Monto` | `decimal(18,2)` | Puede ser negativo (ej. al apostar se registra el descuento como monto negativo). |
| `SaldoResultante` | `decimal(18,2)` | Saldo de la billetera **después** de aplicar este movimiento. |
| `Referencia` | `string?` (opcional) | Dato adicional de contexto — en la práctica, el Id de la predicción relacionada (como texto), o `null` si no aplica (ej. en `BIENVENIDA` y `BONO_DIARIO`). |
| `Fecha` | `datetime` | UTC. |

**Es un ledger de solo inserción: verificado en el código que ningún servicio hace `Update` ni `Remove` sobre `Transacciones`, solo `Add`.** Es el historial completo e inalterable de todos los movimientos de cada billetera.

### `Predicciones` (apuestas)
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | `int` (PK) | |
| `UsuarioId` | `int` | Referencia lógica al usuario (Alexis). |
| `PartidoId` | `int` | Referencia lógica a un partido (Alexis). |
| `FechaInicioPartido` | `datetime` (UTC) | Hora de inicio del partido, tal como la mandó quien creó la apuesta. Se usa para el cierre de apuestas por hora (RF17) y queda guardada como auditoría de con qué hora se validó esa apuesta en particular. |
| `Pronostico` | `string` | Uno de: `LOCAL`, `EMPATE`, `VISITANTE`. |
| `Monto` | `decimal(18,2)` | Monto apostado. |
| `Cuota` | `decimal(9,2)` | La cuota fija vigente para ese pronóstico **en el momento de apostar** (ver sección 4). |
| `Estado` | `string` | Uno de: `PENDIENTE`, `GANADA`, `PERDIDA`. |
| `Fecha` | `datetime` (UTC) | Cuándo se hizo la apuesta. |

### `BonosDiarios`
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | `int` (PK) | |
| `UsuarioId` | `int` | Referencia lógica al usuario. |
| `Fecha` | `date` (sin componente de hora, tipo `DateOnly` en C#) | El día en que se otorgó el bono a ese usuario. |

**Índice único en (`UsuarioId`, `Fecha`)**, verificado en `AppDbContext.OnModelCreating`:
```csharp
modelBuilder.Entity<BonoDiario>()
    .HasIndex(b => new { b.UsuarioId, b.Fecha })
    .IsUnique();
```
Esto hace que sea **la base de datos misma** la que impide dar dos bonos al mismo usuario el mismo día, no solo una validación de la aplicación.

---

## 4. Reglas de negocio

Todas verificadas leyendo el código de los `Services/` correspondientes:

1. **Bono de bienvenida:** al crear una billetera (`POST /api/billeteras`), el usuario arranca con **10.00 UTNGolCoin** (constante `MontoBonoBienvenida = 10m` en `BilleteraService.cs`), registrados como una transacción tipo `BIENVENIDA` con `SaldoResultante = 10.00`.

2. **Cuotas fijas por pronóstico** (constante `CuotasPorPronostico` en `PrediccionService.cs`, un diccionario fácil de editar):
   - `LOCAL` = **2.0**
   - `EMPATE` = **3.0**
   - `VISITANTE` = **2.5**
   No hay cuotas dinámicas ni cálculo en base a estadísticas — son fijas por diseño (el enunciado del proyecto no las exige dinámicas).
   **Por qué se guarda la cuota en la predicción:** porque la cuota podría cambiarse en el futuro (es una constante editable), y al liquidar hay que pagar exactamente con la cuota que estaba vigente **cuando el usuario apostó**, no la que esté vigente el día de la liquidación. Guardarla en la fila de la predicción es lo que hace posible eso.

3. **Una sola apuesta por usuario por partido:** verificado en `PrediccionService.CrearPrediccionAsync` — antes de crear la predicción, se chequea con `AnyAsync(p => p.UsuarioId == usuarioId && p.PartidoId == partidoId)`. Si ya existe, se rechaza con `409 Conflict`. (Nota: esta unicidad se valida a nivel de aplicación, no hay un índice único de base de datos para esto — a diferencia del bono diario, que sí lo tiene.)

4. **Cierre de apuestas por hora (RF17):** no se puede apostar a un partido cuya `FechaInicioPartido` ya pasó, ni exactamente en el instante en que empieza. La validación exacta en código: `if (fechaInicioUtc <= DateTime.UtcNow) throw ...`. La fecha se recibe del frontend (no hay integración real con Alexis todavía para consultarla) y se normaliza siempre a UTC antes de comparar.

5. **Validación de saldo:** no se puede apostar más de lo que hay en la billetera. Validación: `if (billetera.Saldo < monto) throw ...`, con un mensaje que incluye el saldo actual y el monto solicitado.

6. **Liquidación (`monto × cuota`), con estados PENDIENTE → GANADA/PERDIDA:** al recibir el resultado de un partido, para cada predicción en estado `PENDIENTE` de ese partido:
   - Si `Pronostico == resultado`: pasa a `GANADA`, se le suma a la billetera `Monto × Cuota` (redondeado a 2 decimales, `MidpointRounding.AwayFromZero`), y se registra una transacción tipo `PREMIO` por ese monto.
   - Si no coincide: pasa a `PERDIDA`, sin ningún pago adicional (el monto ya se había descontado de la billetera al momento de apostar).

7. **Idempotencia de la liquidación:** verificado en código — el query que busca qué liquidar filtra explícitamente `Estado == "PENDIENTE"`. Una vez que una predicción pasa a `GANADA` o `PERDIDA`, una segunda llamada a `POST /api/utngolcoin/liquidacion` con el mismo `partidoId` no la vuelve a encontrar, así que no puede pagarse dos veces. **Probado en vivo:** se liquidó un partido, se liquidó una segunda vez, y la segunda respuesta fue `{"liquidadas":0,"ganadas":0,"perdidas":0,"totalPagado":0}` sin cambios en el saldo.

8. **Bono antibancarrota (RF20):** un usuario con `Saldo <= 0` recibe **1.00 UTNGolCoin** (constante `MontoBonoDiario = 1m`) por día, registrado como transacción tipo `BONO_DIARIO`, y se guarda una fila en `BonosDiarios` con ese usuario y esa fecha. La condición exacta en código es `Saldo <= 0`, no solo `== 0`.

9. **Idempotencia del bono por día:** doble protección verificada en código:
   - A nivel de aplicación: `BonoDiarioService` excluye explícitamente de la búsqueda a los usuarios que ya tienen una fila en `BonosDiarios` para esa fecha exacta.
   - A nivel de base de datos: el índice único (`UsuarioId`, `Fecha`) de la tabla (sección 3) haría fallar un segundo insert aunque la aplicación no lo filtrara.
   **Probado en vivo:** se ejecutó el bono para un usuario en 0 (recibió su moneda), se ejecutó de nuevo para la misma fecha, y la respuesta fue `{"cantidadBeneficiados":0,"beneficiarios":[]}`.
   **Simulación de días para la demo:** el endpoint acepta una `fecha` opcional en el body. Mandar fechas distintas en llamadas sucesivas simula "pasar de día" sin esperar 24 horas reales — pensado explícitamente para la exposición.

10. **Ranking:** ordenado por `Saldo` descendente primero, y como desempate, por `Aciertos` (predicciones `GANADA`) descendente. Verificado en `RankingService`: `OrderByDescending(r => r.Saldo).ThenByDescending(r => r.Aciertos)`.

---

## 5. Catálogo completo de endpoints

**Los 12 endpoints existentes**, agrupados por controller. Formato de error uniforme en todos: `{ "mensaje": "texto legible" }` (ver también sección 7 de `ESTADO_PROYECTO.md` sobre por qué es consistente en todo el proyecto, incluyendo errores automáticos del framework).

Todas las respuestas de éxito y error de esta sección son **copias literales de respuestas reales** obtenidas corriendo la API contra la base de datos, no ejemplos inventados.

### HealthController

#### `GET /api/health`
Chequeo de que el servicio está arriba.
- **200 OK:**
  ```json
  {"status":"ok","timestamp":"2026-07-22T00:47:02.8083168Z"}
  ```

---

### BilleterasController

#### `POST /api/billeteras`
Crea la billetera de un usuario y le acredita el bono de bienvenida de 10 UTNGolCoin. El usuario en sí **no se crea acá** (vive en Alexis); esto solo crea su billetera local.
- **Body:** `{ "usuarioId": 995 }`
- **201 Created:**
  ```json
  {"id":13,"usuarioId":995,"saldo":10,"fechaCreacion":"2026-07-22T00:47:16.1812796Z"}
  ```
- **400 Bad Request** (usuarioId falta o es <= 0):
  ```json
  {"mensaje":"UsuarioId es requerido y debe ser mayor a 0."}
  ```
- **409 Conflict** (ya existía):
  ```json
  {"mensaje":"El usuario 995 ya tiene una billetera."}
  ```

#### `GET /api/billeteras/{usuarioId}`
Consulta saldo y datos de la billetera.
- **200 OK:**
  ```json
  {"id":13,"usuarioId":995,"saldo":10.00,"fechaCreacion":"2026-07-22T00:47:16.181279"}
  ```
- **404 Not Found:**
  ```json
  {"mensaje":"No existe una billetera para el usuario 88888."}
  ```

---

### PrediccionesController

#### `POST /api/predicciones`
Crea una apuesta 1X2 sobre un partido.
- **Body:**
  ```json
  { "usuarioId": 995, "partidoId": 9951, "fechaInicioPartido": "2026-08-01T20:00:00Z", "pronostico": "LOCAL", "monto": 4 }
  ```
  - `pronostico`: `LOCAL` | `EMPATE` | `VISITANTE` (no distingue mayúsculas/minúsculas, se normaliza internamente).
  - `fechaInicioPartido`: **debe mandarse en UTC, ISO 8601 con "Z" al final** (ver sección 7).
- **201 Created:**
  ```json
  {"id":16,"usuarioId":995,"partidoId":9951,"fechaInicioPartido":"2026-08-01T20:00:00Z","pronostico":"LOCAL","monto":4,"cuota":2.0,"estado":"PENDIENTE","fecha":"2026-07-22T00:47:28.3253122Z"}
  ```
- **400 Bad Request**, varios casos posibles (mensajes reales capturados en pruebas):
  - `usuarioId`/`partidoId` faltan o son <= 0: `{"mensaje":"UsuarioId y PartidoId son requeridos y deben ser mayores a 0."}`
  - Monto <= 0: `{"mensaje":"El monto debe ser mayor a 0."}`
  - Pronóstico inválido: `{"mensaje":"El pronóstico debe ser LOCAL, EMPATE o VISITANTE."}`
  - Partido ya cerrado por hora (RF17): `{"mensaje":"Las apuestas para este partido ya cerraron."}`
  - Saldo insuficiente: `{"mensaje":"Saldo insuficiente. Saldo actual: 6.00, monto solicitado: 999."}`
- **404 Not Found** (sin billetera): `{"mensaje":"El usuario 88888 no tiene una billetera creada."}`
- **409 Conflict** (apuesta duplicada): `{"mensaje":"El usuario 995 ya tiene una predicción para el partido 9951."}`

#### `GET /api/predicciones/usuario/{usuarioId}`
Lista las predicciones de un usuario, más recientes primero.
- **200 OK** (con datos):
  ```json
  [{"id":16,"usuarioId":995,"partidoId":9951,"fechaInicioPartido":"2026-08-01T20:00:00","pronostico":"LOCAL","monto":4.00,"cuota":2.00,"estado":"PENDIENTE","fecha":"2026-07-22T00:47:28.325312"}]
  ```
- **200 OK** (usuario sin apuestas, no es error): `[]`

---

### LiquidacionController

#### `POST /api/utngolcoin/liquidacion`
**Ruta exacta acordada con el backend de Estadísticas de Alexis.** Ver contrato completo en la sección 6.
- **Body:** `{ "partidoId": 9951, "resultado": "LOCAL" }` (puede traer campos extra, se ignoran).
- **200 OK** (con ganadores):
  ```json
  {"partidoId":9951,"liquidadas":1,"ganadas":1,"perdidas":0,"totalPagado":8.00}
  ```
- **200 OK** (segunda llamada al mismo partido — idempotencia, o partido sin pendientes):
  ```json
  {"partidoId":9951,"liquidadas":0,"ganadas":0,"perdidas":0,"totalPagado":0}
  ```
- **400 Bad Request:**
  - `partidoId` inválido: `{"mensaje":"PartidoId es requerido y debe ser mayor a 0."}`
  - Resultado inválido: `{"mensaje":"El resultado debe ser LOCAL, EMPATE o VISITANTE."}`

---

### RankingController

#### `GET /api/ranking`
Tabla de clasificación pública, ordenada por saldo descendente y luego por aciertos descendente (ver regla 10 de la sección 4).
- **Parámetro opcional de query:** `?top=N`.
- **200 OK:**
  ```json
  [{"usuarioId":900,"saldo":14.00,"aciertos":1,"totalPredicciones":1},{"usuarioId":995,"saldo":14.00,"aciertos":1,"totalPredicciones":1},{"usuarioId":100,"saldo":5.00,"aciertos":0,"totalPredicciones":2}]
  ```
- **400 Bad Request** (`top` <= 0): `{"mensaje":"El parámetro top debe ser mayor a 0."}`

---

### BonosController

#### `POST /api/bonos/ejecutar-bono-diario`
Bono antibancarrota (RF20): da 1 UTNGolCoin a todos los usuarios con saldo <= 0 que no lo recibieron todavía en la fecha indicada.
- **Body, opcional:** `{ "fecha": "2026-07-23" }`. Sin body (o body `{}`), usa el día de hoy en UTC.
- **200 OK** (con beneficiados):
  ```json
  {"fecha":"2026-07-22","cantidadBeneficiados":1,"beneficiarios":[{"usuarioId":995,"saldoNuevo":1.00}]}
  ```
- **200 OK** (nadie calificaba, o ya se les había dado el bono esa fecha — no es error):
  ```json
  {"fecha":"2026-07-22","cantidadBeneficiados":0,"beneficiarios":[]}
  ```

#### `GET /api/bonos/estado/{usuarioId}`
Consulta si un usuario está en bancarrota y si ya recibió el bono hoy.
- **200 OK:**
  ```json
  {"usuarioId":995,"saldo":0.00,"enBancarrota":true,"yaRecibioBonoHoy":false,"fecha":"2026-07-22"}
  ```
- **404 Not Found:** `{"mensaje":"El usuario 88888 no tiene una billetera creada."}`

---

### ReportesController

#### `GET /api/reportes/monedas-circulacion`
- **200 OK:**
  ```json
  {"totalMonedasEnCirculacion":21.00,"cantidadBilleteras":4,"totalPagadoEnPremios":16.00}
  ```

#### `GET /api/reportes/partidos-mas-apostados`
- **Parámetro opcional:** `?top=N`.
- **200 OK:**
  ```json
  [{"partidoId":2001,"cantidadPredicciones":1},{"partidoId":3001,"cantidadPredicciones":1},{"partidoId":7001,"cantidadPredicciones":1}]
  ```
- **400 Bad Request** (`top` <= 0): `{"mensaje":"El parámetro top debe ser mayor a 0."}`

---

### TransaccionesController

#### `GET /api/transacciones/usuario/{usuarioId}`
Historial completo del ledger de un usuario (RF14): bonos, predicciones y premios, del más reciente al más antiguo.
- **200 OK:**
  ```json
  [
    {"id":38,"tipo":"PREMIO","monto":8.00,"saldoResultante":14.00,"referencia":"16","fecha":"2026-07-22T00:48:31.98191"},
    {"id":37,"tipo":"PREDICCION","monto":-4.00,"saldoResultante":6.00,"referencia":"16","fecha":"2026-07-22T00:47:28.342085"},
    {"id":36,"tipo":"BIENVENIDA","monto":10.00,"saldoResultante":10.00,"referencia":null,"fecha":"2026-07-22T00:47:16.270332"}
  ]
  ```
- **404 Not Found:** `{"mensaje":"El usuario 88888 no tiene una billetera creada."}`

---

### Casos de error genéricos (aplican a cualquier endpoint)

- **Ruta inexistente (404):** `{"mensaje":"No se pudo procesar la solicitud (HTTP 404)."}`
- **Método HTTP no soportado (405):** `{"mensaje":"No se pudo procesar la solicitud (HTTP 405)."}`
- **JSON mal formado o con tipo inválido (400):** ej. mandar `"usuarioId": "abc"` en vez de un número: `{"mensaje":"The request field is required. The JSON value could not be converted to System.Int32. Path: $.usuarioId | LineNumber: 0 | BytePositionInLine: 18."}` — el mensaje en este caso concreto viene en inglés porque lo genera el parser de JSON de .NET, pero mantiene el mismo formato `{ mensaje }` que todo el resto de la API.
- **Error interno no controlado (500)** (ej. la base de datos caída): `{"mensaje":"Ocurrió un error inesperado en el servidor."}` — el detalle técnico real queda solo en los logs del servidor, nunca se expone al cliente.

---

## 6. Contrato de integración con Alexis (backend de Estadísticas)

**Ruta exacta que Alexis debe llamar:**
```
POST /api/utngolcoin/liquidacion
```

**Cuándo:** cada vez que su backend registra el resultado final de un partido del Mundial.

**Payload que debe mandarme (JSON):**
```json
{ "partidoId": 1001, "resultado": "LOCAL" }
```
- `partidoId` (número entero, **obligatorio**): el mismo id de partido que Fer usó al crear las apuestas sobre ese partido.
- `resultado` (texto, **obligatorio**): `LOCAL`, `EMPATE` o `VISITANTE` — el resultado final del partido en sí (no el pronóstico de ningún usuario en particular). No distingue mayúsculas/minúsculas.
- Según el documento de Alexis, puede incluir también `fase` y `grupo`. **Estos campos no están declarados en mi DTO de request, así que se ignoran automáticamente sin romper la petición** — verificado en vivo mandando `{"partidoId":9951,"resultado":"local","fase":"grupos","grupo":"A"}` y confirmando respuesta `200 OK` normal.

**Qué le devuelvo (JSON, `200 OK`):**
```json
{ "partidoId": 1001, "liquidadas": 3, "ganadas": 1, "perdidas": 2, "totalPagado": 8.00 }
```
- `liquidadas`: cuántas predicciones que estaban `PENDIENTE` para ese partido se procesaron en esta llamada.
- `ganadas` / `perdidas`: cuántas de esas pasaron a `GANADA` o `PERDIDA`.
- `totalPagado`: suma de todos los premios pagados (`monto × cuota`) en esta llamada.

**Si no hay nada que liquidar** (nadie apostó a ese partido, o ya se había liquidado antes): **no es un error**, respondo `200 OK` con `liquidadas: 0`.

**Único código de error posible:** `400 Bad Request` si `partidoId` es inválido o `resultado` no es uno de los tres valores esperados.

**Nota de idempotencia para los reintentos de Alexis:** si su sistema tiene reintentos configurados y me llama dos (o más) veces para el mismo `partidoId`, **no hay ningún riesgo de pago doble**. Solo proceso predicciones en estado `PENDIENTE`; en cuanto una queda `GANADA` o `PERDIDA`, las llamadas siguientes para ese mismo partido simplemente no la vuelven a encontrar y no hacen nada con ella. Esto está probado en vivo (ver sección 4, regla 7).

---

## 7. Guía para los frontends (Fer y Dayana)

### Endpoints que probablemente necesite el frontend público (Fer)
- `POST /api/billeteras` — cuando un usuario nuevo se registra en el sistema (en el sistema de Alexis) y hay que darle su billetera con el bono de bienvenida.
- `GET /api/billeteras/{usuarioId}` — mostrar el saldo actual.
- `POST /api/predicciones` — cuando el usuario hace una apuesta.
- `GET /api/predicciones/usuario/{usuarioId}` — mostrar "mis apuestas" con su estado (PENDIENTE/GANADA/PERDIDA).
- `GET /api/transacciones/usuario/{usuarioId}` — mostrar el historial/extracto de movimientos de la billetera.
- `GET /api/ranking` — mostrar la tabla de posiciones pública.
- `GET /api/bonos/estado/{usuarioId}` — para eventualmente avisarle al usuario si está en bancarrota y qué esperar.

### Endpoints que probablemente necesite el frontend admin (Dayana)
- `GET /api/reportes/monedas-circulacion` — dashboard administrativo.
- `GET /api/reportes/partidos-mas-apostados` — dashboard administrativo.
- `POST /api/utngolcoin/liquidacion` — para disparar manualmente la liquidación de un partido desde un panel admin, en vez de esperar a Alexis (útil también para demos).
- `POST /api/bonos/ejecutar-bono-diario` — si se quiere exponer un botón admin para forzar la ejecución del bono diario en vez de que corra solo (hoy no hay ningún proceso automático/cron que lo dispare solo — hay que llamarlo, sea a mano o desde algún proceso que se agregue después).

### Formato de fechas
Cualquier campo de fecha/hora que reciba este backend (hoy, `fechaInicioPartido`) **debe mandarse en UTC, formato ISO 8601 terminado en "Z"**, por ejemplo: `"2026-08-01T20:00:00Z"`. Si el frontend maneja horas locales de Argentina, hay que convertirlas a UTC antes de mandarlas (restar las horas de diferencia). Esto es importante porque el backend compara esa fecha contra `DateTime.UtcNow` del servidor para el cierre de apuestas por hora (RF17).

### URL base a usar
- **Pruebas locales** (todo corriendo en la misma máquina): `http://localhost:5253` (o `https://localhost:7069`).
- **Integración en red** (Fer o Dayana en otra laptop): la IP de la laptop donde corre este backend en la red local, por ejemplo `http://192.168.1.50:5253`. Ver sección 9 para cómo obtenerla.

### CORS
Ya está habilitado con política abierta (`AllowAnyOrigin`), así que **no hace falta ninguna configuración especial del lado del frontend** para evitar bloqueos de CORS — las llamadas desde cualquier origen (localhost, otra IP, otro puerto) van a funcionar sin problema.

---

## 8. Cómo levantar el proyecto

### Requisitos previos
- .NET SDK 9 o superior instalado.
- MariaDB corriendo, escuchando en `127.0.0.1:3307`, con usuario `root` / password `postgres` (o ajustar la cadena de conexión si es distinto en tu máquina).
- Opcional, solo si hace falta crear migraciones nuevas: herramienta `dotnet-ef` instalada globalmente (`dotnet tool install --global dotnet-ef`).

### Pasos
1. Clonar el repositorio.
2. Revisar la cadena de conexión en `UTNGolCoin.Api/UTNGolCoin.Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "server=127.0.0.1;port=3307;database=utngolcoin;user=root;password=postgres"
   }
   ```
   Ajustar si MariaDB corre con otra IP/puerto/usuario en tu entorno.
3. Restaurar y compilar:
   ```
   cd UTNGolCoin.Api/UTNGolCoin.Api
   dotnet restore
   dotnet build
   ```
4. Crear la base de datos y las tablas (si es la primera vez, o si hay migraciones nuevas sin aplicar):
   ```
   dotnet ef database update
   ```
   Esto lee las migraciones en `Migrations/` y crea (si no existe) la base `utngolcoin` con las 4 tablas descritas en la sección 3.
5. Correr el proyecto:
   ```
   dotnet run
   ```
   O con F5 desde Visual Studio (perfil `https`, configurado para abrir el navegador automáticamente).
6. Abrir la URL del servicio en el navegador (por defecto `http://localhost:5253`). **La página de inicio ya es Swagger** — ahí están los 12 endpoints documentados, con "Try it out" para probarlos sin necesidad de Postman ni escribir código.

---

## 9. Cómo conectarse en red (día de la integración con Fer y Alexis)

1. **Obtener la IP de la laptop donde corre este backend:** abrir una consola de Windows y ejecutar:
   ```
   ipconfig
   ```
   Buscar la "Dirección IPv4" del adaptador conectado al router (ej. `192.168.1.50`). Esa es la IP que Fer y Alexis van a usar para llamar a este servicio (no `localhost`, porque `localhost` solo significa "esta misma máquina").
2. **Dónde poner esa IP:** cada quien la usa del lado suyo (en la configuración de su propio cliente HTTP / URL base de la API que consumen). Del lado de este backend, la única IP que hay que configurar es la de Alexis, en `appsettings.json`, sección `EstadisticasApi:BaseUrl` — hoy es solo un placeholder sin uso real en código (ver sección 2.3), pero es el lugar donde iría el día que se necesite.
3. **Firewall de Windows:** la primera vez que el proyecto corre y escucha en red, Windows puede preguntar si permite el acceso a través del Firewall — hay que aceptarlo (alcanza con redes privadas) para que otras laptops puedan conectarse. Si se bloqueó sin querer, se puede habilitar manualmente en "Firewall de Windows Defender" → "Permitir una aplicación a través del firewall", buscando `dotnet.exe` o el ejecutable publicado del proyecto.
4. **Cómo llegan Fer y Alexis:** ambos apuntan sus clientes HTTP a `http://<IP-de-esta-laptop>:5253/api/...` (o el puerto HTTPS si corresponde). Como Kestrel escucha en `0.0.0.0` y CORS está abierto, no hay pasos adicionales de configuración de este lado.

---

## 10. Historial de construcción (resumen honesto por sesión)

El proyecto se construyó incrementalmente, un commit por sesión de trabajo:

1. **Andamiaje** - estructura de carpetas por capas, Swagger como página de inicio, Kestrel en `0.0.0.0`, CORS abierto, endpoint de salud.
2. **Base de datos** - instalación de EF Core + Pomelo, las 4 entidades, el `DbContext`, primera migración aplicada contra MariaDB.
3. **Billetera y bono de bienvenida** - creación de billetera con 10 monedas iniciales.
4. **Crear apuestas** - validaciones de saldo, billetera y apuesta duplicada; cuotas fijas.
5. **Cierre de apuestas por hora (RF17)** - campo `FechaInicioPartido` agregado, validación de cierre.
6. **Liquidación de premios (RF12/RF19)** - webhook `POST /api/utngolcoin/liquidacion`, pago de premios, idempotencia.
7. **Bono antibancarrota (RF20)** - bono diario de 1 moneda a saldo <= 0, con simulación de fecha para la demo.
8. **Ranking y consulta de apuestas (RF21/RF22)** - tabla de clasificación pública.
9. **Reportes básicos (RF27)** - monedas en circulación y partidos más apostados.
10. **Cierre y documentación** - verificación de degradación controlada (RNF05), unificación de mensajes de error (RNF10), documentación Swagger con comentarios XML (RNF09), reescritura de `ESTADO_PROYECTO.md` como informe maestro.
11. **Verificación posterior - RF14** - se detectó que el historial de transacciones no tenía un endpoint que lo expusiera (solo se insertaba, nunca se consultaba). Se agregó `GET /api/transacciones/usuario/{usuarioId}`.

Este documento (`INFORME_MAESTRO_UTNGOLCOIN.md`) es una sesión adicional posterior al cierre formal, escrita para dejar un informe único, exhaustivo y autosuficiente pensado específicamente para que Fer y Dayana (y sus asistentes de IA) puedan construir sus frontends sin depender de más contexto.

---

## 11. Checklist de requisitos cubiertos

| Requisito | Estado | Verificado en |
|---|---|---|
| RF12 - Liquidación de apuestas al terminar el partido | ✅ Cubierto | `POST /api/utngolcoin/liquidacion`, código de `LiquidacionService`, probado en vivo |
| RF14 - Historial de transacciones del usuario | ✅ Cubierto | `GET /api/transacciones/usuario/{usuarioId}`, código de `TransaccionService`, probado en vivo |
| RF17 - No apostar a partido ya iniciado | ✅ Cubierto | Validación en `PrediccionService`, probado en vivo (fecha pasada → 400) |
| RF19 - Webhook de resultado desde Estadísticas | ✅ Cubierto | `POST /api/utngolcoin/liquidacion`, ruta exacta verificada |
| RF20 - Bono antibancarrota | ✅ Cubierto | `POST /api/bonos/ejecutar-bono-diario`, probado en vivo ciclo completo |
| RF21 - Ranking de usuarios | ✅ Cubierto | `GET /api/ranking`, orden verificado en código y en vivo |
| RF22 - Consulta de predicciones y su estado | ✅ Cubierto | `GET /api/predicciones/usuario/{usuarioId}` |
| RF27 - Reportes básicos | ✅ Cubierto | `GET /api/reportes/*`, probado en vivo |
| RNF05 - Degradación controlada | ✅ Cubierto | Sin `HttpClient` en todo el código (verificado con búsqueda exhaustiva) |
| RNF09 - Documentación Swagger | ✅ Cubierto | Comentarios XML en los 12 endpoints, generados en `swagger.json` real |
| RNF10 - Mensajes de error claros y consistentes | ✅ Cubierto | Formato `{ mensaje }` unificado, incluidos errores automáticos del framework, probado en vivo |

**Sobre RF01, RF13, RF15, RF16, RF18 (y cualquier otro RF no mencionado acá):** en ningún momento de la construcción de este proyecto se compartió el enunciado oficial completo del proyecto integrador. No tengo el texto exacto de esos requisitos, así que no puedo confirmar con certeza si están cubiertos o no. Recomendación honesta: contrastar esta tabla contra el documento oficial del proyecto antes de la entrega final. Dicho esto, funcionalidad que probablemente aplica a algunos de esos números (aunque nunca se les asignó el número explícitamente en esta conversación) sí existe: creación de billetera de usuario con bono inicial, creación de apuestas con validación de saldo, una apuesta por partido, ledger de transacciones.

---

## 12. Mejoras futuras / limitaciones honestas

- **Password de MariaDB en texto plano:** `appsettings.json` tiene la contraseña de la base en texto plano (`password=postgres`). Válido para desarrollo académico local, pero antes de cualquier entorno real habría que moverla a variables de entorno o a `dotnet user-secrets`.
- **Sin autenticación/autorización (JWT pendiente):** ningún endpoint de este backend requiere ningún tipo de token o credencial hoy. Cualquiera que le llegue a la IP puede crear billeteras, apostar a nombre de cualquier `usuarioId`, o disparar liquidaciones. Si el proyecto lo requiere, falta integrar JWT — probablemente emitido por el backend de Alexis, que es el dueño real de los usuarios.
- **`fechaInicioPartido` confiada al frontend:** hoy no hay ninguna verificación cruzada contra el backend de Alexis de que esa fecha sea la real del partido — se confía en lo que manda quien crea la apuesta. El placeholder `EstadisticasApi:BaseUrl` en `appsettings.json` está pensado para el día que se quiera consultar esa hora directamente a la API de Alexis en vez de recibirla de terceros, pero **no hay código que lo use todavía**.
- **Sin proceso automático para el bono diario:** `POST /api/bonos/ejecutar-bono-diario` hay que llamarlo (manualmente o desde algún proceso externo tipo cron/scheduled task); el backend no lo dispara solo cada día.
- **Concurrencia:** las validaciones de "una apuesta por partido" y "saldo suficiente" se chequean en la aplicación antes de escribir, sin un lock ni una transacción a nivel de aislamiento estricto que impida una condición de carrera si dos requests llegan exactamente al mismo tiempo para el mismo usuario. Para el volumen y contexto de este proyecto académico (MariaDB local, sin carga concurrente real) no representa un riesgo práctico. El caso donde sí se protegió a nivel de base de datos (con un índice único) fue el bono antibancarrota, por ser el escenario donde más importaba evitar un pago duplicado.
- **Sin tests automatizados:** toda la verificación de este proyecto se hizo probando manualmente los endpoints contra la base de datos real durante el desarrollo (documentado sesión por sesión). No hay una suite de tests unitarios ni de integración.

---

## Nota de cierre

Este documento se generó **leyendo el código fuente real** de `UTNGolCoin.Api/UTNGolCoin.Api/` (todos los `Controllers/`, `Services/`, `Services/Dtos/`, `Models/`, `Data/AppDbContext.cs`, `Program.cs`, `appsettings.json`, `UTNGolCoin.Api.csproj`) y **corriendo la API contra la base de datos MariaDB real** para capturar ejemplos de respuesta literales de los 12 endpoints, incluyendo sus casos de error. Ninguna afirmación de comportamiento en este documento fue inferida sin verificación directa contra el código o contra una prueba real.

**El código fuente del repositorio es, y seguirá siendo, la fuente de verdad final.** Este documento describe el estado del proyecto en el momento en que se escribió; si el código cambia después, hay que volver a verificar contra él en vez de asumir que este documento sigue siendo exacto para siempre.
