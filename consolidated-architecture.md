# Sistema de resultados deportivos — Arquitectura consolidada

> Documento de consolidación del modelado de dominio basado en el estándar ODF (Olympic Data Feed), diseñado como un
> monolito modular sin microservicios. Este documento refleja todas las decisiones tomadas hasta el momento e identifica
> áreas pendientes de ampliar.

---

## 1. Visión general

El sistema es un **OVR (On-Venue Results)** que gestiona el ciclo de vida completo de una competencia deportiva: desde
la recepción de participantes hasta la publicación de resultados oficiales y su distribución a consumidores externos.

Se implementa como un **monolito modular** donde cada módulo corresponde a un bounded context de DDD, con fronteras
claras, interfaces públicas bien definidas, y comunicación entre módulos a través de eventos de dominio internos o
interfaces explícitas. Los módulos comparten el mismo proceso y base de datos (MongoDB) pero nunca acceden directamente
a las colecciones de otro módulo.

### Principios de diseño

- **Capturar primero, procesar después, nunca descartar.** Cada capa del sistema garantiza que el dato sobreviva a su
  propio fallo.
- **Separar identidad de asignación.** Un participante existe una vez; sus inscripciones, funciones y resultados son
  relaciones independientes.
- **ODF como published language.** El estándar ODF define el vocabulario de salida, no la lógica de negocio interna.
- **Resiliencia en cada frontera.** Cada punto de comunicación entre módulos o sistemas externos es un punto potencial
  de fallo y se diseña asumiendo que fallará.

---

## 2. Ecosistema externo

El OVR no opera en aislamiento. Se integra con sistemas upstream (que lo alimentan) y downstream (que consumen de él).

### 2.1 Sistemas upstream

**GMS (Games Management System)** — Fuente de verdad pre-competición. Internamente comprende dos subsistemas
relevantes: **ACR** (Accreditation), que gestiona datos personales y acreditaciones, y **SEQ** (Sport Entries &
Qualification), que gestiona inscripciones deportivas a nivel de evento. SEQ es el que captura qué atleta compite en
qué evento, incluyendo datos de inscripción como `Seed` y tipo de clasificación (`QUAL_TYPE`). Relación DDD:
**Conformist + Anti-Corruption Layer**. No podemos negociar su modelo de datos; nuestro ACL traduce sus estructuras a
nuestro dominio interno.

**Timing & Scoring (T&S)** — Puede ser un proveedor externo (Omega, Swiss Timing, Seiko) o una solución propia. Es el
componente central de captura de datos crudos de competición. Incluye:

- Hardware layer: photofinish (cámara A + B), sensores de bloques de salida (sets redundantes), transponders RFID,
  medición láser/EDS, anemómetro, scoring pads de jueces, consola de operador manual, game/shot clock.
- Processing engine: aplica reglas de deporte (truncamiento de tiempos, cálculo de mayorías de jueces, detección de
  salida en falso).
- Base de datos propia con WAL y backup (si el proveedor la implementa — no se puede asumir).
- Relación DDD: **Published Language**. El T&S publica su contrato; nosotros nos adaptamos.

**MIM (Man in the Middle)** — Capa de traducción que se interpone entre el T&S y los consumidores cuando el proveedor
externo no tiene capacidad de alimentar directamente al FOP SCB, FOP TV y CIS. El MIM:

- Traduce los datos del T&S al formato que esperan los consumidores de FOP.
- Re-distribuye simultáneamente hacia FOP SCB (scoreboard en sede), FOP TV (gráficos de televisión en sede) y CIS (
  sistema de comentaristas en sede).
- Traduce al formato que espera el OVR para la ingesta.
- Si el proveedor sí tiene capacidad de FOP, el MIM actúa como passthrough transparente.
- Es el ACL del adapter pattern mencionado en el diseño.

### 2.2 Results concentrator

Capa de persistencia propia que se coloca entre el MIM/T&S y el OVR. Todo dato que entra se persiste en un **WAL
inmutable** antes de que cualquier módulo del OVR lo procese. Garantiza:

