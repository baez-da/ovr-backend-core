# OVR Backend Core

Sistema OVR (On-Venue Results) para gestión del ciclo de vida completo de competencias deportivas, basado en el estándar ODF (Olympic Data Feed).

## Stack

- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- MongoDB 8 (driver 3.4)
- MediatR 12.4 (CQRS + pipeline behaviors)
- FluentValidation 11.11
- ErrorOr 2.0 (result pattern)
- Serilog + Seq (logging estructurado)
- xUnit + FluentAssertions + NSubstitute + Testcontainers.MongoDb

## Arquitectura

Monolito modular con DDD. Cada módulo es un bounded context independiente. Vertical Slice Architecture dentro de cada módulo: no hay carpeta `Application/` separada, la capa de aplicación vive en `Features/`.

### Estructura de un módulo

```
OVR.Modules.{Name}/
├── {Name}Module.cs          # DI + registro de endpoints
├── Domain/                  # Agregados, value objects, reglas de negocio
├── Features/                # Capa de aplicación (CQRS por caso de uso)
│   └── {UseCase}/
│       ├── {UseCase}Command.cs o Query.cs   # MediatR request
│       ├── {UseCase}Handler.cs              # Orquestación
│       ├── {UseCase}Validator.cs            # FluentValidation
│       └── {UseCase}Endpoint.cs             # Minimal API
├── Persistence/             # Repository + MongoDB documents + mappings
├── Contracts/               # Interfaces públicas entre módulos
├── Errors/                  # Errores tipados del módulo
└── EventHandlers/           # INotificationHandler para eventos de otros módulos
```

### Módulos (bounded contexts)

| Módulo | Responsabilidad |
|--------|----------------|
| ParticipantRegistry | Identidad universal de personas (atletas, oficiales, coaches) |
| TeamComposition | Agrupación de atletas en equipos por evento |
| CompetitionConfig | Estructura de eventos, fases, RSC, formatos |
| Scheduling | Calendario, sedes, estado de schedule |
| Entries | Inscripciones de participantes en eventos |
| OfficialAssignment | Asignación de oficiales técnicos a unidades |
| CoachAssignment | Asignación de coaches/staff a entries |
| DataEntry | Captura de datos crudos, validación, estado de unidades |
| Progression | Avance entre fases (quién clasifica) |
| Reporting | Generación de PDFs |
| DataDistribution | Distribución de mensajes ODF |
| CommonCodes | Catálogos de datos de referencia ODF: importación Excel, consulta, validación cross-módulo |
| Ingestion | ACL para datos externos (GMS, T&S) con WAL |

### Comunicación entre módulos

- **Domain events** vía MediatR `IPublisher` (pub/sub desacoplado)
- **Contracts** (`IParticipantReader`, etc.) para queries directas entre módulos
- **SharedKernel** para value objects compartidos (RSC, ParticipantId, Gender, Organisation)
- Los eventos de integración están en `SharedKernel/Domain/Events/Integration/`

### Pipeline de MediatR

Request → `LoggingBehavior` → `ValidationBehavior` → Handler

Los validators se ejecutan automáticamente antes del handler. No validar manualmente en handlers lo que FluentValidation ya cubre.

### Niveles de validación

Hay 3 niveles con responsabilidades distintas. No mezclarlos.

| Nivel | Responsable | Pregunta que responde | Ejemplo |
|-------|------------|----------------------|---------|
| **1. Entrada** | `FluentValidation` (Validator del slice) | ¿El request tiene forma válida para intentar el caso de uso? | Campo vacío, formato email, longitud máxima, rango |
| **2. Aplicación** | Handler (consulta repositorios) | ¿El contexto permite ejecutar esta operación? | Recurso no existe, duplicado, carrera cerrada |
| **3. Dominio** | Agregado / Entity (invariantes internas) | ¿Las reglas del negocio lo permiten? | Transición de estado inválida, categoría incompatible con edad, cupo excedido |

- **Nivel 1** retorna `ValidationException` → `400 Bad Request` (automático vía pipeline behavior).
- **Nivel 2 y 3** retornan `ErrorOr<T>` con errores tipados (`Error.NotFound`, `Error.Conflict`, `Error.Validation`) → status code según `ErrorType`.
- Las reglas de dominio (nivel 3) deben vivir **dentro del agregado**, no en el handler. Si el dato puede llegar por API, evento, job o importación, la invariante debe protegerse igual.
- No usar FluentValidation para reglas que requieren consultar estado (BD, otros servicios). Eso es nivel 2 o 3.

## Comandos

```bash
# Infraestructura local
docker compose --profile db up -d          # MongoDB en puerto 27018
docker compose --profile logging up -d     # Seq en http://localhost:8081

# Build
dotnet build

# Tests
dotnet test

# Run
dotnet run --project src/OVR.Api
```

## Convenciones de código

- **Handlers delgados**: La lógica de negocio vive en el dominio (agregados, value objects), no en handlers. El handler solo orquesta.
- **Factory methods**: Crear agregados con `Entity.Create(...)`, nunca con constructores públicos. El constructor sin parámetros es para deserialización de MongoDB.
- **Domain events**: Se levantan dentro del agregado con `RaiseDomainEvent()`. El handler los despacha después de persistir y luego llama `ClearDomainEvents()`.
- **State machines**: Transiciones de estado se validan dentro del agregado (ver `Entry.ChangeStatus()`).
- **ErrorOr**: Usar para retornos de handlers en vez de excepciones para errores de negocio esperados. Excepciones solo para errores inesperados.
- **Central Package Management**: Versiones de paquetes en `Directory.Packages.props`. No especificar versiones en `.csproj` individuales.
- **TreatWarningsAsErrors** está habilitado globalmente.

## i18n y Errores de API

- **Formato de errores**: Todos los errores retornan ProblemDetails RFC 9457 con `errorCode`, `detail` y `errors[]`.
- **Idiomas soportados**: `eng` (default), `spa`, `por`. Detección: header `Language` > `Accept-Language` > default.
- **Archivos de traducción**: Estructura plana `{ "ErrorCode": "mensaje con {{param}}" }` distribuida por módulo:
  - **Globales** (`src/OVR.Api/I18n/{lang}.json`): traducciones compartidas (`Validation.*`, `Fields.*`).
  - **Por módulo** (`src/OVR.Modules.{Name}/I18n/{lang}.json`): errores de negocio del módulo (`{Module}.*`).
- **Agregar traducción de error de negocio**: (1) definir error con `Metadata` en `Errors/{Module}Errors.cs`, (2) agregar key `{Module}.{ErrorName}` en los 3 JSON **del módulo**.
- **Agregar traducción de validación**: agregar key `Validation.{FluentValidationErrorCode}` (ej: `NotEmptyValidator`) en los 3 JSON **globales**.
- **Agregar traducción de campo**: agregar key `Fields.{PropertyName}` en los 3 JSON **globales**.
- **Nuevo módulo con i18n**: crear `I18n/{eng,spa,por}.json` en el módulo y agregar en `.csproj`: `<Content Include="I18n\**" Link="I18n.{Name}\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />`.
- **Endpoints**: Todos pasan `HttpContext` a `.ToApiResult(httpContext)` / `.ToCreatedResult(uri, httpContext)`.

## Referencia

- Documento de arquitectura completo: `consolidated-architecture.md`
- El RSC (Results System Code, 34 chars) es el identificador compartido central entre módulos
- ODF es el published language de salida, no la lógica interna
