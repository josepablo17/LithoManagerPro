# Estado del proyecto LithoManagerPro

> Última revisión: 25 de noviembre de 2026.
> Último commit funcional: `0f43667` (17 ago, base de planilla). Último commit: `f87a66b` (14 oct, ajustes del CI).

Sistema de gestión de recursos humanos: empleados, departamentos, vacaciones y permisos, documentos y planilla (Costa Rica: CCSS, impuesto de renta, aguinaldo).

## Stack

| Capa | Tecnología |
|---|---|
| API | .NET 9 Web API (`LithoManager.Api`) |
| Lógica de aplicación | `LithoManager.Application` |
| Acceso a datos | Dapper con procedimientos almacenados (`LithoManager.Infrastructure`) |
| Base de datos | SQL Server, proyecto `.sqlproj` (6 esquemas, 32 tablas y 74 procedimientos) |
| Frontend | React, Vite, React Router, CSS Modules y Lucide (`LithoManager.Client`) |
| Pruebas | xUnit (`UnitTests`, `IntegrationTests`), Vitest y React Testing Library |
| Infraestructura | Dockerfile, `compose.yaml` y GitHub Actions (`.github/workflows/ci.yml`) |
| Utilidades | `LithoManager.Bootstrap`, que crea el primer SuperAdministrator |

Esquemas de BD: `Security`, `HumanResources`, `LeaveManagement`, `Documents`, `Payroll` y `Audit`.

## Estado por módulo

| Módulo | Backend | Frontend | Notas |
|---|---|---|---|
| Autenticación y sesiones | ✅ | ✅ | JWT, refresh tokens, versión de token, contraseña temporal y recuperación de contraseña por correo |
| Departamentos | ✅ | ✅ | CRUD |
| Empleados | ✅ | ✅ | CRUD completo |
| Solicitudes de vacaciones y permisos | ✅ | ✅ | Crear, aprobar o rechazar y cancelar |
| Saldos de vacaciones | ✅ | ✅ | Mi saldo con desglose; los administradores consultan y ajustan el saldo de cualquier empleado |
| Documentos de empleados | ✅ | ✅ | Almacenamiento local |
| Planilla: configuración | 🟡 | ❌ | Solo lectura (ver abajo) |
| Planilla: operaciones | 🟡 | ❌ | Horarios, asistencia, horas extra e incapacidades |
| Planilla: cálculo | ❌ | ❌ | **Siguiente pieza grande** |
| Administración de usuarios | ❌ | ❌ | `security/users` es una pantalla provisional |
| Formularios | ❌ | ❌ | `forms` es una pantalla provisional |
| Chat interno | ❌ | ❌ | `chat` es una pantalla provisional |
| Dashboard | — | 🟡 | Revisar si muestra datos reales |

Las pantallas provisionales usan `PlaceholderPage` en `LithoManager.Client/src/App.jsx`.

### Planilla: qué existe hoy (`PayrollController`)

Configuración (GET):
- Conceptos de planilla
- CCSS: tipos de cargas sociales, tasas y bases mínimas
- Impuesto de renta: tramos y créditos
- Tipos de turno y reglas de horas extra
- Tipos de incapacidad y reglas de aguinaldo

Operaciones:
- `POST work-schedules`: asignar horario
- `POST attendance-records`: registrar asistencia
- `POST overtime-records`, `PATCH .../response` y `PATCH .../cancel`: registrar, aprobar o rechazar y cancelar horas extra
- `POST employee-disabilities` y sus `PATCH`: registrar y actualizar incapacidades

Falta:
- Períodos de planilla (quincenal o mensual) con sus estados: abierto, calculado y cerrado
- Motor de cálculo: salario bruto, horas extra, rebajo por incapacidades, CCSS obrera y patronal, renta, neto
- Detalle por empleado y por concepto, y las boletas de pago
- Cálculo y pago del aguinaldo
- Pantallas del frontend

## Hoja de ruta sugerida

1. **Estabilizar**
   - [x] Confirmar que la solución compila y las pruebas pasan (25 nov: build sin errores ni advertencias, 167 pruebas unitarias, 58 de integración y 12 de frontend en verde; lint y build del cliente OK)
   - [x] Revisar que el último CI en GitHub esté en verde (14 oct, "workflow v4": success)
   - [x] Atender `npm audit` (25 nov: 10 vulnerabilidades en dependencias de desarrollo como Vite, Vitest y ESLint; corregidas con `npm audit fix`, ahora 0)
   - [ ] Eliminar `LithoManager.Database - Copy.sqlproj` si sobra
   - [ ] Levantar todo con `docker compose` y crear el administrador con Bootstrap
2. **Cerrar lo que ya tiene backend**
   - [x] Pantalla de saldos de vacaciones (25 nov, probada contra la base de datos local)
   - [ ] Restaurar la sesión al recargar: hoy el cliente guarda el token solo en memoria, así que cualquier recarga devuelve al login aunque el backend ya tiene refresh tokens
   - [ ] `AdjustEmployeeLeaveBalance` devuelve el saldo sin los datos de la política (el derecho anual llega en 0); el cliente vuelve a consultar el saldo como solución temporal
   - [ ] HumanResourcesStaff puede ver saldos en la API pero no listar empleados, así que solo ve "Mi saldo"
   - [ ] Los nombres de tipos de permiso y de políticas están en inglés en los seeds ("Vacation", "Costa Rica Vacation Standard")
3. **Planilla** (el módulo más diferenciador para la entrevista)
   - [ ] Tablas y procedimientos de períodos y detalle de planilla
   - [ ] Servicio de cálculo con pruebas unitarias para cada regla: CCSS, tramos de renta y horas extra
   - [ ] Endpoints para calcular, cerrar y consultar la planilla y las boletas
   - [ ] Pantallas de configuración, operaciones y corrida de planilla
4. **Administración de usuarios**: listar, crear, asignar roles y desactivar
5. **Opcionales**: formularios, chat y dashboard con métricas reales

## Historial (ramas y PRs)

| Fecha | PR | Contenido |
|---|---|---|
| 08 ago | #1–#4 | Invalidación de sesiones por versión de token, validación ligera de tokens, pruebas con usuarios inactivos y refactorización de la API de autenticación |
| 09 ago | #5 | Módulo de departamentos |
| 10 ago | #6 | Módulo de empleados |
| 11 ago | #7 | Cliente React y sesiones con refresh seguro |
| 12 ago | #8 | CRUD de departamentos en el cliente |
| 13 ago | #9 | Vacaciones y permisos |
| 14 ago | #10 | Documentos de empleados |
| 16 ago | #11 | Empleados completo |
| 17 ago | #12 | Base y operaciones de planilla |
| 18 sep | #13 | Herramienta Bootstrap del administrador inicial y Docker |
| 14 oct | — | Workflow de CI para backend, frontend y base de datos |

## Puntos para explicar en una entrevista

- **Seguridad:** access token de vida corta más refresh token rotativo; una `TokenVersion` en el usuario invalida todas las sesiones de inmediato; contraseña temporal con cambio obligatorio; el primer administrador se crea con una herramienta aparte en lugar de un usuario sembrado.
- **Arquitectura:** separación en capas Api, Application e Infrastructure; Dapper con procedimientos almacenados para tener control del SQL y mejor rendimiento; base de datos versionada como proyecto `.sqlproj`.
- **Dominio:** reglas reales de Costa Rica (CCSS, tramos de renta, aguinaldo) guardadas como datos configurables y no escritas en el código.
- **Proceso:** una rama y un PR por módulo, pruebas unitarias, de integración y de frontend, CI en GitHub Actions y Docker.