- Si el proveedor externo no tiene WAL/backup, el concentrador es nuestra única red de seguridad.
- Si es proveedor propio, es una capa adicional de respaldo.
- Permite replay: si el OVR se cae y reinicia, el concentrador re-emite desde el último checkpoint.
- Permite recálculos: Data Entry puede solicitar re-procesamiento desde los datos crudos originales.
- Deduplicación por idempotency key: `source + timestamp + lane/bib`.

### 2.3 Consumidores FOP (alimentados por MIM o T&S directo)

- **FOP SCB** — Scoreboard físico en la sede (lo que ven los espectadores en gradas).
- **FOP TV** — Gráficos inyectados en señal de televisión (marcador en esquina de pantalla).
- **CIS** — Sistema de información para comentaristas (splits, estadísticas, biografías).

### 2.4 Sistemas downstream del OVR

- **ODR (Olympic Data Repository)** — Hub central que redistribuye mensajes ODF. Relación: **Open Host Service**.
- **TV Graphics** — Consume mensajes ODF en tiempo real. Relación: **Customer/Supplier**.
- **Web / Scoreboards públicos** — Relación: **Published Language** (ODF).
- **Live results (TSLiveResult, etc.)** — Relación: **Customer/Supplier**.
- **GMS (flujo inverso)** — El OVR notifica de vuelta al GMS vía ODR cuando crea participantes locales, registra IRMs, o
  publica resultados. El GMS pasa de director a espectador notificado.

---

## 3. Transfer of control

Concepto crítico de la arquitectura. La "propiedad" de los datos cambia de manos en un momento específico:

**Fase pre-competición: GMS es el dueño.** Gestiona registros, cuotas, acreditaciones. Empuja datos hacia el OVR para
inicializar sistemas.

**Initial Download.** Entre 3 y 5 días antes del inicio de la competición (SS-x), el GMS ejecuta una descarga masiva
hacia el OVR que incluye: todos los participantes acreditados (`DT_PARTIC`), equipos conformados (`DT_PARTIC_TEAMS`) e
inscripciones por evento (`DT_ENTRIES`). Se envían **todos** los atletas — incluyendo cancelados (`CANCEL`) y reservas
(`AP`) — para que la sede tenga datos completos ante posibles sustituciones de última hora. Los participantes llegan
pre-inscritos en los eventos específicos del SEQ, con la excepción de disciplinas donde la selección se hace en la
reunión de capitanes (esquí de fondo, biatlón, gimnasia artística): en esos casos, los atletas llegan inscritos en un
**evento genérico** a nivel disciplina-género (ej. "Biatlón Masculino") y es el operador del OVR quien los mueve al
evento real una vez definido. Estos mensajes se envían múltiples veces antes de la fecha oficial de transferencia para
pruebas, verificación de calidad de datos y traducción de nombres por consumidores externos.

**Punto de transferencia.** Coincide con: cierre oficial de inscripciones, finalización de reunión técnica, pesaje
oficial (deportes por peso), o publicación de primera start list.

**Fase de competición: OVR toma el control.** El OVR es el dueño absoluto de lo que ocurre en el campo de juego. GMS
queda bloqueado para cambios directos. El OVR genera mensajes ODF y los envía al ODR, notificando al GMS y demás
sistemas. **El flujo se invierte:** tras la transferencia, es el OVR quien actualiza electrónicamente al SEQ/GMS para
que los registros centrales reflejen los cambios hechos en sede (nuevas inscripciones, correcciones, bajas).

---

## 4. Bounded contexts del monolito

### 4.1 Participant registry

**Responsabilidad:** Gestionar la identidad universal de todas las personas involucradas en la competencia.

**Agregado raíz:** `Participant`

**Atributos clave:**

- `id` — Identificador universal, inmutable, asignado por el GMS. En contextos semi-profesionales, el OVR puede generar
  IDs locales con prefijo especial (`LOC-001`, `OVR-999`) para evitar colisiones.
