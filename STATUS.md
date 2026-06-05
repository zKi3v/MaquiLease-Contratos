# 📊 Estado del Proyecto MaquiLease (Actualizado 2026-05-21)

Este documento centraliza el avance del sistema y las metas pendientes para la entrega final, alineado con la [Guía Maestra (PLAN.md)](../../Capstone/PLAN.md).

## 🏆 Resumen de Ejes Capstone (UPN)

| Eje | Estado | % Est. | Notas |
| :--- | :--- | :--- | :--- |
| **1. Transaccional** | **Completado** | 100% | Backend y Frontend completamente funcionales (incluyendo detalle de contrato, wizard de creación, registro de pagos y recibos en PDF). |
| **2. BI (Dashboards)** | **Completado** | 100% | Dashboard analítico finalizado con todas las métricas operativas y financieras integradas. |
| **3. Sistema Inteligente** | **Completado** | 100% | Módulo IA activo (Risk Score, Pricing, Forecast, Segmentación). |
| **4. Seguridad / Auth** | **Completado** | 100% | **Firebase Auth** integrado y sincronizado con base de datos local. |

---

## 🕒 Actividad Reciente (Hoy 2026-06-02)
- **Módulo de Gestión de Usuarios y Roles (Fase F - RBAC)**: Implementación completa de la administración de usuarios del sistema con seguridad basada en roles (RBAC):
  - **Backend**: Adición de endpoints para listar usuarios (`GET /api/auth/users`), actualizar roles (`PUT /api/auth/users/{id}/role`) y alternar el estado de activación (`PUT /api/auth/users/{id}/status`) en `AuthController.cs`, asegurando validaciones a nivel de base de datos para restringir el acceso únicamente a cuentas de tipo `admin`.
  - **Frontend**: Creación del componente `UsersListComponent` en Angular 18 con una interfaz de usuario premium usando PrimeNG (Tablas, Dropdowns y Tags). Integración de protección de rutas mediante guards funcionales, sincronización automática de roles con SQL Server en el servicio `AuthService` y visibilidad dinámica del menú en `SidebarComponent`.
- **Armonización y Unificación de Ancho de Contenedores**: Estandarización de la geometría de maquetación en el *Sistema IA*, *Alertas* e *Historial de Pagos* a un ancho uniforme de `max-width: 1400px; margin: 0 auto; padding: 1.5rem;` para eliminar saltos y desalineaciones horizontales al navegar.
- **Campos de Catálogos Secundarios (AutoComplete Premium)**: Reemplazo de dropdowns redundantes por componentes `p-autoComplete` con activación instantánea al enfocar (`completeOnFocus`) y filtrado case-sensitive en tiempo real, permitiendo escribir directamente tanto para buscar como para crear nuevos registros dinámicos.
- **Perfeccionamiento UI/UX & Suite IA**: Refinamiento dinámico del score de confianza del Matchmaker (ligado proactivamente al riesgo crediticio), cajas de alerta con gradientes de marca y auras neon para cada pestaña inteligente, tooltips contextuales (`pTooltip`) integrados de forma global y transiciones sutiles de levantamiento en tablas y tarjetas del Dashboard.
- **Merge de la Rama Main**: Sincronización de todas las ramas y validación de compilación del frontend y backend sin errores.
- **Fase E (Cierre Transaccional)**: Validación de la **Vista de Detalle de Contrato** (`ContractDetail`) e **Historial de Pagos** (`PaymentsList`) con recibos oficiales generados y descargados mediante QuestPDF.
- **Soporte de Modo Oscuro Completo**: Integración en el panel de notificaciones y la interfaz del Sistema de IA.

---

## ✅ Avance Actual (Implementado)

### 🔒 Autenticación & Seguridad (Fase A)
- **Firebase Auth**: Implementado en Frontend y Backend (`Program.cs`).
- **Sincronización de Usuarios**: El `AuthController` vincula identidades de Firebase con la tabla `Users` en SQL Server.
- **Autorización**: JWT Bearer activo; endpoints protegidos y roles definidos (`admin`, `operador`, `gerente`).

