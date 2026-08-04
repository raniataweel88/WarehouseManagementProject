using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;

namespace WarehouseUserManagement.Services
{
    /// <summary>
    /// يستخدم Claude API (Vision) لقراءة صورة/PDF فاتورة شراء ورقية واستخراج بياناتها تلقائياً:
    /// اسم المورد، رقم وتاريخ الفاتورة، والأصناف مع الكميات والأسعار. يتطلب مفتاح API صالح
    /// مضبوط بالإعداد Anthropic:ApiKey (يُفضّل عبر User Secrets أو متغير بيئة، وليس appsettings.json).
    /// </summary>
    public class InvoiceScanService : IInvoiceScanService
    {
        private const string Model = "claude-sonnet-5";
        private const int MaxOutputTokens = 4096;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvoiceScanService> _logger;

        public InvoiceScanService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<InvoiceScanService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ScannedInvoiceResult> ScanAsync(byte[] fileBytes, string contentType, List<Product> activeProducts, CancellationToken ct = default)
        {
            var apiKey = _configuration["Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvoiceScanException("لم يتم ضبط مفتاح Anthropic API بعد. راجع الإعداد Anthropic:ApiKey.");

            var isPdf = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
            var base64Data = Convert.ToBase64String(fileBytes);

            var contentBlock = isPdf
                ? new Dictionary<string, object>
                {
                    ["type"] = "document",
                    ["source"] = new Dictionary<string, object> { ["type"] = "base64", ["media_type"] = "application/pdf", ["data"] = base64Data }
                }
                : new Dictionary<string, object>
                {
                    ["type"] = "image",
                    ["source"] = new Dictionary<string, object> { ["type"] = "base64", ["media_type"] = contentType, ["data"] = base64Data }
                };

            var promptText = BuildPrompt();

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = MaxOutputTokens,
                ["system"] = "أنت أداة استخراج بيانات (OCR + Parsing) لفواتير شراء ورقية لمستودع. أرجع JSON صِرف فقط بدون أي شرح أو Markdown أو ```. إذا تعذّرت قراءة حقل بوضوح اجعله null بدل تخمينه.",
                ["messages"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["role"] = "user",
                        ["content"] = new object[]
                        {
                            contentBlock,
                            new Dictionary<string, object> { ["type"] = "text", ["text"] = promptText }
                        }
                    }
                }
            };