- `type` — `athlete` | `team_official` | `tech_official`. Tres tipos desde el diseño, nunca workarounds de "coach
  disfrazado de atleta".
- `/Description` — Bloque fijo de datos biográficos estándar: `GivenName`, `FamilyName`, `Gender`, `BirthDate`,
  `Organisation`, `IFId`.
- `/ExtendedDescription` — Pares clave-valor para datos específicos por disciplina (peso en JUD:
  `Code=WEIGHT, Value=105`; altura en BKB: `Code=HEIGHT, Value=205`; nombre en camiseta en FBL:
  `Code=SHIRT_NAME, Value="G. OCHOA"`). La estructura base nunca cambia; se agregan nuevos `Code` por diccionario de
  deporte.

**Regla fundamental:** Un participante existe una vez y nunca se elimina. Lo que cambia es el estado de sus
inscripciones y asignaciones, no su perfil.

**Mensajes ODF asociados:** `DT_PARTIC`.

### 4.2 Team composition

**Responsabilidad:** Agrupar participantes tipo `athlete` en entidades de equipo.

**Agregado raíz:** `Team`

**Atributos clave:**

- `teamId` — ID propio del equipo (ej. `JUDXTEAM--FRA01`).
- `Composition` — Lista de `participantId` de los miembros.
- `Substitute` — Atributo por miembro (`Y`/`N`) para indicar suplentes.

**Regla de oro ODF:** Un equipo participa en un solo evento; si las mismas personas participan en múltiples eventos, hay
un equipo distinto (con ID distinto) para cada evento. Esto no es una convención arbitraria — el código del evento está
incrustado en el propio `teamId` (formato `DDDGEEEEEEEENOCnn`, ej. `ATHM4X400m--ESP01`), por lo que reutilizar un ID
entre eventos es estructuralmente imposible. El sufijo `nn` (01, 02…) resuelve además el caso de una misma organización
con múltiples equipos en el mismo evento (ej. parejas de Voleibol de Playa). **Un equipo en ODF no representa a un grupo
de personas, sino a un registro de competencia**: cada instancia aísla su propio `EntryStatus`, composición (`Order`,
titulares vs. suplentes), atributos dinámicos (`Seed`, `UNIFORM`) y resultados/estadísticas, de modo que una
descalificación o cambio en un evento nunca contamina otro.

**Relación con Participant Registry:** Consume IDs de participantes existentes. Nunca crea participantes.

**Mensajes ODF asociados:** `DT_PARTIC_TEAMS`.

### 4.3 Competition config

**Responsabilidad:** Definir el "qué" y el "cómo" de la competencia antes de que se compita.

**Value object central:** Código RSC (Results System Code) de 34 caracteres. Define: disciplina (`FBL`) + evento (`M`) +
fase (`GP01`, `QFNL`) + unidad (`000100`). El RSC es un **shared kernel** — se crea y gobierna aquí, pero se usa por
referencia en Data Entry, Progression y Reporting como value object inmutable.

**Elementos que define:**

- Fases (`Phase`) y su secuencia.
- Formatos de competición (`CompetitionFormatType`): round robin, eliminación directa, contrarreloj, etc.
- Tipos de progresión (`ProgressionType`): cómo se avanza entre fases.
- Reglas específicas por disciplina (máscaras de formato de tiempo, reglas de truncamiento, umbrales de viento para
  récords).

**Mensajes ODF asociados:** Parte de `DT_SCHEDULE` (estructura de la competición).

### 4.4 Scheduling

**Responsabilidad:** Gestionar el calendario temporal de la competencia.

**Agregado:** `ScheduleUnit`

**Atributos clave:**

- RSC de la unidad.
- `StartDate` / `EndDate` — Día y hora programada.
- `Venue` / `Location` — Estadio y cancha específica.
- `ScheduleStatus` — `SCHEDULED` → `DELAYED` → `RUNNING` → `FINISHED`.

