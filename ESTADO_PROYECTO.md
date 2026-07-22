# UTNGolCoin API - Estado del proyecto

Documento maestro del backend **UTNGolCoin**, proyecto integrador **UTN GolMundial 2026**. Pensado para que lo lean tanto personas (Fer - frontend público, Dayana - frontend admin) como para que cualquier IA retome el proyecto sin más contexto que este archivo.

## 1. Descripción del servicio

UTNGolCoin es el backend de una moneda virtual y sistema de apuestas para el Mundial 2026. Los usuarios reciben UTNGolCoin (moneda virtual, sin valor real) y pueden apostar 1X2 (LOCAL/EMPATE/VISITANTE) sobre partidos. Cuando un partido termina, se liquidan las apuestas: se paga premio a los que acertaron. Existe además un bono de bienvenida, un bono diario "antibancarrota" para quien se queda sin saldo, un ranking público y reportes administrativos.

Este backend **no gestiona usuarios ni partidos** — esos viven en otro sistema (ver punto 2). Acá solo se gestiona la billetera, las apuestas y la moneda.

## 2. Los tres sistemas del proyecto integrador

- **UTNGolCoin (este backend)** - ASP.NET Core Web API, .NET 9. Moneda virtual y apuestas.
- **Frontend público** (Fer, C#) - consume esta API para que los usuarios vean su saldo, aposten, y vean el ranking.
- **Frontend admin** (Dayana) - consume esta API (sobre todo los reportes) para la parte administrativa.
- **Backend de Estadísticas** (Alexis, Jakarta EE en Linux) - dueño de los usuarios y los partidos. Le manda a este backend el resultado de cada partido llamando al webhook `POST /api/utngolcoin/liquidacion`.

Los `usuarioId` y `partidoId` que maneja este backend son **referencias lógicas** a IDs que existen en el sistema de Alexis; acá no hay tablas de usuarios ni de partidos.

## 3. Stack técnico

- **Framework:** ASP.NET Core Web API, **.NET 9**, con controladores (no minimal APIs).
- **Base de datos:** MariaDB, host `127.0.0.1`, puerto **3307**, usuario `root`, password `postgres`, base `utngolcoin`.
- **ORM:** Entity Framework Core con el proveedor **Pomelo.EntityFrameworkCore.MySql** (v9.0.0) + **Microsoft.EntityFrameworkCore.Design** (v9.0.0, para migraciones).
- **Documentación de API:** Swagger / OpenAPI vía **Swashbuckle.AspNetCore** (v10.2.3), con comentarios XML incluidos.
- **CORS:** política abierta (`AllowAnyOrigin`, `AllowAnyHeader`, `AllowAnyMethod`), pensada para desarrollo/demo en red local.

## 4. Cómo levantar el proyecto

### Requisitos previos
- .NET SDK 9 (o superior) instalado.
- MariaDB corriendo en `127.0.0.1:3307` con usuario `root` / password `postgres` (o ajustar la cadena de conexión si es distinto).
- Herramienta `dotnet-ef` instalada globalmente si se necesita crear/aplicar migraciones nuevas: `dotnet tool install --global dotnet-ef`.

### Pasos
1. Clonar el repositorio.
2. Revisar `UTNGolCoin.Api/UTNGolCoin.Api/appsettings.json`: la cadena de conexión ya está configurada para el entorno de desarrollo:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "server=127.0.0.1;port=3307;database=utngolcoin;user=root;password=postgres"
   }
   ```
   Si MariaDB corre en otra IP/puerto/usuario, ajustar acá.
3. Restaurar paquetes y compilar:
   ```
   cd UTNGolCoin.Api/UTNGolCoin.Api
   dotnet restore
   dotnet build
   ```
4. Crear la base de datos y las tablas (si es la primera vez, o si hay migraciones nuevas sin aplicar):
   ```
   dotnet ef database update
   ```
   Esto crea la base `utngolcoin` (si no existe) y todas las tablas, a partir de las migraciones en `Migrations/`.
5. Correr el proyecto:
   ```
   dotnet run
   ```
   O con F5 desde Visual Studio (perfil `https`, ya configurado para abrir el navegador automáticamente en Swagger).
6. Abrir el navegador en la URL del servicio (por defecto `http://localhost:5253` o `https://localhost:7069`): **la página de inicio ya es Swagger**, ahí están todos los endpoints documentados y se pueden probar con "Try it out".

