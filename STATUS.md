# 📊 Estado del Proyecto MaquiLease (Actualizado 2026-05-07)

Este documento centraliza el avance del sistema y las metas pendientes para la entrega final, alineado con la [Guía Maestra (PLAN.md)](../../Capstone/PLAN.md).

## 🏆 Resumen de Ejes Capstone (UPN)

| Eje | Estado | % Est. | Notas |
| :--- | :--- | :--- | :--- |
| **1. Transaccional** | Avanzado | 85% | Backend completo (CRUDs, Firma, PDF). Frontend pendiente ContractDetail y PaymentsList. |
| **2. BI (Dashboards)** | Parcial | 50% | 3 de 6 visualizaciones activas (KPIs, Distribución, Forecast). |
| **3. Sistema Inteligente** | **Completado** | 100% | Módulo IA activo (Risk Score, Pricing, Forecast, Segmentación). |
| **4. Seguridad / Auth** | **Completado** | 100% | **Firebase Auth** integrado y sincronizado con base de datos local. |

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

### 📊 Fase C: Dashboard BI Completo
- Implementar visualizaciones restantes en el backend (`DashboardController`):
    - Tasa de morosidad (`overdue-rate`).
    - Distribución de tipos de contrato.
    - Segmentación de clientes por valor y riesgo.

### 🔔 Fase D: Automatización & Alertas
- **Background Jobs**: `DueDateMonitorJob` para procesar moras y generar alertas automáticas cada 24h.
- **Sistema de Alertas**: Panel de notificaciones proactivas y badge en el header.

---

## 🛠️ Guía de Ejecución

| Comando | Acción |
| :--- | :--- |
| `docker-compose up --build -d` | **Modo Test**: Levanta todo con datos de prueba ficticios. |
| `dotnet run` | **Backend**: Inicia la API (puerto 5033). |
| `ng serve` | **Frontend**: Inicia Angular (puerto 4200). |
| `npm run deploy` | **Producción**: Actualiza el frontend en Vercel. |