### 🧠 Sistema Inteligente (Fase B)
- **Motor Backend**: `IntelligenceService` con algoritmos ponderados para evaluación financiera (Salud de Activos, Matchmaker y Simulador Crediticio What-If).
- **Predicción y Análisis**: Cálculo de Risk Score, recomendación dinámica de Precios, Proyección de ingresos (Forecast 3 bandas) y Segmentación.
- **Dashboard Interactivo**: `/intelligence` integrado con PrimeNG Charts, Knobs y validación visual de morosidad (Tooltips informativos).

### 📊 Dashboard BI (Fase C)
- **Visualizaciones Completas**: KPIs generales, Proyección de ingresos, Distribución de Activos, Tasa de morosidad mensual, Contratos por Tipo y Segmentación de Clientes por riesgo.
- **Integración API**: Endpoints conectados para ingesta de datos en tiempo real al acceder al Dashboard principal.

### 🔔 Automatización & Alertas (Fase D)
- **Background Jobs**: Procesos automáticos (`DueDateMonitorJob` y `RiskScoreRecalcJob`) que evalúan moras y riesgos.
- **Centro de Notificaciones**: UI reactiva con polling (cada 60s) en el *header* y un panel dedicado en `/alerts` para gestión de notificaciones.

### 💰 Cierre Transaccional & Datos (Fase E)
- **Vista de Detalle de Contrato**: Visualización completa del cronograma de cuotas, estado individual y KPIs de pago por contrato.
- **Registro y Gestión de Pagos**: Funcionalidad frontend para pagar cuotas pendientes/vencidas (parcial o total) con generación y descarga automática de recibo PDF.
- **Historial Global de Pagos**: Panel centralizado `/payments` con estadísticas de recaudación y descarga directa de comprobantes.
- **Modo Dual**: Soporte para `InMemory` (desarrollo rápido) y `SQL Server` (producción).
- **Seed Data System**: Generación automática de 9 entidades pobladas para pruebas de integración.
- **QuestPDF**: Generación de recibos de pago en PDF funcional.
- **Lógica de Contratos**: Cálculo automático de cronograma de cuotas e impacto en estado de activos.

### 🎨 UI/UX Premium & Catálogos Dinámicos
- **Geometría Uniforme**: Alineación horizontal simétrica en todas las secciones avanzadas (`max-width: 1400px`).
- **AutoComplete Premium**: Reemplazo de dropdowns pesados por inputs reactivos con sugerencias activadas en click y filtrado de texto case-sensitive para creación libre de registros en sectores, categorías y marcas de maquinaria.
- **Estética de Alta Gama**: Degradados con auras neon dinámicas, blobs de luz radiales blur de fondo en Dashboard y micro-animaciones hover con sombras fluidas.
- **Firma Digital & Detalle Financiero**: Wizard de contratos con canvas de firma iluminado digitalmente y vista de cuotas vencidas destacadas.
- **PrimeNG 17**: Uso de componentes avanzados y Signals de Angular 18.
- **Dashboard**: Layout responsivo con soporte nativo para Dark/Light mode.

### 👥 Gestión de Usuarios & Roles (Fase F)
- **Administración en Frontend**: Vista `/users` exclusiva para el rol `admin` que lista a todos los usuarios del sistema.
- **Control de Estado de Usuario**: Botones de activación y desactivación de cuentas con confirmación e imposibilidad de auto-desactivación para el administrador actual.
- **Gestión de Roles Dinámica**: Cambio instantáneo de rol de base de datos (`admin`, `operador`, `gerente`) sincronizado con Firebase.
- **Filtro de Menú Dinámico**: El sidebar y los guards ocultan o bloquean páginas avanzadas según el rol del usuario autenticado.

---

## 🚀 Próximamente (Roadmap Pendiente / Futuro)
- Ninguno pendiente para los objetivos Capstone obligatorios. Todo el alcance de las Fases A a E se encuentra al 100% completado y verificado en la rama actual.

---

## 🛠️ Guía de Ejecución

| Comando | Acción |
| :--- | :--- |
| `docker-compose up --build -d` | **Modo Test**: Levanta todo con datos de prueba ficticios. |
| `dotnet run` | **Backend**: Inicia la API (puerto 5033). |
| `ng serve` | **Frontend**: Inicia Angular (puerto 4200). |
| `npm run deploy` | **Producción**: Actualiza el frontend en Vercel. |

