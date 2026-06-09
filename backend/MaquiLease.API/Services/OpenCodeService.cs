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
                
                var systemPrompt = "Eres el Auditor Financiero Inteligente de MaquiLease S.A.C., un sistema de IA experto en análisis de riesgo crediticio para arrendamiento financiero de maquinaria pesada. Tu objetivo es emitir un informe formal, riguroso, comercialmente viable y estructurado en Markdown en español.";
                var userPrompt = $@"Realiza una auditoría crediticia detallada para el siguiente cliente:
- **Razón Social**: {clientName}
- **Sector Económico**: {sector}
- **Risk Score Actual (ML.NET)**: {riskScore}/100
- **Resumen Financiero y Cuotas**: {paymentSummary}
- **Resumen de Contratos**: {activeContractsSummary}

Estructura tu informe estrictamente con los siguientes puntos y títulos en Markdown:
1. ### 📊 DICTAMEN DE SOLVENCIA
   (Análisis del riesgo general: Bajo, Medio, Alto o Crítico en base al Risk Score y su comportamiento de pago).
2. ### 🔍 ANÁLISIS DE FACTORES DE RIESGO
   (Evalúa las cuotas vencidas y cómo influye la coyuntura del sector económico de operación en su capacidad de pago).
3. ### 💡 RECOMENDACIONES COMERCIALES
   (Directrices sobre tasas de interés sugeridas, plazos de contratos recomendados, garantías exigidas y cuota inicial sugerida).
4. ### 📝 CLÁUSULA DE CONTRATO RECOMENDADA
   (Redacta una cláusula contractual legal y formal a medida, orientada a mitigar los riesgos específicos detectados en esta auditoría, lista para insertar en el contrato).";

                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.3
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
    }
}