### Si algo no compila o no conecta
- Verificar que MariaDB esté corriendo y aceptando conexiones en el puerto 3307.
- Si cambia algo en `Models/` (nuevas entidades o campos), hay que crear una migración nueva (`dotnet ef migrations add NombreDeLaMigracion`) y aplicarla (`dotnet ef database update`).

## 5. Integración en red (para que Fer, Dayana y Alexis se conecten desde otras laptops)

- El servicio **escucha en `0.0.0.0`** (todas las interfaces de red), no solo en `localhost`. Esto se configura en la sección `Kestrel` de `appsettings.json`:
  ```json
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://0.0.0.0:5253" },
      "Https": { "Url": "https://0.0.0.0:7069" }
    }
  }
  ```
  Esta configuración tiene prioridad sobre `launchSettings.json`, así que aplica tanto si se corre con `dotnet run` como si se publica y corre el `.exe` directo.
- **Para que otra laptop en la misma red llame a este backend**, necesita la IP de la laptop donde corre UTNGolCoin (no `localhost`). Para obtenerla: abrir una consola en Windows y ejecutar:
  ```
  ipconfig
  ```
  Buscar la "Dirección IPv4" del adaptador de red conectado al router (ej. `192.168.1.50`). Esa es la IP que Fer, Dayana y Alexis deben usar, por ejemplo `http://192.168.1.50:5253/api/...`.
- **Firewall de Windows:** la primera vez que se corre el proyecto y escucha en red, Windows puede preguntar si se permite el acceso a través del Firewall — hay que aceptarlo (redes privadas alcanza) para que otras laptops puedan conectarse. Si ya se bloqueó por error, se puede habilitar manualmente en "Firewall de Windows Defender" → "Permitir una aplicación a través del Firewall" y buscar `dotnet.exe` o el ejecutable del proyecto.
- **CORS** está abierto (`AllowAnyOrigin`) para que el frontend de Fer pueda llamar a la API sin bloqueos del navegador, sin importar desde qué IP/puerto se sirva el frontend.
- **Formato de fechas:** cualquier campo de fecha/hora que reciba este backend (por ejemplo `fechaInicioPartido`) debe mandarse **en UTC, formato ISO 8601 con la "Z" al final** (ej. `"2026-08-01T20:00:00Z"`), no en hora local de Argentina. Si el frontend maneja horas locales, tiene que convertirlas a UTC antes de mandarlas.
- **appsettings.json - sección `EstadisticasApi`:** placeholder para cuando exista integración directa con el backend de Alexis (día que se necesite, por ejemplo, consultar la hora real de un partido en vez de recibirla del frontend):
  ```json
  "EstadisticasApi": { "BaseUrl": "http://IP_DE_ALEXIS:PUERTO" }
  ```
  **Todavía no se usa en el código** (no hay ningún `HttpClient` llamando a esta URL) — ver la sección de Degradación Controlada más abajo.

## 6. Degradación controlada (RNF05) - independencia entre servicios

**Este backend nunca hace llamadas salientes a otro servicio en tiempo de ejecución.** Se verificó (búsqueda en todo el código fuente) que no existe ningún `HttpClient`, `IHttpClientFactory` ni `AddHttpClient` en el proyecto — UTNGolCoin nunca llama al backend de Alexis ni al frontend de Fer, solo expone sus propios endpoints.

Consecuencia práctica: **si el backend de Estadísticas de Alexis se cae, UTNGolCoin sigue funcionando al 100%** para todo lo que no depende de un resultado de partido: crear billeteras, apostar (mientras el partido no haya cerrado por hora), consultar saldo, ranking, reportes, bono antibancarrota. Lo único que queda "en pausa" es la liquidación de las apuestas de los partidos que Alexis todavía no informó — esas predicciones simplemente quedan en `PENDIENTE` hasta que Alexis llame al webhook (o hasta que se dispare manualmente desde Swagger para la demo). No hay ningún timeout, excepción ni caída en cascada: UTNGolCoin no está esperando activamente una respuesta de Alexis en ningún momento, porque es Alexis quien lo llama a él, no al revés.