            var client = _httpClientFactory.CreateClient("Anthropic");
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "تعذر الاتصال بخدمة Claude API لمسح الفاتورة");
                throw new InvoiceScanException("تعذر الاتصال بخدمة الذكاء الاصطناعي. تحقق من الاتصال بالإنترنت وحاول مجدداً.");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Claude API رجّع خطأ ({Status}): {Body}", response.StatusCode, responseJson);
                var friendly = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "مفتاح Anthropic API غير صحيح أو منتهي."
                    : "تعذّر تحليل الفاتورة، حاول مرة أخرى.";
                throw new InvoiceScanException(friendly);
            }

            var extractedText = ExtractTextFromResponse(responseJson);
            var parsed = ParseModelJson(extractedText);

            MatchProducts(parsed, activeProducts);

            return parsed;
        }

        private static string BuildPrompt()
        {
            return """
                هذه صورة/مستند فاتورة شراء ورقية لمستودع. اقرأ محتواها بدقة وأرجع كائن JSON واحد فقط
                (بدون أي نص إضافي قبله أو بعده) بالشكل التالي بالضبط:

                {
                  "invoiceNumber": "رقم الفاتورة كما هو مكتوب أو null",
                  "invoiceDate": "بصيغة yyyy-MM-dd أو null إذا غير واضح",
                  "supplierName": "اسم المورد/الشركة البائعة أو null",
                  "supplierPhone": "رقم هاتف المورد إن وجد أو null",
                  "supplierAddress": "عنوان المورد إن وجد أو null",
                  "notes": "أي ملاحظات إضافية مكتوبة على الفاتورة أو null",
                  "items": [
                    {
                      "name": "اسم الصنف كما هو مكتوب بالفاتورة",
                      "barcode": "الباركود إن كان مطبوعاً بجانب الصنف أو null",
                      "quantity": 0,
                      "unitPrice": 0
                    }
                  ]
                }

                ملاحظات مهمة:
                - quantity و unitPrice أرقام فقط (بدون رموز عملة أو فواصل آلاف).
                - لا تخترع أصنافاً أو أرقاماً غير موجودة فعلياً بالمستند.
                - إذا كان هناك عمود "الإجمالي" لكل سطر ولم يكن سعر الوحدة مكتوباً صراحة، احسب unitPrice = الإجمالي / الكمية.
                - أرجع مصفوفة items فارغة [] إذا لم تستطع قراءة أي صنف بوضوح.
                """;
        }

        /// <summary>Claude API قد يُرجع أكثر من Content Block نصي؛ نجمعها كلها.</summary>
        private static string ExtractTextFromResponse(string responseJson)
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (!doc.RootElement.TryGetProperty("content", out var contentArray))
                throw new InvoiceScanException("استجابة غير متوقعة من خدمة الذكاء الاصطناعي.");

            var sb = new StringBuilder();
            foreach (var block in contentArray.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "text" &&
                    block.TryGetProperty("text", out var textProp))
                {
                    sb.Append(textProp.GetString());
                }
            }

            var text = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
                throw new InvoiceScanException("لم يتمكن الذكاء الاصطناعي من قراءة أي بيانات من الملف المرفوع.");

            return text;
        }

        private static ScannedInvoiceResult ParseModelJson(string rawText)
        {
            // نزيل أي Markdown fences احتياطاً (```json ... ```) في حال أضافها النموذج رغم التعليمات
            var cleaned = rawText.Trim();
            if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                cleaned = firstNewline >= 0 ? cleaned[(firstNewline + 1)..] : cleaned;
                var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0) cleaned = cleaned[..lastFence];
            }

            RawScanDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<RawScanDto>(cleaned, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                throw new InvoiceScanException("تعذّر فهم استجابة الذكاء الاصطناعي، جرّب صورة أوضح للفاتورة.");
            }

            if (dto is null)
                throw new InvoiceScanException("تعذّر استخراج بيانات من الملف المرفوع.");

            DateTime? invoiceDate = null;
            if (!string.IsNullOrWhiteSpace(dto.InvoiceDate) && DateTime.TryParse(dto.InvoiceDate, out var parsedDate))
                invoiceDate = parsedDate.Date;

            return new ScannedInvoiceResult
            {
                InvoiceNumber = string.IsNullOrWhiteSpace(dto.InvoiceNumber) ? null : dto.InvoiceNumber.Trim(),
                InvoiceDate = invoiceDate,
                SupplierName = string.IsNullOrWhiteSpace(dto.SupplierName) ? null : dto.SupplierName.Trim(),
                SupplierPhone = string.IsNullOrWhiteSpace(dto.SupplierPhone) ? null : dto.SupplierPhone.Trim(),
                SupplierAddress = string.IsNullOrWhiteSpace(dto.SupplierAddress) ? null : dto.SupplierAddress.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Items = (dto.Items ?? new List<RawScanItemDto>())
                    .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                    .Select(i => new ScannedInvoiceItem
                    {
                        RawName = i.Name!.Trim(),
                        Barcode = string.IsNullOrWhiteSpace(i.Barcode) ? null : i.Barcode.Trim(),
                        Quantity = i.Quantity ?? 0,
                        UnitPrice = i.UnitPrice ?? 0
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// يحاول مطابقة كل صنف مستخرج من الفاتورة مع كتالوج الأصناف الفعّال: أولاً بالباركود
        /// (مطابقة تامة)، ثم بالاسم (مطابقة تامة فمطابقة جزئية Contains) كحل احتياطي.
        /// </summary>
        private static void MatchProducts(ScannedInvoiceResult result, List<Product> activeProducts)
        {
            foreach (var item in result.Items)
            {
                Product? match = null;

                if (!string.IsNullOrWhiteSpace(item.Barcode))
                {
                    match = activeProducts.FirstOrDefault(p =>
                        !string.IsNullOrWhiteSpace(p.Barcode) &&
                        p.Barcode.Equals(item.Barcode, StringComparison.OrdinalIgnoreCase));
                }

                match ??= activeProducts.FirstOrDefault(p =>
                    p.ProductName.Equals(item.RawName, StringComparison.OrdinalIgnoreCase));

                match ??= activeProducts.FirstOrDefault(p =>
                    p.ProductName.Contains(item.RawName, StringComparison.OrdinalIgnoreCase) ||
                    item.RawName.Contains(p.ProductName, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                    item.ProductId = match.ProductId;
            }
        }

        private class RawScanDto
        {
            [JsonPropertyName("invoiceNumber")] public string? InvoiceNumber { get; set; }
            [JsonPropertyName("invoiceDate")] public string? InvoiceDate { get; set; }
            [JsonPropertyName("supplierName")] public string? SupplierName { get; set; }
            [JsonPropertyName("supplierPhone")] public string? SupplierPhone { get; set; }
            [JsonPropertyName("supplierAddress")] public string? SupplierAddress { get; set; }
            [JsonPropertyName("notes")] public string? Notes { get; set; }
            [JsonPropertyName("items")] public List<RawScanItemDto>? Items { get; set; }
        }

        private class RawScanItemDto
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("barcode")] public string? Barcode { get; set; }
            [JsonPropertyName("quantity")] public decimal? Quantity { get; set; }
            [JsonPropertyName("unitPrice")] public decimal? UnitPrice { get; set; }
        }
    }
}