**Comportamiento:** Emite `DT_SCHEDULE_UPDATE` cuando hay cambios (retrasos por clima, reprogramaciones).

**Mensajes ODF asociados:** `DT_SCHEDULE`, `DT_SCHEDULE_UPDATE`.

### 4.5 Entries

**Responsabilidad:** Inscribir participantes (o equipos) en eventos específicos. Punto de convergencia entre Participant
Registry, Team Composition y Competition Config.

**Agregado raíz:** `Entry`

**Atributos clave:**

- `participantId` (o `teamId`) — Quién se inscribe.
- `eventRSC` — En qué evento.
- `EntryStatus` — `ACTIVE` | `WDR` (Withdrawn) | `DNS` (Did Not Start) | otros códigos por disciplina.

**Regla crítica:** `EntryStatus` opera **por evento, no por persona**. Un mismo `participantId` puede tener `ACTIVE` en
SWM y `WDR` en OWS simultáneamente. Esto resuelve el problema histórico del atleta lesionado que aparecía en start lists
de disciplinas donde ya no competía.

**Materialización del transfer of control:** Antes de la transferencia, el módulo recibe inscripciones del SEQ/GMS vía
ACL como carga inicial (Initial Download). Una vez completada la transferencia, el módulo deja de aceptar cambios del
GMS y solo el operador local puede modificar. Esto incluye: corregir inscripciones existentes, dar de baja atletas
(`WDR`), y reasignar atletas de eventos genéricos a eventos reales (en disciplinas donde la selección se define en
reunión de capitanes). Cada cambio realizado en sede genera un `DT_ENTRIES` actualizado que Data Distribution emite
hacia el exterior, invirtiendo el flujo original GMS → OVR a OVR → GMS/SEQ.

**Mensajes ODF asociados:** `DT_ENTRIES`.

### 4.6 Official assignment

**Responsabilidad:** Asignar oficiales técnicos a unidades específicas con funciones por unidad.

**Value object:** `OfficialAssignment` = `participantId` + `unitRSC` + `function`.

**Diseño clave:** La `function` (del catálogo `CC@DISCIPLINE_FUNCTION`) es un atributo de la **asignación**, no de la
persona. El mismo Orsato puede tener `function=REFEREE` en MEX vs BRA y `function=VAR_ASSISTANT` en ARG vs GER.
Participant Registry solo sabe que es `type=tech_official` (su categoría general).

### 4.7 Coach assignment

**Responsabilidad:** Vincular oficiales de equipo (coaches, médicos) a entries o start lists.

**Value object:** `CoachAssignment` = `participantId` + `entryId`.

**Diseño clave:** Los coaches son participantes con `type=team_official` desde el origen. Las start lists tienen
secciones separadas (competitors, coaches, officials), cada una poblada por participantes del tipo correspondiente. No
se necesita ningún workaround de "coach como atleta con flag".

### 4.8 Data entry

**Responsabilidad:** Captura de datos crudos, validación con reglas de disciplina, gestión del estado de unidades, y
confirmación de resultados.

Este bounded context unifica lo que conceptualmente podrían ser "captura en vivo" y "determinación de resultado" en un
solo contexto cohesivo, porque en la operación real de sede, el flujo de captura-validación-confirmación es continuo y
no hay un handoff real entre dos equipos.

**Funciones:**

- Recibe datos crudos del results concentrator (tiempos, puntos de jueces, acciones manuales del operador). En este
  punto aún no se sabe quién ganó ni quién progresó.
- Valida con reglas específicas por deporte (ej: dos ganadores en BOX = imposible, segunda amarilla en FBL =
  roja/expulsión, tiempo de reacción < 100ms = salida en falso).
- Gestiona el estado de todas las unidades en disputa.
- Solo cuando confirma un resultado, este queda disponible para distribuirse.
- Puede relanzar recálculos de resultados consumiendo datos del results concentrator.

**Entidades/value objects clave:**