La sección `EstadisticasApi:BaseUrl` en `appsettings.json` (ver punto 5) es un placeholder para una integración *futura y opcional* (consultar la hora real de un partido). Mientras no se implemente ningún `HttpClient` que la use, el servicio sigue siendo completamente independiente.

## 7. Mensajes de error (RNF10)

Todos los endpoints devuelven errores en el mismo formato JSON:
```json
{ "mensaje": "Descripción legible del problema." }
```
Esto aplica de forma consistente a:
- Errores de validación de negocio (400, 404, 409) que devuelve cada controlador.
- Errores automáticos de ASP.NET Core, como JSON mal formado o tipos inválidos en el body (400) — antes devolvían el `ValidationProblemDetails` por defecto del framework, ahora se normalizaron al mismo formato `{ mensaje }`.
- Rutas inexistentes o métodos no soportados (404/405) — antes devolvían cuerpo vacío, ahora también responden `{ mensaje }`.
- Excepciones no controladas (por ejemplo, que la base de datos esté caída): responden `500` con `{ "mensaje": "Ocurrió un error inesperado en el servidor." }`. El detalle técnico real de la excepción se registra en los logs del servidor (consola), nunca se expone al cliente.

Códigos HTTP usados de forma consistente en todo el proyecto: `200` (consulta u operación exitosa), `201` (recurso creado), `400` (datos inválidos / regla de negocio no cumplida), `404` (recurso no encontrado), `409` (conflicto, ej. algo que ya existía).

## 8. Documentación Swagger (RNF09)

Swagger UI es la página de inicio del servicio (`/`). Cada endpoint tiene un resumen (`summary`) describiendo qué hace y, cuando aplica, notas adicionales sobre las reglas de negocio que aplica y los códigos de respuesta posibles — generado a partir de comentarios XML en el código (`GenerateDocumentationFile` habilitado en el `.csproj`, incluidos en Swagger vía `IncludeXmlComments`). Se puede probar cualquier endpoint directamente desde ahí con "Try it out", sin necesidad de Postman ni de escribir código.

## 9. Reglas de negocio clave

- **Bono de bienvenida:** al crear una billetera, el usuario arranca con **10 UTNGolCoin**, registrados como transacción tipo `BIENVENIDA`.
- **Cuotas fijas** por pronóstico (constante en `Services/PrediccionService.cs`, fácil de cambiar): `LOCAL = 2.0`, `EMPATE = 3.0`, `VISITANTE = 2.5`. No hay cuotas dinámicas.
- **Una apuesta por partido:** un usuario no puede tener más de una predicción para el mismo `partidoId`.
- **Cierre de apuestas por hora (RF17):** no se puede apostar a un partido cuya `fechaInicioPartido` ya pasó (o es exactamente ahora).
- **Liquidación (RF12/RF19):** al recibir el resultado de un partido, se paga `monto × cuota` a las predicciones que acertaron (pasan a `GANADA`, transacción tipo `PREMIO`); las que no acertaron pasan a `PERDIDA` sin pago (el monto ya se había descontado al apostar). Es **idempotente**: solo se procesan predicciones `PENDIENTE`, así que liquidar dos veces el mismo partido no vuelve a pagar.
- **Bono antibancarrota (RF20):** un usuario con saldo `<= 0` recibe **1 UTNGolCoin** por día (transacción tipo `BONO_DIARIO`), como máximo un bono por usuario por día (garantizado por un índice único `usuarioId + fecha` en la tabla `bonosdiarios`).
- **Ranking (RF21):** ordenado por saldo descendente, y como desempate, por cantidad de aciertos (predicciones `GANADA`) descendente.
- **Ledger de transacciones:** la tabla `Transacciones` es de solo inserción — nunca se edita ni se borra ninguna fila, es el historial completo de movimientos de cada billetera.

## 10. Modelo de datos

