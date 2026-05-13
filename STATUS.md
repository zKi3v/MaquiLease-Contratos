# 📊 Estado del Proyecto MaquiLease (Actualizado 2026-05-13)

Este documento centraliza el avance del sistema y las metas pendientes para la entrega final, alineado con la [Guía Maestra (PLAN.md)](../../Capstone/PLAN.md).

## 🏆 Resumen de Ejes Capstone (UPN)

| Eje | Estado | % Est. | Notas |
| :--- | :--- | :--- | :--- |
| **1. Transaccional** | Avanzado | 85% | Backend completo (CRUDs, Firma, PDF). Frontend pendiente ContractDetail y PaymentsList. |
| **2. BI (Dashboards)** | **Completado** | 100% | Dashboard analítico finalizado con todas las métricas operativas y financieras integradas. |
| **3. Sistema Inteligente** | **Completado** | 100% | Módulo IA activo (Risk Score, Pricing, Forecast, Segmentación). |
| **4. Seguridad / Auth** | **Completado** | 100% | **Firebase Auth** integrado y sincronizado con base de datos local. |

---

## 🕒 Actividad Reciente (Hoy 2026-05-13)
- **Fase C (Dashboard BI)**: Finalización del tablero analítico con 6 visualizaciones (Morosidad, Tipos de Contrato, Segmentación IA, etc.).
- **Fase D (Automatización)**: Implementación de *Background Jobs* en .NET para monitoreo de vencimientos y cálculo de penalidades cada 1 minuto (prueba, para produccion se usara el monitorio cada 24H).
- **Alertas Proactivas**: Sistema de notificaciones en tiempo real con badge dinámico en el *header* y página de gestión `/alerts`.

---

## ✅ Avance Actual (Implementado)

### 🔒 Autenticación & Seguridad (Fase A)
- **Firebase Auth**: Implementado en Frontend y Backend (`Program.cs`).
- **Sincronización de Usuarios**: El `AuthController` vincula identidades de Firebase con la tabla `Users` en SQL Server.
- **Autorización**: JWT Bearer activo; endpoints protegidos y roles definidos (`admin`, `operador`, `gerente`).

### 🧠 Sistema Inteligente (Fase B)
- **Motor Backend**: `IntelligenceService` con algoritmos ponderados para evaluación financiera.
- **Predicción y Análisis**: Cálculo de Risk Score, recomendación dinámica de Precios, Proyección de ingresos (Forecast 3 bandas) y Segmentación.
- **Dashboard Interactivo**: `/intelligence` integrado con PrimeNG Charts, Knobs y validación visual de morosidad (Tooltips informativos).

### 📊 Dashboard BI (Fase C)
- **Visualizaciones Completas**: KPIs generales, Proyección de ingresos, Distribución de Activos, Tasa de morosidad mensual, Contratos por Tipo y Segmentación de Clientes por riesgo.
- **Integración API**: Endpoints conectados para ingesta de datos en tiempo real al acceder al Dashboard principal.

### 🔔 Automatización & Alertas (Fase D)
- **Background Jobs**: Procesos automáticos (`DueDateMonitorJob` y `RiskScoreRecalcJob`) que evalúan moras y riesgos.
- **Centro de Notificaciones**: UI reactiva con polling (cada 60s) en el *header* y un panel dedicado en `/alerts` para gestión de notificaciones.

### ⚙️ Backend & Datos (Fase E parcial)
- **Modo Dual**: Soporte para `InMemory` (desarrollo rápido) y `SQL Server` (producción).
- **Seed Data System**: Generación automática de 9 entidades pobladas para pruebas de integración.
- **QuestPDF**: Generación de recibos de pago en PDF funcional.
- **Lógica de Contratos**: Cálculo automático de cronograma de cuotas e impacto en estado de activos.

### 🎨 UI/UX Premium
- **PrimeNG 17**: Uso de componentes avanzados y Signals de Angular 18.
- **Wizard de Contratos**: Proceso de 3 pasos con firma digital integrada.
- **Dashboard**: Layout responsivo con soporte nativo para Dark/Light mode.

---

## 🚀 Próximamente (Roadmap Pendiente)

### 💰 Fase E: Cierre Transaccional
- **Vista de Detalle de Contrato**: UI para visualizar cronograma de cuotas y estado individual del contrato.
- **Gestión de Pagos**: Funcionalidad en el frontend para registrar el pago de cuotas e integrarlo con la generación de recibos PDF existente.

---

## 🛠️ Guía de Ejecución

| Comando | Acción |
| :--- | :--- |
| `docker-compose up --build -d` | **Modo Test**: Levanta todo con datos de prueba ficticios. |
| `dotnet run` | **Backend**: Inicia la API (puerto 5033). |
| `ng serve` | **Frontend**: Inicia Angular (puerto 4200). |
| `npm run deploy` | **Producción**: Actualiza el frontend en Vercel. |