- `UnitResult` — Resultado de una unidad de competición, identificado por `unitRSC`. Estados: `START_LIST` → `LIVE` →
  `UNOFFICIAL` → `OFFICIAL`. Versionado: cada publicación es una versión inmutable. `OFFICIAL` = sellado; cambios
  posteriores solo por protesta formal (crea nueva versión, no muta).
- `CompetitorResult` — Resultado de un competidor dentro de una unidad. Apunta a la **Entry** (no directamente al
  participante). Incluye: `rank`, `mark`/`time`, `IRM` (DSQ, DNF, DNS, WDR), `WLT` (win/loss/tie), `QualificationMark` (
  Q/q).
- `RecordIndicator` — Códigos fijos: `WR` (World Record), `OR` (Olympic Record), etc.
- `Tie` — Atributo `Tie="Y"` para empates explícitos.

**Por qué `CompetitorResult` apunta a `Entry` y no a `Participant`:** Esta es la decisión de diseño que resuelve los
problemas históricos:

- Entry SWM (`ACTIVE`) → tiene resultado con rank y medalla.
- Entry OWS (`WDR`) → sin resultado, no aparece en start list.
- Entry JUD equipo (`DNS` en composición) → el equipo tiene resultado, el atleta marcado como DNS; la regla de la IF
  decide si recibe medalla.
- Consulta de medallas: busca entries con resultado `OFFICIAL`, no participantes. Las entries retiradas no interfieren.

**Formatos de tiempo y marca (ODF):**

- Carreras cortas (ATH 100m): `m:ss.ff` (centésimas).
- Maratón: `h:mm:ss`.
- Lanzamientos/saltos: `#0.00` (dos decimales).
- Puntos (decatlón): `#000` (entero sin decimales).
- El OVR trunca (no redondea) según regla de la IF, luego formatea según máscara ODF.

**Captura por tipo de disciplina:**

- FBL (manual): operador registra acciones (goles, tarjetas) por radio/observación.
- BOX (semiautomatizado): scoring pads electrónicos de 5 jueces → OVR calcula mayoría.
- ATH (automatizado por hardware): photofinish + sensores → OVR certifica y trunca.

**Mensajes ODF asociados:** `DT_RESULT` (con estados START_LIST, LIVE, UNOFFICIAL, OFFICIAL), `DT_PLAY_BY_PLAY`,
`DT_STATS`.

### 4.9 Progression

**Responsabilidad:** Ejecutar la lógica de avance entre fases de competición.

**Consume:** Resultados confirmados (eventos de dominio de Data Entry con el RSC de la unidad terminada).

**Herramientas por tipo de formato:**

- Fase de grupos (round robin): `DT_POOL_STANDING` — actualiza tabla de posiciones (puntos, diferencia de goles).
- Eliminación directa: `DT_BRACKETS` — actualiza árbol del torneo, empareja ganadores/perdedores.
- Clasificación por marcas (ATH, SWM): Marcas `Q` (clasificación automática por posición) y `q` (clasificación por mejor
  tiempo global). Genera start lists de la siguiente fase asignando carriles según tiempo.

**Relación con RSC:** Usa el RSC del evento de resultado confirmado para ubicar en qué fase y formato está la unidad,
consultando lo que Competition Config definió. No tiene dependencia directa con Data Entry — escucha eventos.

**Mensajes ODF asociados:** `DT_POOL_STANDING`, `DT_BRACKETS`.

### 4.10 Reporting

**Responsabilidad:** Generación de reportes (PDF) con plantillas por disciplina.

**Consume:** Datos de Data Entry (resultados) y Progression (standings, brackets).

**Implementación:** Templates Handlebars por disciplina, generación de PDF con Playwright/Chromium.

**Es un generic subdomain:** No contiene lógica de negocio deportiva; solo presenta lo que otros módulos calcularon.

### 4.11 Data distribution

**Responsabilidad:** Empaquetar datos en mensajes ODF (XML/JSON) y distribuirlos a consumidores downstream.

**Patrones de resiliencia:**