Las 4 tablas son registros planos: se referencian por Id pero **no usan relaciones maestro-detalle de EF** (sin propiedades de navegación), para mantenerlo simple y fácil de explicar.

### Billeteras
| Campo | Tipo | Descripción |
|---|---|---|
| Id | int (PK) | |
| UsuarioId | int | Referencia lógica al usuario (vive en el backend de Estadísticas de Alexis, no es FK local) |
| Saldo | decimal(18,2) | Saldo actual de UTNGolCoin |
| FechaCreacion | datetime | Cuándo se creó la billetera |

### Transacciones (ledger, solo inserción)
| Campo | Tipo | Descripción |
|---|---|---|
| Id | int (PK) | |
| BilleteraId | int | Billetera a la que pertenece el movimiento |
| Tipo | string | `BIENVENIDA`, `PREDICCION`, `PREMIO`, `BONO_DIARIO` |
| Monto | decimal(18,2) | Puede ser negativo (ej. al apostar) |
| SaldoResultante | decimal(18,2) | Saldo de la billetera después de este movimiento |
| Referencia | string, opcional | Ej. id de la predicción relacionada |
| Fecha | datetime | |

### Predicciones (apuestas)
| Campo | Tipo | Descripción |
|---|---|---|
| Id | int (PK) | |
| UsuarioId | int | Referencia lógica al usuario |
| PartidoId | int | Referencia lógica a un partido (vive en Estadísticas) |
| FechaInicioPartido | datetime | Hora de inicio del partido en UTC, usada para el cierre de apuestas (RF17) y guardada como auditoría |
| Pronostico | string | `LOCAL`, `EMPATE`, `VISITANTE` |
| Monto | decimal(18,2) | Monto apostado |
| Cuota | decimal(9,2) | Cuota fija aplicada al momento de apostar |
| Estado | string | `PENDIENTE`, `GANADA`, `PERDIDA` |
| Fecha | datetime | Cuándo se hizo la apuesta |

### BonosDiarios
| Campo | Tipo | Descripción |
|---|---|---|
| Id | int (PK) | |
| UsuarioId | int | Referencia lógica al usuario |
| Fecha | date (sin hora) | Día en que se otorgó el bono. Índice único (`UsuarioId`, `Fecha`) que impide dar dos bonos el mismo día al mismo usuario |

## 11. Catálogo completo de endpoints

Formato de error en todos los casos: `{ "mensaje": "..." }` (ver sección 7).

---

### `GET /api/health`
Chequeo simple de que el servicio está arriba.
- **200 OK:** `{ "status": "ok", "timestamp": "2026-07-21T22:18:00Z" }`

---

### `POST /api/billeteras`
Crea la billetera de un usuario y le acredita el bono de bienvenida de 10 UTNGolCoin.
- **Body:** `{ "usuarioId": 501 }`
- **201 Created:** `{ "id": 1, "usuarioId": 501, "saldo": 10.00, "fechaCreacion": "2026-07-21T22:43:34Z" }`
- **400:** `usuarioId` falta o es <= 0.
- **409:** el usuario ya tiene billetera.

### `GET /api/billeteras/{usuarioId}`
Consulta saldo y datos de la billetera.
- **200 OK:** `{ "id": 1, "usuarioId": 501, "saldo": 10.00, "fechaCreacion": "2026-07-21T22:43:34Z" }`
- **404:** el usuario no tiene billetera.

---

### `POST /api/predicciones`
Crea una apuesta 1X2 sobre un partido.
- **Body:** `{ "usuarioId": 501, "partidoId": 1001, "fechaInicioPartido": "2026-08-01T20:00:00Z", "pronostico": "LOCAL", "monto": 4 }`
  - `pronostico`: `LOCAL` | `EMPATE` | `VISITANTE` (no distingue mayúsculas/minúsculas).
  - `fechaInicioPartido`: UTC, ISO 8601 con "Z" (ver sección 5).
