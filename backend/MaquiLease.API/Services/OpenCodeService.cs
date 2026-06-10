using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MaquiLease.API.Services
{
    public class OpenCodeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenCodeService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _model;

        public OpenCodeService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenCodeService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            var config = configuration.GetSection("OpenCode");
            _apiKey = config["ApiKey"] ?? "";
            _baseUrl = config["BaseUrl"] ?? "https://opencode.ai/zen/go/v1";
            _model = config["Model"] ?? "deepseek-v4-flash";
        }

        public async Task<string> GetClientAuditReportAsync(string clientName, string sector, decimal riskScore, string paymentSummary, string activeContractsSummary)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("OpenCode API Key no configurada. Usando simulador local (modo offline/contingencia).");
                return GetMockAuditReport(clientName, sector, riskScore, paymentSummary, activeContractsSummary);
            }

            try
            {
                var requestUrl = $"{_baseUrl.TrimEnd('/')}/chat/completions";
                var currentDateStr = DateTime.Now.ToString("dd/MM/yyyy");
                
                var systemPrompt = "Eres el Auditor Financiero Inteligente de MaquiLease S.A.C., un sistema de IA experto en análisis de riesgo crediticio para arrendamiento financiero de maquinaria pesada. Tu objetivo es emitir un informe formal, riguroso, comercialmente viable y estructurado en Markdown en español.";
                var userPrompt = $@"Realiza una auditoría crediticia detallada para el siguiente cliente:
- **Razón Social**: {clientName}
- **Sector Económico**: {sector}
- **Risk Score Actual (ML.NET)**: {riskScore}/100
- **Resumen Financiero y Cuotas**: {paymentSummary}
- **Resumen de Contratos**: {activeContractsSummary}
- **Fecha de Auditoría**: {currentDateStr}

Instrucciones Críticas de Precisión y Reglas de Negocio:
1. **Escala y Clasificación del Risk Score**:
   El Risk Score del sistema va de 0 a 100 (donde 0 es riesgo nulo y 100 es riesgo máximo/default). Las categorías oficiales son:
   - Score de 0 a 25: **Bajo**
   - Score de 25.01 a 50: **Medio**
   - Score de 50.01 a 75: **Alto**
   - Score de 75.01 a 100: **Crítico**
   Debes clasificar al cliente estrictamente en la categoría que le corresponde por su score ({riskScore}). Por ejemplo, un score de 26.50 es Riesgo **Medio**, por lo que NO debes decir que se ubica en el percentil de máxima alerta ni llamarlo crítico.
2. **Consistencia de Datos**:
   Básate ÚNICAMENTE en las cifras provistas en los datos anteriores. No inventes cuotas (ej. si el resumen dice 3 cuotas, no hables de 12), ni montos de exposición. Usa exactamente la información dada.
3. **Fecha de Emisión**:
   Escribe la fecha actual real ({currentDateStr}) en el pie del reporte. Nunca uses marcadores de posición literales como '[Fecha Actual]'.
4. **Brevedad y Rendimiento (Velocidad)**:
   Sé sumamente conciso, estructurado y directo. Todo el informe completo no debe exceder las 220 palabras en total. Evita rodeos o explicaciones largas.

Estructura tu informe estrictamente con los siguientes puntos y títulos en Markdown:
1. ### 📊 DICTAMEN DE SOLVENCIA
   (Análisis resumido de solvencia y riesgo: Bajo, Medio, Alto o Crítico en base al Risk Score y su comportamiento de pago real).
2. ### 🔍 ANÁLISIS DE FACTORES DE RIESGO
   (Evaluación de cuotas vencidas y el impacto específico de la coyuntura del sector {sector}).
3. ### 💡 RECOMENDACIONES COMERCIALES
   (Pautas muy breves de tasas de interés, plazos de contratos, garantías y cuota inicial sugeridas para este nivel de riesgo).
4. ### 📝 CLÁUSULA DE CONTRATO RECOMENDADA
   (Cláusula contractual legal y formal a medida, proporcional al riesgo score {riskScore}/100, lista para insertar en el contrato).

Auditoría emitida por sistema IA de MaquiLease S.A.C. – Fecha de emisión: {currentDateStr}";

                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.2
                };

                var jsonPayload = JsonSerializer.Serialize(requestBody);
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Enviando petición a la API de OpenCode Go para el cliente {ClientName}...", clientName);
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error de la API de OpenCode Go ({StatusCode}): {ErrorMsg}. Usando fallback local.", response.StatusCode, errorMsg);
                    return GetMockAuditReport(clientName, sector, riskScore, paymentSummary, activeContractsSummary) + $"\n\n*(Nota: El sistema detectó un inconveniente con el servicio de OpenCode [{response.StatusCode}] y activó el reporte local de contingencia).*";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    if (message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? "Error: Contenido vacío del modelo.";
                    }
                }

                return GetMockAuditReport(clientName, sector, riskScore, paymentSummary, activeContractsSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción ocurrida al llamar a OpenCode Go. Usando fallback local.");
                return GetMockAuditReport(clientName, sector, riskScore, paymentSummary, activeContractsSummary) + $"\n\n*(Nota: Conexión offline detectada. Se activó el reporte local de contingencia).*";
            }
        }

        private string GetMockAuditReport(string clientName, string sector, decimal riskScore, string paymentSummary, string activeContractsSummary)
        {
            var sb = new StringBuilder();
            string category;
            string severity;
            string recommendations;
            string clause;

            if (riskScore > 70)
            {
                category = "CRÍTICO / ALTO RIESGO";
                severity = "se encuentra en una posición extremadamente vulnerable. La morosidad reiterada y el estado actual de las cuotas vencidas comprometen seriamente su solvencia operativa.";
                recommendations = @"- **Cuota Inicial**: Exigir un pago inicial mínimo del 30% del valor total del contrato.
- **Plazos**: Restringir los contratos a un máximo de 6 a 12 meses. Evitar financiamientos a largo plazo.
- **Tasa de Interés**: Aplicar un recargo de +3.5% sobre la tasa base en concepto de prima por riesgo de crédito.
- **Garantías**: Requerir una Fianza Bancaria Solidaria e irrevocable por el 100% de la maquinaria arrendada y el compromiso del representante legal como aval solidario.";
                clause = $@"**CLÁUSULA DÉCIMA: DE LA RESOLUCIÓN ANTICIPADA Y RECUPERACIÓN INMEDIATA POR INCUMPLIMIENTO**
En caso de que el Arrendatario ({clientName}) incurra en mora en el pago de una (01) cuota mensual según el cronograma adjunto, y habiendo transcurrido tres (03) días calendario desde su vencimiento sin que se verifique el abono total, el Arrendador (MaquiLease S.A.C.) queda facultado de pleno derecho para declarar la resolución automática del presente contrato. En tal supuesto, el Arrendatario se obliga a restituir de forma inmediata la maquinaria a favor del Arrendador. Adicionalmente, el Arrendador queda plenamente autorizado para ingresar a los locales u obras de operación del Arrendatario en el sector {sector} a fin de proceder con la desmovilización física y toma de posesión del activo, asumiendo el Arrendatario todos los costos logísticos y legales derivados de la ejecución de esta garantía.";
            }
            else if (riskScore > 40)
            {
                category = "MEDIO / MODERADO";
                severity = "muestra un comportamiento financiero regular, pero con indicios de retrasos puntuales en amortizaciones. Requiere un monitoreo activo de sus cuentas por cobrar.";
                recommendations = @"- **Cuota Inicial**: Sugerir una cuota inicial del 15% al 20%.
- **Plazos**: Plazos recomendados entre 12 y 18 meses.
- **Tasa de Interés**: Aplicar un recargo preventivo de +1.5% sobre la tasa estándar de colocación.
- **Garantías**: Exigir la firma de Pagarés notariales y el endoso de pólizas de seguro multirriesgo del activo a favor de MaquiLease S.A.C.";
                clause = $@"**CLÁUSULA DÉCIMA: DE LAS ALERTAS PREVENTIVAS Y GESTIÓN DE MORA TEMPRANA**
Las partes acuerdan que el Arrendatario ({clientName}) se somete al sistema de monitoreo financiero preventivo de MaquiLease S.A.C. Ante la existencia de una cuota con retraso superior a los cinco (05) días calendario posteriores a su fecha de vencimiento, se suspenderán los servicios de asistencia técnica no programada y el arrendador emitirá una alerta de riesgo moderado. Si el atraso persiste por más de quince (15) días, se aplicará una penalidad de retención temporal sobre el uso de la maquinaria, reservándose el Arrendador el derecho de inmovilización del activo en obras de {sector} hasta la regularización de la deuda.";
            }
            else
            {
                category = "BAJO / PREMIUM";
                severity = "posee un historial impecable de abonos puntuales. Su solvencia crediticia y bajo riesgo sectorial lo catalogan como un cliente altamente confiable para colocación de activos de gran envergadura.";
                recommendations = @"- **Cuota Inicial**: Permitir cuota inicial reducida del 5% o financiamiento al 100% de la primera cuota.
- **Plazos**: Habilitar plazos flexibles y extendidos de hasta 24 o 36 meses.
- **Tasa de Interés**: Otorgar una tasa preferencial (con descuento de hasta -1.5% de la tasa base).
- **Fidelización**: Ofrecer prioridad en la renovación de flota y opciones de compra residual flexibles al finalizar el arrendamiento.";
                clause = $@"**CLÁUSULA DÉCIMA: DE LAS CONDICIONES PREFERENCIALES Y RENOVACIÓN OPERATIVA**
En atención a la excelente calificación crediticia de 'Cliente Premium', el Arrendatario ({clientName}) gozará de prioridad absoluta en la renovación de contratos y la opción de sustitución del activo arrendado por modelos nuevos importados por MaquiLease S.A.C. al cumplimiento de la mitad de la vigencia contractual. Asimismo, en caso de atrasos excepcionales no mayores a diez (10) días calendario, MaquiLease S.A.C. condonará las penalidades moratorias, siempre y cuando se notifique previamente la solicitud de prórroga justificadamente.";
            }

            sb.AppendLine($"# INFORME DE AUDITORÍA IA — {clientName.ToUpper()}");
            sb.AppendLine($"> **Fecha de Emisión**: {DateTime.Now:dd/MM/yyyy HH:mm} | **Auditor**: Inteligencia Artificial MaquiLease");
            sb.AppendLine($"> **Modelo**: `{_model}` (OpenCode AI - Fallback local)");
            sb.AppendLine();
            sb.AppendLine("### 📊 DICTAMEN DE SOLVENCIA");
            sb.AppendLine($"El perfil crediticio del cliente ha sido determinado como **{category}** en base al Risk Score de **{riskScore}/100** calculado por el sistema de Machine Learning local.");
            sb.AppendLine($"El cliente {severity}");
            sb.AppendLine();
            sb.AppendLine("### 🔍 ANÁLISIS DE FACTORES DE RIESGO");
            sb.AppendLine($"*   **Rendimiento Operativo**: {paymentSummary}");
            sb.AppendLine($"*   **Estado de Contratos**: {activeContractsSummary}");
            sb.AppendLine($"*   **Riesgo Sectorial**: El sector de **{sector}** presenta un perfil de comportamiento que influye en el cálculo predictivo del score, modulando la sensibilidad a la mora en situaciones macroeconómicas.");
            sb.AppendLine();
            sb.AppendLine("### 💡 RECOMENDACIONES COMERCIALES");
            sb.AppendLine("Basado en el perfil predictivo, el área comercial debe adoptar las siguientes pautas de contratación:");
            sb.AppendLine(recommendations);
            sb.AppendLine();
            sb.AppendLine("### 📝 CLÁUSULA DE CONTRATO RECOMENDADA");
            sb.AppendLine("Para el wizard de formalización de contratos, se sugiere incorporar de forma obligatoria la siguiente cláusula redactada a medida:");
            sb.AppendLine();
            sb.AppendLine(clause);

            return sb.ToString();
        }

        public async Task<string> GetDraftedTermsAsync(
            string clientName, 
            string sector, 
            decimal riskScore, 
            string assetName, 
            string serviceName, 
            string contractType, 
            decimal totalAmount, 
            decimal initialPayment, 
            int installments)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("OpenCode API Key no configurada. Usando simulador local de cláusulas.");
                return GetMockDraftedTerms(clientName, sector, riskScore, assetName, serviceName, contractType, totalAmount, initialPayment, installments);
            }

            try
            {
                var requestUrl = $"{_baseUrl.TrimEnd('/')}/chat/completions";
                
                var systemPrompt = "Eres un abogado experto en redacción de contratos de leasing y prestación de servicios para maquinaria pesada en el Perú. Escribes cláusulas y términos formales, directos y legalmente viables.";
                
                var objectText = !string.IsNullOrEmpty(assetName) ? $"Arrendamiento de activo: {assetName}" : $"Prestación de servicios: {serviceName}";
                if (contractType == "mixto") objectText = $"Arrendamiento de activo: {assetName} y prestación de servicios técnicos: {serviceName}";

                var userPrompt = $@"Redacta una cláusula contractual formal y personalizada para el siguiente acuerdo de MaquiLease:
- **Cliente (Arrendatario)**: {clientName}
- **Sector Económico**: {sector}
- **Risk Score (Riesgo)**: {riskScore}/100
- **Objeto**: {objectText}
- **Monto Total**: S/. {totalAmount:N2} (Abono Inicial: S/. {initialPayment:N2})
- **Plazo**: {installments} cuotas mensuales.

Instrucciones obligatorias de tono y redacción:
1. **Tono Corporativo y Equilibrado**: Redacta en un tono estrictamente formal, profesional, legal y diplomático aplicable en contratos comerciales peruanos. Evita usar un lenguaje excesivamente punitivo, hostil o agresivo (como 'ingreso forzoso', 'bloqueo sin previo aviso' o amenazas de desmovilización física inmediata sin notificación). El texto debe buscar la seguridad jurídica de MaquiLease pero con un estilo respetuoso y constructivo.
2. **Proporcionalidad del Riesgo**: Modula la severidad de los términos de acuerdo al Risk Score del cliente ({riskScore}/100):
   - **Riesgo Bajo (Score 0-35)**: Redacta cláusulas estándar de buena fe comercial, enfocadas en la puntualidad, con penalidades moderadas e intereses por mora ordinarios (ej. recargo del 1.5% o 2% diario sobre la cuota vencida) y plazos de gracia razonables (ej. 3 a 5 días) para subsanar atrasos puntuales.
   - **Riesgo Medio (Score 36-70)**: Incluye términos preventivos normales, requiriendo notificaciones escritas de atraso antes de cualquier suspensión de servicios técnicos o resolución del acuerdo.
   - **Riesgo Alto/Crítico (Score > 70)**: Añade salvaguardas contractuales más firmes de garantía de pago y recuperación ordenada del activo en caso de incumplimientos de cuotas persistentes, redactadas en un lenguaje jurídico elegante y corporativo.
3. **Brevedad**: Debe ser un texto compacto de un único párrafo o dos apartados específicos (máximo 180 palabras), redactado como una cláusula real de contrato (ej: 'CLÁUSULA DE CONDICIONES OPERATIVAS Y CUMPLIMIENTO...'). No incluyas introducciones, saludos ni notas aclaratorias; devuelve únicamente el texto legal redactado.";

                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.4
                };

                var jsonPayload = JsonSerializer.Serialize(requestBody);
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error en OpenCode Terms Generator ({StatusCode}): {ErrorMsg}. Usando fallback local.", response.StatusCode, errorMsg);
                    return GetMockDraftedTerms(clientName, sector, riskScore, assetName, serviceName, contractType, totalAmount, initialPayment, installments);
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    if (message.TryGetProperty("content", out var content))
                    {
                        return content.GetString()?.Trim() ?? "Error: Contenido vacío del modelo.";
                    }
                }

                return GetMockDraftedTerms(clientName, sector, riskScore, assetName, serviceName, contractType, totalAmount, initialPayment, installments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en OpenCode Terms Generator. Usando fallback local.");
                return GetMockDraftedTerms(clientName, sector, riskScore, assetName, serviceName, contractType, totalAmount, initialPayment, installments);
            }
        }

        private string GetMockDraftedTerms(string clientName, string sector, decimal riskScore, string assetName, string serviceName, string contractType, decimal totalAmount, decimal initialPayment, int installments)
        {
            var targetObject = !string.IsNullOrEmpty(assetName) ? assetName : serviceName;
            var penaltyRate = riskScore > 70 ? "5%" : (riskScore > 40 ? "3%" : "1.5%");
            
            return $"**CLÁUSULA ADICIONAL DE CONDICIONES COMERCIALES Y MITIGACIÓN DE RIESGO**\n" +
                   $"Las partes acuerdan que el presente contrato de {contractType.ToUpper()} tiene por objeto el activo/servicio '{targetObject}' por un valor total de S/. {totalAmount:N2}, con un abono inicial de S/. {initialPayment:N2} y financiamiento en {installments} cuotas mensuales. " +
                   $"En consideración al nivel de riesgo crediticio del Arrendatario ({clientName}, Sector {sector}) evaluado con un Score de {riskScore}/100, se pacta que ante cualquier atraso en el pago de las cuotas mensuales, se devengará de forma automática un interés moratorio diario acumulativo equivalente al {penaltyRate} del monto de la cuota impaga. " +
                   $"Asimismo, el Arrendador se reserva el derecho de retención, suspensión de soporte técnico e inmovilización en obra del activo ante un retraso superior a los cinco (05) días calendario.";
        }

        public async Task<string> GetChatAssistantResponseAsync(List<Models.DTOs.ChatMessageDto> messages, string systemContext)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("OpenCode API Key no configurada. Usando chat local de contingencia.");
                return GetMockChatResponse(messages);
            }

            try
            {
                var requestUrl = $"{_baseUrl.TrimEnd('/')}/chat/completions";
                
                var systemPrompt = $"Eres el Asistente Inteligente de MaquiLease S.A.C., una IA que ayuda a responder consultas sobre contratos, clientes, activos y deudas. " +
                                   $"Tienes acceso en tiempo real a las estadísticas y estado operativo del sistema mediante la siguiente información técnica en JSON de la base de datos:\n" +
                                   $"{systemContext}\n\n" +
                                   $"Puedes guiar al usuario e incluso sugerirle que se dirija a diferentes secciones de la plataforma utilizando enlaces de Markdown normales en tu respuesta. El frontend interceptará estos enlaces y navegará automáticamente mediante el Router de Angular. Guía al usuario usando estos enlaces cuando sea oportuno. Las rutas válidas del sistema son:\n" +
                                   $"- Dashboard Principal: `/dashboard`\n" +
                                   $"- Módulo de IA: `/intelligence` (Risk scores, recomendador de precios, salud de activos y simulador crediticio sandbox)\n" +
                                   $"- Lista de Clientes: `/clients` e ingresar nuevo cliente: `/clients/new`\n" +
                                   $"- Inventario de Activos: `/assets` e ingresar nuevo activo: `/assets/new`\n" +
                                   $"- Catálogo de Servicios: `/services` e ingresar nuevo servicio: `/services/new`\n" +
                                   $"- Lista de Contratos: `/contracts` e iniciar nuevo contrato: `/contracts/new`\n" +
                                   $"- Historial Global de Pagos: `/payments` (Cobros y recibos pdf)\n" +
                                   $"- Panel de Alertas: `/alerts` (Alertas de mora y riesgo)\n" +
                                   $"- Gestión de Usuarios: `/users` (Control de accesos y roles)\n\n" +
                                   $"Ejemplo: 'Puedes ver todos los detalles en la [Lista de Clientes](/clients) o [iniciar la creación de un nuevo contrato](/contracts/new)'.\n\n" +
                                   $"**Redirección Automática**:\n" +
                                   $"Si el usuario te pide explícitamente que lo redirijas o lleves a una sección (ej. \"redirígeme a crear un contrato\", \"llévame a ver las alertas\", \"abre la lista de clientes\", \"ir al módulo de IA\"), DEBES incluir el comando de redirección exacto en el siguiente formato: `[REDIRECT:ruta]` al final de tu mensaje, además de un mensaje cordial confirmando la redirección.\n" +
                                   $"Ejemplo: 'De acuerdo, te estoy redirigiendo a la pantalla de alertas. [REDIRECT:/alerts]'. Las únicas rutas válidas para REDIRECT son las indicadas anteriormente.\n\n" +
                                   $"Responde de manera profesional, concisa (máximo 125 palabras), clara y estructurada en español. " +
                                   $"Utiliza viñetas y formato Markdown. " +
                                   $"Si el usuario pregunta por algo que no está en el contexto técnico proporcionado ni en el historial de chat, explícale cordialmente que no tienes acceso a esos datos específicos en este momento.";

                var requestMessages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                foreach (var msg in messages)
                {
                    requestMessages.Add(new { role = msg.Role, content = msg.Content });
                }

                var requestBody = new
                {
                    model = _model,
                    messages = requestMessages.ToArray(),
                    temperature = 0.5
                };

                var jsonPayload = JsonSerializer.Serialize(requestBody);
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error en OpenCode Chat ({StatusCode}): {ErrorMsg}. Usando fallback local.", response.StatusCode, errorMsg);
                    return GetMockChatResponse(messages) + "\n\n*(Nota: Modo local activado debido a desconexión del servidor).*";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    if (message.TryGetProperty("content", out var content))
                    {
                        return content.GetString()?.Trim() ?? "Error: Contenido vacío del modelo.";
                    }
                }

                return GetMockChatResponse(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en OpenCode Chat. Usando fallback local.");
                return GetMockChatResponse(messages) + "\n\n*(Nota: Conexión local de contingencia).*";
            }
        }

        private string GetMockChatResponse(List<Models.DTOs.ChatMessageDto> messages)
        {
            var lastUserMsg = messages.LastOrDefault(m => m.Role == "user")?.Content?.ToLowerInvariant() ?? "";
            
            if (lastUserMsg.Contains("redir") || lastUserMsg.Contains("lleva") || lastUserMsg.Contains("ir a") || lastUserMsg.Contains("abre") || lastUserMsg.Contains("navega"))
            {
                if (lastUserMsg.Contains("contrato") && (lastUserMsg.Contains("crear") || lastUserMsg.Contains("nuevo") || lastUserMsg.Contains("creación")))
                    return "Perfecto, te estoy redirigiendo a la creación de un nuevo contrato. [REDIRECT:/contracts/new]";
                if (lastUserMsg.Contains("contrato"))
                    return "Entendido, te redirijo al listado de contratos. [REDIRECT:/contracts]";
                if (lastUserMsg.Contains("cliente") && (lastUserMsg.Contains("crear") || lastUserMsg.Contains("nuevo") || lastUserMsg.Contains("registro")))
                    return "Por supuesto, te redirijo al formulario para registrar un nuevo cliente. [REDIRECT:/clients/new]";
                if (lastUserMsg.Contains("cliente"))
                    return "De acuerdo, te redirijo a la lista de clientes. [REDIRECT:/clients]";
                if (lastUserMsg.Contains("activo") || lastUserMsg.Contains("inventario") || lastUserMsg.Contains("maquina") || lastUserMsg.Contains("maquinaria"))
                    return "Entendido, abriendo el catálogo de activos y maquinaria. [REDIRECT:/assets]";
                if (lastUserMsg.Contains("alerta"))
                    return "Abriendo el panel de alertas de riesgo y morosidad. [REDIRECT:/alerts]";
                if (lastUserMsg.Contains("pago") || lastUserMsg.Contains("cobro") || lastUserMsg.Contains("cuota"))
                    return "Redirigiendo al historial de pagos y cobros. [REDIRECT:/payments]";
                if (lastUserMsg.Contains("inteligencia") || lastUserMsg.Contains("ia") || lastUserMsg.Contains("riesgo") || lastUserMsg.Contains("precio"))
                    return "Cargando el módulo de Inteligencia Artificial (score de riesgo, sugerencia de precios). [REDIRECT:/intelligence]";
                if (lastUserMsg.Contains("dashboard") || lastUserMsg.Contains("principal") || lastUserMsg.Contains("inicio"))
                    return "Redirigiendo a la vista del Dashboard Principal. [REDIRECT:/dashboard]";
                if (lastUserMsg.Contains("usuario"))
                    return "Redirigiendo a la configuración y gestión de usuarios. [REDIRECT:/users]";
            }

            if (lastUserMsg.Contains("activo") || lastUserMsg.Contains("maquinaria") || lastUserMsg.Contains("maquina"))
            {
                return "De acuerdo al inventario registrado en el sistema, disponemos de maquinaria para minería y agroindustria. Los activos críticos de minería (como la CAT 320) muestran alertas por desgaste, mientras que la flota agrícola está estable y en su mayoría disponible. ¿Deseas programar algún servicio técnico preventivo?";
            }
            
            if (lastUserMsg.Contains("contrato") || lastUserMsg.Contains("cuota") || lastUserMsg.Contains("mora") || lastUserMsg.Contains("deuda"))
            {
                return "El sistema reporta una tasa de cobro histórica de aproximadamente el 85%. Los clientes del sector construcción son los que presentan mayor mora, acumulando cuotas vencidas y activando alertas preventivas en la mesa de control de riesgos.";
            }

            if (lastUserMsg.Contains("cliente") || lastUserMsg.Contains("ruc") || lastUserMsg.Contains("empresa"))
            {
                return "La cartera comercial clasifica a los clientes en cuatro segmentos: Premium (excelente comportamiento), Crecimiento (recientes), En Riesgo y Problemático. Los clientes Premium tienen acceso a tasas de descuento preferenciales y plazos flexibles de hasta 36 meses.";
            }

            return "Hola. Soy el Asistente IA de MaquiLease. Puedo ayudarte con resúmenes del negocio, auditorías comerciales, estados de maquinaria e inventario, o comportamiento crediticio de la cartera. ¿En qué puedo apoyarte hoy?";
        }
    }
}