- Outbox pattern + retry con exponential backoff.
- Sequence number por RSC para ordering (consumidores descartan mensajes stale).
- Entrega multi-canal independiente (fallo de TV no bloquea web).
- Full state resync: cualquier consumer desconectado puede pedir estado completo.
- Dead letter queue: mensajes que fallan N veces van a cola de revisión, nunca se pierden.

**Relación DDD:** Implementa **Open Host Service** con **Published Language** (ODF).

---

## 5. Shared kernel: el RSC

El código RSC no "pertenece" exclusivamente a Competition Config. Lo que pertenece a ese contexto es la definición y
estructura del RSC.

Una vez creado, el RSC es un **shared kernel** — un value object inmutable que múltiples bounded contexts comparten por
referencia:

- **Competition Config:** Issuer. Crea y gobierna los RSCs.
- **Data Entry:** Usa el RSC como clave de asociación para vincular resultados a unidades.
- **Progression:** Usa el RSC como clave de consulta para saber qué resultados alimentan qué fase.
- **Reporting:** Usa el RSC como clave de agrupación para generar el PDF correcto.
- **Scheduling:** Usa el RSC para vincular unidades a fechas y venues.

En NestJS: el RSC como value object vive en un módulo `shared/common`. Competition Config exporta la lógica de creación
y validación. Los demás módulos importan solo la clase del value object como tipo.

---

## 6. Resiliencia por capas

### 6.1 Hardware externo (responsabilidad del proveedor)

- Cámaras de photofinish A + B redundantes.
- Sensores de bloques con sets A y B e UPS independiente.
- Firmware con log local que sobrevive a corte de red.
- Red primaria (fibra) + red backup (ethernet) con failover automático.
- Último respaldo: juez con cronómetro manual (FAT).

### 6.2 ACL / Ingesta (primera línea del monolito)

- **Write-ahead log:** Todo dato crudo se persiste ANTES de procesarlo. Append-only, inmutable.
- **Multi-listener:** Listener TCP primario + backup + endpoint manual (fallback humano).
- **Deduplicación:** Idempotency key = `source + timestamp + lane/bib`.
- **Replay capability:** Si el procesamiento falla, el WAL permite re-procesar desde cero.

### 6.3 Data entry (procesamiento resiliente)

- **Outbox pattern:** Evento + estado en misma transacción. Garantiza at-least-once.
- **Event sourcing parcial:** Cada acción = evento inmutable. Reconstruir estado desde log para recálculos.
- **Graceful degradation:** Si falla un subsistema secundario (ej: sensor de viento), registrar sin flag de récord en
  vez de descartar.
- **Manual override:** Operador puede forzar un valor con audit trail completo (quién, cuándo, por qué).

### 6.4 Results (integridad post-confirmación)

- **State machine estricta:** `START_LIST` → `LIVE` → `UNOFFICIAL` → `OFFICIAL`. Transiciones unidireccionales salvo
  protesta.
- **Inmutabilidad post-official:** `OFFICIAL` = sellado. Cambios solo por protest/amendment (nueva versión, no
  mutación).
- **Versionado:** Cada publicación = versión. Nunca sobrescribir, apilar.
- **Snapshot por unidad:** Cada partido/carrera = snapshot aislado restaurable independientemente.

### 6.5 Data distribution (última milla)

- **Outbox + retry** con exponential backoff.
- **Message ordering:** Sequence number por RSC.
- **Multi-channel delivery:** Fallo de uno no bloquea otros.
- **Full state resync:** Consumer reconectado puede pedir estado completo.
- **Dead letter queue:** Mensajes fallidos van a revisión, nunca se pierden.

---

## 7. Problemas históricos resueltos por el modelo

### 7.1 Atleta lesionado en multi-disciplina (SWM/OWS)

**Problema anterior:** Se eliminaba o duplicaba al atleta. Eliminar lo borraba de todo el sistema; duplicar rompía
medallas, rankings y reportes.