- **201 Created:** `{ "id": 1, "usuarioId": 501, "partidoId": 1001, "fechaInicioPartido": "2026-08-01T20:00:00Z", "pronostico": "LOCAL", "monto": 4.00, "cuota": 2.00, "estado": "PENDIENTE", "fecha": "2026-07-21T23:10:48Z" }`
- **400:** `usuarioId`/`partidoId` faltan o son <= 0; monto <= 0; pronóstico inválido; partido ya cerrado por hora ("Las apuestas para este partido ya cerraron"); saldo insuficiente.
- **404:** el usuario no tiene billetera.
- **409:** el usuario ya tiene una predicción para ese partido.

### `GET /api/predicciones/usuario/{usuarioId}`
Lista las predicciones de un usuario (más recientes primero).
- **200 OK:** `[{ "id": 1, "usuarioId": 501, "partidoId": 1001, "fechaInicioPartido": "2026-08-01T20:00:00Z", "pronostico": "LOCAL", "monto": 4.00, "cuota": 2.00, "estado": "PENDIENTE", "fecha": "2026-07-21T23:10:48Z" }]` (o `[]` si no apostó todavía).

---

### `POST /api/utngolcoin/liquidacion` - webhook para Alexis
**Ruta exacta acordada con el backend de Estadísticas.** También sirve para disparo manual desde Swagger en la demo.
- **Body que manda Alexis:** `{ "partidoId": 1001, "resultado": "LOCAL" }`
  - `resultado`: `LOCAL` | `EMPATE` | `VISITANTE` (resultado final del partido, no distingue mayúsculas/minúsculas).
  - Puede incluir campos extra (`fase`, `grupo`, etc.) — se ignoran automáticamente, no rompen la petición.
- **200 OK:** `{ "partidoId": 1001, "liquidadas": 3, "ganadas": 1, "perdidas": 2, "totalPagado": 8.00 }`
  - Si no hay predicciones pendientes de ese partido (nadie apostó, o ya se liquidó antes), **no es error**: responde `200` con `liquidadas: 0`.
- **400:** `partidoId` inválido, o `resultado` no es LOCAL/EMPATE/VISITANTE.
- **Idempotente:** solo toca predicciones `PENDIENTE`; llamar dos veces con el mismo `partidoId` no vuelve a pagar.

---

### `GET /api/ranking`
Tabla de clasificación pública, ordenada por saldo descendente y luego por aciertos descendente.
- **Parámetro opcional:** `?top=N`.
- **200 OK:** `[{ "usuarioId": 900, "saldo": 14.00, "aciertos": 1, "totalPredicciones": 1 }, ...]`
- **400:** `top` <= 0.

---

### `POST /api/bonos/ejecutar-bono-diario` - bono antibancarrota (RF20)
Da 1 UTNGolCoin a todos los usuarios con saldo <= 0 que no lo recibieron todavía en la fecha indicada.
- **Body opcional:** `{ "fecha": "2026-07-23" }`. Sin body (o sin `fecha`), usa el día de hoy (UTC).
- **200 OK:** `{ "fecha": "2026-07-22", "cantidadBeneficiados": 1, "beneficiarios": [{ "usuarioId": 970, "saldoNuevo": 1.00 }] }`
- No hay error especial: si nadie califica, responde `200` con `cantidadBeneficiados: 0`.
- **Cómo simular el paso de un día para la demo:** llamar este endpoint con fechas distintas en llamadas sucesivas (ver sección 12 más abajo para el paso a paso completo).

### `GET /api/bonos/estado/{usuarioId}`
Consulta si un usuario está en bancarrota y si ya recibió el bono hoy.
- **200 OK:** `{ "usuarioId": 970, "saldo": 0.00, "enBancarrota": true, "yaRecibioBonoHoy": false, "fecha": "2026-07-22" }`
- **404:** el usuario no tiene billetera.

---

### `GET /api/reportes/monedas-circulacion` (RF27)
- **200 OK:** `{ "totalMonedasEnCirculacion": 20.00, "cantidadBilleteras": 3, "totalPagadoEnPremios": 8.00 }`

### `GET /api/reportes/partidos-mas-apostados` (RF27)
- **Parámetro opcional:** `?top=N`.
- **200 OK:** `[{ "partidoId": 2001, "cantidadPredicciones": 2 }, ...]`
- **400:** `top` <= 0.

## 12. Cómo demostrar el bono antibancarrota en la exposición (paso a paso)

