# 📊 Estado del Proyecto MaquiLease

Este documento centraliza el avance del sistema y las metas pendientes para la entrega final.

## ✅ Avance Actual (Implementado)

### 🎨 UI/UX Premium
- **Login**: Pantalla dividida con diseño corporativo moderno.
- **Dashboard**: Layout responsivo con Sidebar inteligente y Topbar con buscador y perfil.
- **Dark/Light Mode**: Soporte nativo para cambio de tema fluido.
- **Responsividad**: Optimizado para smartphones (Sidebar con backdrop y gestures).

### ⚙️ Backend & Datos
- **Arquitectura**: .NET 7 con Entity Framework Core.
- **Seed Data System**: Generación automática de datos ficticios (Clientes, Activos, Contratos) para pruebas.
- **Modo Dual**: Soporte para base de datos In-Memory (Test) y SQL Server (Producción).
- **Despliegue**: Frontend automatizado en Vercel.

### 💼 Lógica de Negocio
- **CRUDs**: Gestión básica de Clientes, Maquinaria, Servicios y Contratos.
- **Flujo de Pago**: Lógica inicial para registro de cuotas y estados.

---

## 🚀 Próximamente (Por Implementar)

### 🧠 Fase 5: Sistema Inteligente (Prioridad)
- **Risk Score**: Algoritmo para predecir morosidad de clientes basado en historial.
- **Pricing Recommendation**: Sugerencia automática de precios para alquileres.
- **Ingresos Proyectados**: Gráficos analíticos con bandas de confianza (ML.NET).

### 🔔 Notificaciones y Automatización (Fase 3)
- **Background Jobs**: Tareas automáticas para monitorear vencimientos cada 24h.
- **Sistema de Alertas**: Notificaciones proactivas en el Dashboard para contratos por vencer.

### 📄 Generación de Reportes
- **PDF Export**: Generación de contratos y comprobantes de pago usando QuestPDF.

---

## 🛠️ Guía de Ejecución

| Comando | Acción |
| :--- | :--- |
| `docker-compose up --build -d` | **Modo Test**: Levanta todo con datos de prueba ficticios. |
| `npm run deploy` | **Producción**: Actualiza el frontend en Vercel. |
| `git push origin main` | **Sincronización**: Actualiza el código en GitHub y dispara los deploys automáticos. |