**Solución:** El `EntryStatus` opera por evento. Entry SWM = `ACTIVE`, Entry OWS = `WDR`. El perfil en Participant
Registry no se toca. Las start lists consultan entries activas; el atleta lesionado no aparece en OWS automáticamente.

### 7.2 Medallas fantasma en equipo (JUD individual + equipos)

**Problema anterior:** El atleta aparecía como ganador de medalla en equipos aunque no compitió por lesión.

**Solución:** `CompetitorResult` apunta a `Entry`. Dentro de la composición del equipo, el estado del atleta se marca
como `DNS`. La regla de medalla por disciplina (que vive en Data Entry) decide si un miembro DNS recibe medalla o no. El
resultado del evento individual es inmutable e independiente.

### 7.3 Coaches en start lists

**Problema anterior:** Se añadía al coach como "atleta con flag diferenciador". Workaround incómodo que requería
comunicación especial a todos los sistemas.

**Solución:** Participant Registry maneja `type=team_official` desde el diseño. Las start lists tienen secciones
separadas: competitors, coaches, officials. Cada sección se puebla con participantes del tipo correspondiente.

### 7.4 Oficial con múltiples funciones (árbitro/VAR)

**Problema anterior:** La función estaba fija en el perfil del oficial.

**Solución:** La `function` es un atributo del `OfficialAssignment` (la relación participante-unidad), no del
participante. Orsato tiene `function=REFEREE` en una unidad y `function=VAR_ASSISTANT` en otra.

---

## 8. Documentación de referencia

**ODF (público):** Data Dictionaries genéricos y por deporte. Disponibles en repositorios del COI. Define el vocabulario
y formato de los mensajes XML/JSON.

**ORIS (restringido):** Manuales funcionales, técnicos y operativos del COI + IFs + proveedor de cronometraje.
Distribuidos bajo NDA. Contienen la lógica de negocio detallada, flujos de pantalla del operador, bocetos de gráficos
TV, diseños de scoreboards, y formatos de reportes impresos.

**Estrategia de desarrollo sin acceso a ORIS:** Partir desde los *Data Dictionaries* públicos de ODF y leerlos “a la
inversa” para reconstruir la lógica de negocio implícita en las restricciones, estructuras y estados que exigen los
mensajes de salida. La premisa es que, si ODF define con precisión qué puede emitirse, en qué momento y bajo qué
condiciones, entonces también deja entrever el modelo operativo que necesita existir detrás.

A partir de ahí, el diseño no intenta replicar la complejidad institucional de unos Juegos Olímpicos, sino capturar sus
invariantes: trazabilidad, consistencia, auditabilidad, control de estados y rigor operativo. Esa disciplina burocrática
es rígida; el estándar ODF, en cambio, es flexible, robusto y extensible. No impone una única implementación: ofrece un
lenguaje común bien estructurado para expresar resultados, cronogramas, inscripciones, progresiones y estadísticas.

En consecuencia, ODF debe entenderse como un **published language**, no como una cárcel arquitectónica. Su valor está en
que proporciona una gramática suficientemente fuerte como para modelar competiciones de alto nivel, pero también
suficientemente adaptable como para sostener eventos semiprofesionales, regionales o universitarios sin traicionar la
coherencia del dominio.

---

## 9. Áreas pendientes de ampliar

- Flujo detallado de protestas y amendments post-competencia.
- Modelo de medallas y ceremonia de premiación.
- Gestión de sorteos y seedings (cómo se arman los brackets iniciales y se asignan carriles/cabezas de serie).
- Integración con sistemas de anti-doping y control de dopaje.
- Modelo de datos para estadísticas extendidas por disciplina (`DT_STATS`).
- Flujo de suspensión y reanudación de partidos (lluvia, incidentes).
- Escenarios de competencia semi-profesional: creación local de participantes, sincronización retroactiva con GMS,
  callbacks.
- Estrategia de testing y validación de reglas por disciplina.
- Modelo de permisos y roles de operador dentro del OVR.
- Infraestructura técnica: estructura de módulos NestJS, colecciones MongoDB, estrategia de event bus interno.