No hace falta ningún endpoint especial para dejar a un usuario en saldo 0 — alcanza con lo que ya existe:
1. `POST /api/predicciones` apostando **todo** el saldo del usuario a cualquier pronóstico.
2. `POST /api/utngolcoin/liquidacion` para ese mismo partido, con el **resultado contrario** al apostado → la apuesta queda `PERDIDA` y el saldo baja a 0.
3. `GET /api/bonos/estado/{usuarioId}` → muestra `enBancarrota: true`.
4. `POST /api/bonos/ejecutar-bono-diario` sin body (usa "hoy") → el usuario recibe su moneda.
5. `GET /api/bonos/estado/{usuarioId}` de nuevo → `enBancarrota: false`, `yaRecibioBonoHoy: true`. Volver a ejecutar el paso 4 el mismo día no paga de nuevo (idempotencia).
6. Para simular "el día siguiente": repetir los pasos 1-2 para volver a dejar al usuario en 0, y llamar `POST /api/bonos/ejecutar-bono-diario` con `{ "fecha": "2026-07-23" }` (la fecha de "mañana") → vuelve a recibir su moneda.

## 13. Progreso por sesión (las 10 sesiones del plan, completas)

### Sesión 1 - Andamiaje
- Estructura de carpetas por capas (`Controllers/`, `Models/`, `Services/`, `Data/`).
- Eliminado el ejemplo WeatherForecast de la plantilla.
- Swagger configurado como página de inicio en `/`.
- Kestrel escuchando en `0.0.0.0` (puertos 5253 http / 7069 https).
- CORS abierto para desarrollo.
- Endpoint de prueba `GET /api/health`.
- *(Ajuste posterior de nombres: `Domain` → `Models`, `Infrastructure` → `Data`, `Application` → `Services`, para que sea más fácil de explicar en la defensa.)*

### Sesión 2 - Base de datos
- Instalados Pomelo.EntityFrameworkCore.MySql (9.0.0) y Microsoft.EntityFrameworkCore.Design (9.0.0).
- Creadas las 4 entidades (`Billetera`, `Transaccion`, `Prediccion`, `BonoDiario`) y el `AppDbContext`.
- Cadena de conexión a MariaDB configurada, con `ServerVersion.AutoDetect`.
- Migración `InicialCreacion` aplicada: base `utngolcoin` y las 4 tablas creadas.

### Sesión 3 - Billetera y bono de bienvenida
- `BilleteraService` + `BilleterasController`: creación de billetera con saldo inicial 10 y transacción `BIENVENIDA`, consulta de saldo.

### Sesión 4 - Apuestas / predicciones
- `PrediccionService` + `PrediccionesController`: validaciones de billetera, monto, saldo y apuesta duplicada; cuotas fijas por pronóstico.

### Sesión 5 - Cierre de apuestas por hora (RF17)
- Campo `FechaInicioPartido` agregado a `Prediccion`. Validación de cierre por hora agregada al flujo de creación de apuestas.
- Placeholder `EstadisticasApi:BaseUrl` agregado en `appsettings.json` para integración futura.

### Sesión 6 - Liquidación de premios (RF12/RF19)
- `LiquidacionService` + `LiquidacionController`: ruta exacta `POST /api/utngolcoin/liquidacion`, pago de premios, idempotencia.

### Sesión 7 - Bono antibancarrota (RF20)
- `BonoDiarioService` + `BonosController`: bono diario de 1 moneda a quien está en saldo <= 0, con simulación de fecha para la demo.

### Sesión 8 - Ranking y consulta de apuestas (RF21/RF22)
- `RankingService` + `RankingController`. Revisión de `GET /api/predicciones/usuario/{usuarioId}` (ya cumplía RF22 sin cambios).

### Sesión 9 - Reportes básicos (RF27)
- `ReporteService` + `ReportesController`: monedas en circulación y partidos con más predicciones.

### Sesión 10 - Cierre y documentación (esta sesión)
- **RNF05 (degradación controlada):** verificado que el backend no depende de Alexis en tiempo de ejecución (ninguna llamada saliente en todo el código); documentado en la sección 6.
- **RNF10 (mensajes de error claros):** unificado el formato `{ mensaje }` para errores de validación automática del framework, rutas/métodos no soportados, y excepciones no controladas (antes tenían formatos inconsistentes o cuerpo vacío).
- **RNF09 (documentación Swagger):** agregados comentarios XML con resumen y códigos de respuesta a todos los endpoints; Swagger ahora los muestra.
- Reescrito este documento (`ESTADO_PROYECTO.md`) como informe maestro completo.
- Actualizado `README.md` con una versión corta y presentable.
- Verificación final: compila sin errores ni advertencias, y se probaron manualmente los 11 endpoints con datos reales contra la base.

## 14. Checklist de requisitos funcionales cubiertos

Requisitos explícitamente identificados y trabajados sesión por sesión, con su implementación probada:

| Requisito | Cubierto | Dónde |
|---|---|---|
| RF12 - Liquidación de apuestas al terminar el partido | ✅ | `POST /api/utngolcoin/liquidacion`, Sesión 6 |
| RF17 - No apostar a partido ya iniciado | ✅ | Validación en `PrediccionService`, Sesión 5 |
| RF19 - Webhook de resultado desde Estadísticas | ✅ | `POST /api/utngolcoin/liquidacion`, Sesión 6 |
| RF20 - Bono antibancarrota | ✅ | `POST /api/bonos/ejecutar-bono-diario`, Sesión 7 |
| RF21 - Ranking de usuarios | ✅ | `GET /api/ranking`, Sesión 8 |
| RF22 - Consulta de predicciones y su estado | ✅ | `GET /api/predicciones/usuario/{usuarioId}`, Sesión 4/8 |
| RF27 - Reportes básicos | ✅ | `GET /api/reportes/*`, Sesión 9 |
| RNF05 - Degradación controlada | ✅ | Sin llamadas salientes a otros servicios, sección 6 |
| RNF09 - Documentación Swagger | ✅ | Comentarios XML en todos los endpoints, sección 8 |
| RNF10 - Mensajes de error claros y consistentes | ✅ | Formato `{ mensaje }` unificado, sección 7 |

**Funcionalidad implementada sin un número de RF explícito durante las sesiones** (probablemente corresponde a RF01 y otros RF del enunciado que no se numeraron en esta conversación): creación de billetera con bono de bienvenida de 10 monedas, creación de apuestas con validación de saldo, cuotas fijas por pronóstico, una apuesta por partido, ledger de transacciones.

**Importante - honestidad sobre esta lista:** en esta conversación nunca se compartió el enunciado oficial completo del proyecto, así que no tengo el texto exacto de RF01, RF13, RF14, RF15, RF16, RF18 (ni de otros RF no mencionados). No puedo confirmar con certeza si están cubiertos o no sin ese documento. Recomiendo contrastar esta tabla contra el enunciado oficial antes de la entrega.

## 15. Mejoras futuras (pendientes, honestas)

- **Password de la base en texto plano:** `appsettings.json` tiene la contraseña de MariaDB en texto plano. Es aceptable para desarrollo académico local, pero antes de cualquier entorno real habría que moverla a variables de entorno o `dotnet user-secrets`.
- **JWT / autenticación:** todavía no hay autenticación ni autorización en ningún endpoint. Si el proyecto lo requiere, pendiente integrar JWT (probablemente emitido por el backend de Alexis, dueño de los usuarios).
- **Integración real con la API de Estadísticas de Alexis:** hoy `fechaInicioPartido` se recibe del frontend "de buena fe". El día que haya integración directa, se podría reforzar consultando la hora real del partido a la API de Alexis (usando `EstadisticasApi:BaseUrl`) en vez de confiar en el dato que manda el frontend.
- **Concurrencia:** las validaciones de "una apuesta por partido" y "saldo suficiente" se chequean en la aplicación antes de escribir; en un escenario de alta concurrencia (dos apuestas simultáneas del mismo usuario) podría haber una carrera. Para este proyecto académico, con MariaDB corriendo local y sin carga real, no representa un riesgo práctico. El bono antibancarrota sí tiene esta protección a nivel de base de datos (índice único), por ser el caso donde más importaba.
