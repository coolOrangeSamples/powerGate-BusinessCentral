using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessCentralPlugin.Helper;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class Api
    {
        private static Api _instance;
        public static Api Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Api();

                return _instance;
            }
        }

        private static readonly string Company;

        static Api()
        {
            Company = Configuration.Company;
        }

        private static HttpClient _httpClient;
        private static HttpClientWithLogging _httpClientWithLogging;

        private static HttpClient GetHttpClient()
        {
            if (_httpClient == null)
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                ServicePointManager.DefaultConnectionLimit = 20;

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMilliseconds(-1)
                };

                _httpClientWithLogging = new HttpClientWithLogging(_httpClient);
            }

            return _httpClient;
        }

        private static HttpRequestMessage CreateRequest(string url, HttpMethod method)
        {
            // Ensure HttpClient is initialized
            GetHttpClient();

            var token = GetToken();
            // Build full URL by combining BaseUrl with the relative path
            var fullUrl = Configuration.BaseUrl + url;
            var request = new HttpRequestMessage(method, fullUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

            return request;
        }

        private async Task<byte[]> GetResourceAsByteArray(string url)
        {
            var token = GetToken();
            var client = GetHttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }

        private static Token GetToken()
        {
            if (Configuration.AuthType == "OAuth")
            {
                return MicrosoftOAuth.GetToken(
                    Configuration.TenantId,
                    Configuration.ClientId,
                    Configuration.ClientSecret);
            }
            else if (Configuration.AuthType == "Basic")
            {
                var authenticationString = $"{Configuration.Username}:{Configuration.Password}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
                return new Token { AccessToken = base64EncodedAuthenticationString, TokenType = "Basic" };
            }
            else { return null; }
        }

        #region Lookups
        // API Company
        public async Task<List<Company>> GetCompanies()
        {
            var request = CreateRequest($"/Company?$select=Name,Id", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<Company>>(request);
            return response.Value;
        }

        // Page 30010: APIV2 - Vendors
        public async Task<List<Vendor>> GetVendors()
        {
            var request = CreateRequest($"/Company('{Company}')/Vendors?$select=id,number,displayName", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<Vendor>>(request);
            return response.Value;
        }

        // Page 30025: APIV2 - Item Categories
        public async Task<List<Lookup>> GetItemCategories()
        {
            var request = CreateRequest($"/Company('{Company}')/ItemCategories?$select=id,code", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<Lookup>>(request);
            return response.Value;
        }

        // Page 30030: APIV2 - Units of Measure
        public async Task<List<Lookup>> GetUnitsOfMeasures()
        {
            var request = CreateRequest($"/Company('{Company}')/UnitsOfMeasures?$select=id,code", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<Lookup>>(request);
            return response.Value;
        }

        // Page 30096: APIV2 - Inventory Post. Group
        public async Task<List<Lookup>> GetInventoryPostingGroups()
        {
            var request = CreateRequest($"/Company('{Company}')/InventoryPostingGroups?$select=id,code", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<Lookup>>(request);
            return response.Value;
        }

        // Page 30079: APIV2 - Gen. Prod. Post. Group
        public async Task<List<Lookup>> GetGeneralProductPostingGroups()
        {
            var request = CreateRequest($"/Company('{Company}')/GeneralProductPostingGroups?$select=id,code", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<Lookup>>(request);
            return response.Value;
        }

        // Page 7500: Item Attributes
        public async Task<List<AttributeDefinition>> GetItemAttributeDefinitions()
        {
            var request = CreateRequest($"/Company('{Company}')/ItemAttributes?$select=ID,Name,Type,Blocked", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<AttributeDefinition>>(request);
            return response.Value;
        }

        // Page 99000798: Routing Links
        public async Task<List<RoutingLink>> GetRoutingLinks()
        {
            var request = CreateRequest($"/Company('{Company}')/RoutingLinks", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<RoutingLink>>(request);
            return response.Value;
        }
        #endregion

        #region Items
        // Page 30008: APIV2 - Items
        public async Task<ItemMin> GetItemMin(string number)
        {
            var request = CreateRequest($"/Company('{Company}')/Items?$filter=number eq '{number}'&$select=id,number", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<ItemMin>>(request);
            return response.Value.FirstOrDefault();
        }

        // Page 30008: APIV2 - Items
        private async Task<ItemMin> GetItemMinWithPicture(string number)
        {
            var request = CreateRequest($"/Company('{Company}')/Items?$filter=number eq '{number}'&$expand=Itemspicture($select=id,pictureContent)&$select=id,number", HttpMethod.Get);

            try
            {
                var response = await _httpClientWithLogging.SendAsync<ODataResponse<ItemMin>>(request);
                return response.Value?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        // Page 30: Item Card
        public async Task<List<ItemCard>> GetItemCards()
        {
            var request = CreateRequest($"/Company('{Company}')/ItemCards?$select=No,Description,Blocked,Base_Unit_of_Measure,Net_Weight,Unit_Price,Inventory,Gen_Prod_Posting_Group,Vendor_No,Replenishment_System", HttpMethod.Get);
            var response = await _httpClientWithLogging.SendAsync<ODataResponse<ItemCard>>(request);
            return response.Value;
        }

        // Page 30: Item Card
        public async Task<ItemCard> GetItemCard(string number)
        {
            var request = CreateRequest($"/Company('{Company}')/ItemCards('{number}')?$select=No,Description,Blocked,Type,Base_Unit_of_Measure,Net_Weight,Unit_Price,Inventory,Gen_Prod_Posting_Group,Vendor_No,Replenishment_System", HttpMethod.Get);

            try
            {
                return await _httpClientWithLogging.SendAsync<ItemCard>(request);
            }
            catch
            {
                return null;
            }
        }

        // Page 30: Item Card
        public async Task<ItemCardMin> GetItemCardMin(string number)
        {
            var request = CreateRequest($"/Company('{Company}')/ItemCards('{number}')?$select=No,Description", HttpMethod.Get);
            return await _httpClientWithLogging.SendAsync<ItemCardMin>(request);
        }

        public async Task<byte[]> GetItemPicture(string number)
        {
            var itemMin = await GetItemMinWithPicture(number);
            if (itemMin?.Itemspicture?.pictureContentodatamediaReadLink != null)
            {
                try
                {
                    return await GetResourceAsByteArray(itemMin.Itemspicture.pictureContentodatamediaReadLink);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }

            return null;
        }

        // Page 30008: APIV2 - Items
        public async Task SetItemPicture(string number, byte[] thumbnail)
        {
            if (thumbnail == null || thumbnail.Length <= 0)
                return;

            var itemMin = await GetItemMinWithPicture(number);

            var request = CreateRequest($"/Company('{Company}')/Items({itemMin.id})/Itemspicture/pictureContent", new HttpMethod("PATCH"));
            request.Headers.Add("If-Match", itemMin.Itemspicture.odataetag);
            request.Content = new ByteArrayContent(thumbnail);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            await _httpClientWithLogging.SendAsync(request);
        }

        // Page 30: Item Card
        public async Task<ItemCard> CreateItemCard(ItemCard itemCard)
        {
            var request = CreateRequest($"/Company('{Company}')/ItemCards", HttpMethod.Post);
            var body = new
            {
                itemCard.No,
                itemCard.Description,
                Blocked = false,
                Type = "Inventory",
                itemCard.Base_Unit_of_Measure,
                itemCard.Net_Weight,
                Inventory_Posting_Group = Configuration.DefaultInventoryPostingGroup,
                Item_Category_Code = Configuration.DefaultItemCategoryCode,
                Gen_Prod_Posting_Group = Configuration.DefaultGeneralProductPostingGroup,
                Replenishment_System = Configuration.DefaultReplenishmentSystem
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<ItemCard>(request);
        }

        // Page 30: Item Card
        public async Task<ItemCard> UpdateItemCard(ItemCard itemCard)
        {
            var itemCardMin = await GetItemCardMin(itemCard.No);

            var request = CreateRequest($"/Company('{Company}')/ItemCards('{itemCard.No}')", new HttpMethod("PATCH"));
            request.Headers.Add("If-Match", itemCardMin.odataetag);
            var body = new
            {
                itemCard.No,
                itemCard.Description,
                itemCard.Base_Unit_of_Measure,
                itemCard.Net_Weight
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<ItemCard>(request);
        }

        // Page 30: Item Card
        public async Task<ItemCard> UpdateItemCardProductionBom(string number)
        {
            var itemCardMin = await GetItemCardMin(number);

            var request = CreateRequest($"/Company('{Company}')/ItemCards('{itemCardMin.No}')", new HttpMethod("PATCH"));
            request.Headers.Add("If-Match", itemCardMin.odataetag);
            var body = new
            {
                Production_BOM_No = number
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<ItemCard>(request);
        }
        #endregion

        #region Links and Attributes (CodeUnits from coolOrange App)
        // CodeUnit 50150: ItemRecordLinks
        public async Task<List<Link>> GetItemLinks(string itemNumber)
        {
            var request = CreateRequest($"/ItemRecordLinks_GetLinks?company={Company}", HttpMethod.Post);
            var body = new { itemNumber };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var httpResponse = await _httpClientWithLogging.SendAsync(request);

                // If item doesn't exist, return empty list instead of throwing
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    return new List<Link>();

                httpResponse.EnsureSuccessStatusCode();

                // Read as bytes first to avoid any stream position issues
                var bytes = await httpResponse.Content.ReadAsByteArrayAsync();

                // Remove UTF-8 BOM if present (EF BB BF)
                var startIndex = 0;
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    startIndex = 3;
                }

                var content = Encoding.UTF8.GetString(bytes, startIndex, bytes.Length - startIndex).Trim();

                try
                {
                    var jsonDoc = JsonDocument.Parse(content);

                    if (jsonDoc.RootElement.TryGetProperty("value", out var valueElement))
                    {
                        var result = valueElement.GetString();
                        if (result == null)
                            return new List<Link>();

                        return JsonSerializer.Deserialize<List<Link>>(result, JsonHelper.DeserializeOptions);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON Parse Error in GetItemLinks: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetItemLinks: {ex.Message}");
                throw;
            }

            return new List<Link>();
        }

        // CodeUnit 50150: ItemRecordLinks
        public async Task<List<Link>> GetItemLinks()
        {
            var request = CreateRequest($"/ItemRecordLinks_GetAllLinks?company={Company}", HttpMethod.Post);

            try
            {
                var httpResponse = await _httpClientWithLogging.SendAsync(request);
                httpResponse.EnsureSuccessStatusCode();

                // Read as bytes first to avoid any stream position issues
                var bytes = await httpResponse.Content.ReadAsByteArrayAsync();

                // Remove UTF-8 BOM if present (EF BB BF)
                var startIndex = 0;
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    startIndex = 3;
                }

                var content = Encoding.UTF8.GetString(bytes, startIndex, bytes.Length - startIndex).Trim();

                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    var result = valueElement.GetString();
                    if (result == null)
                        return new List<Link>();

                    return JsonSerializer.Deserialize<List<Link>>(result, JsonHelper.DeserializeOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetItemLinks(): {ex.Message}");
                throw;
            }

            return new List<Link>();
        }

        // CodeUnit 50150: ItemRecordLinks
        private async Task SetItemLink(string itemNumber, string description, string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            var request = CreateRequest($"/ItemRecordLinks_SetLink?company={Company}", HttpMethod.Post);
            var body = new { itemNumber, url, description };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClientWithLogging.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Warning: SetItemLink failed for item {itemNumber}, description '{description}': {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: SetItemLink exception for item {itemNumber}, description '{description}': {ex.Message}");
            }
        }

        public async Task SetItemLinks(string itemNumber, string[] descriptions, string[] urls)
        {
            if (descriptions.Length != urls.Length)
                throw new Exception("The number of descriptions and urls must be the same.");

            for (int i = 0; i < descriptions.Length; i++)
                await SetItemLink(itemNumber, descriptions[i], urls[i]);
        }

        // CodeUnit 50149: ItemAttributes
        public async Task<List<Attribute>> GetItemAttributes(string itemNumber)
        {
            var request = CreateRequest($"/ItemAttributes_GetItemAttributes?company={Company}", HttpMethod.Post);
            var body = new { itemNumber };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var httpResponse = await _httpClientWithLogging.SendAsync(request);

                // If item doesn't exist, return empty list instead of throwing
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    return new List<Attribute>();

                httpResponse.EnsureSuccessStatusCode();

                // Read as bytes first to avoid any stream position issues
                var bytes = await httpResponse.Content.ReadAsByteArrayAsync();

                // Remove UTF-8 BOM if present (EF BB BF)
                var startIndex = 0;
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    startIndex = 3;
                }

                var content = Encoding.UTF8.GetString(bytes, startIndex, bytes.Length - startIndex).Trim();

                try
                {
                    var jsonDoc = JsonDocument.Parse(content);

                    if (jsonDoc.RootElement.TryGetProperty("value", out var valueElement))
                    {
                        var result = valueElement.GetString();
                        if (result == null)
                            return new List<Attribute>();

                        return JsonSerializer.Deserialize<List<Attribute>>(result, JsonHelper.DeserializeOptions);
                    }
                }
                catch (JsonException jex)
                {
                    Console.WriteLine($"JSON Parse Error in GetItemAttributes: {jex.Message}");
                    Console.WriteLine($"Full content: {content}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetItemAttributes: {ex.Message}");
                throw;
            }

            return new List<Attribute>();
        }

        // CodeUnit 50149: ItemAttributes
        public async Task<List<Attribute>> GetItemAttributes()
        {
            var request = CreateRequest($"/ItemAttributes_GetAllItemAttributes?company={Company}", HttpMethod.Post);

            try
            {
                var httpResponse = await _httpClientWithLogging.SendAsync(request);
                httpResponse.EnsureSuccessStatusCode();

                // Read as bytes first to avoid any stream position issues
                var bytes = await httpResponse.Content.ReadAsByteArrayAsync();

                // Remove UTF-8 BOM if present (EF BB BF)
                var startIndex = 0;
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    startIndex = 3;
                }

                var content = Encoding.UTF8.GetString(bytes, startIndex, bytes.Length - startIndex).Trim();

                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    var result = valueElement.GetString();
                    if (result == null)
                        return new List<Attribute>();

                    return JsonSerializer.Deserialize<List<Attribute>>(result, JsonHelper.DeserializeOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetItemAttributes(): {ex.Message}");
                throw;
            }

            return new List<Attribute>();
        }

        // CodeUnit 50149: ItemAttributes
        private async Task SetItemAttribute(string itemNumber, string attributeName, string attributeValue)
        {
            if (string.IsNullOrEmpty(attributeValue))
                attributeValue = string.Empty;

            var request = CreateRequest($"/ItemAttributes_SetItemAttribute?company={Company}", HttpMethod.Post);
            var body = new { itemNumber, attributeName, attributeValue };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClientWithLogging.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Warning: SetItemAttribute failed for item {itemNumber}, attribute '{attributeName}': {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: SetItemAttribute exception for item {itemNumber}, attribute '{attributeName}': {ex.Message}");
            }
        }

        public async Task SetItemAttributes(string itemNumber, string[] attributeNames, string[] attributeValues)
        {
            if (attributeNames.Length != attributeValues.Length)
                throw new Exception("The number of attribute names and values must be the same.");

            for (int i = 0; i < attributeNames.Length; i++)
                await SetItemAttribute(itemNumber, attributeNames[i], attributeValues[i]);
        }
        #endregion

        #region BOMs
        // Page 99000786: Production BOM
        public async Task<ProductionBOMMin> GetBomHeaderMin(string number)
        {
            var request = CreateRequest($"/Company('{Company}')/ProductionBOMs('{number}')?$select=No", HttpMethod.Get);
            return await _httpClientWithLogging.SendAsync<ProductionBOMMin>(request);
        }

        // Page 99000786: Production BOM
        public async Task<ProductionBOM> GetBomHeaderAndRows(string number)
        {
            var request = CreateRequest($"/Company('{Company}')/ProductionBOMs('{number}')?$expand=ProductionBOMsProdBOMLine($select=Production_BOM_No,Line_No,No,Description,Quantity_per,Unit_of_Measure_Code,Routing_Link_Code)&$select=No,Description,Unit_of_Measure_Code", HttpMethod.Get);

            try
            {
                return await _httpClientWithLogging.SendAsync<ProductionBOM>(request);
            }
            catch
            {
                return null;
            }
        }

        // Page 99000786: Production BOM
        public async Task<ProductionBOM> CreateBomHeader(ProductionBOM bomHeader)
        {
            var request = CreateRequest($"/Company('{Company}')/ProductionBOMs", HttpMethod.Post);
            var body = new
            {
                bomHeader.No,
                bomHeader.Description,
                bomHeader.Unit_of_Measure_Code,
                Status = "New"
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<ProductionBOM>(request);
        }

        // Page 99000786: Production BOM
        public async Task<ProductionBOM> UpdateBomHeader(ProductionBOM bomHeader)
        {
            var bomHeaderMin = await GetBomHeaderMin(bomHeader.No);

            var request = CreateRequest($"/Company('{Company}')/ProductionBOMs('{bomHeader.No}')", new HttpMethod("PATCH"));
            request.Headers.Add("If-Match", bomHeaderMin.odataetag);
            var body = new
            {
                bomHeader.No,
                bomHeader.Description,
                bomHeader.Unit_of_Measure_Code
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<ProductionBOM>(request);
        }

        // Page 99000788: Lines
        public async Task<ProdBOMLineMin> GetBomRowMin(string parentNumber, int position, string childNumber)
        {
            var request = CreateRequest($"/Company('{Company}')/ProductionBOMLines('{parentNumber}','',{position})?$filter=No eq '{childNumber}'&$select=Production_BOM_No,Line_No,No", HttpMethod.Get);
            return await _httpClientWithLogging.SendAsync<ProdBOMLineMin>(request);
        }

        // Page 99000788: Lines
        public async Task<ProdBOMLine> GetBomRow(string parentNumber, int position, string childNumber)
        {
            var request = CreateRequest($"/Company('{Company}')/ProductionBOMLines('{parentNumber}','',{position})?$filter=No eq '{childNumber}'$select=Production_BOM_No,Line_No,No,Description,Quantity_per,Unit_of_Measure_Code,Routing_Link_Code", HttpMethod.Get);

            try
            {
                return await _httpClientWithLogging.SendAsync<ProdBOMLine>(request);
            }
            catch
            {
                return null;
            }
        }

        // Page 99000788: Lines
        public async Task<ProdBOMLine> CreateBomRow(ProdBOMLine bomRow)
        {
            var request = CreateRequest($"/Company('{Company}')/ProductionBOMLines", HttpMethod.Post);
            var body = new
            {
                bomRow.Production_BOM_No,
                bomRow.Line_No,
                Type = "Item",
                bomRow.No,
                bomRow.Description,
                bomRow.Quantity_per,
                bomRow.Unit_of_Measure_Code,
                bomRow.Routing_Link_Code
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<ProdBOMLine>(request);
        }

        // Page 99000788: Lines
        public async Task<ProdBOMLine> UpdateBomRow(ProdBOMLine bomRow)
        {
            var bomRowMin = await GetBomRowMin(bomRow.Production_BOM_No, bomRow.Line_No, bomRow.No);

            var request = CreateRequest($"/Company('{Company}')/ProductionBOMLines('{bomRow.Production_BOM_No}','',{bomRow.Line_No})", new HttpMethod("PATCH"));
            request.Headers.Add("If-Match", bomRowMin.odataetag);
            var body = new
            {
                bomRow.Production_BOM_No,
                bomRow.Line_No,
                Type = "Item",
                bomRow.No,
                bomRow.Description,
                bomRow.Quantity_per,
                bomRow.Unit_of_Measure_Code,
                bomRow.Routing_Link_Code
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<ProdBOMLine>(request);
        }

        // Page 99000788: Lines
        public async Task DeleteBomRow(ProdBOMLine bomRow)
        {
            var bomRowMin = await GetBomRowMin(bomRow.Production_BOM_No, bomRow.Line_No, bomRow.No);

            var request = CreateRequest($"/Company('{Company}')/ProductionBOMLines('{bomRow.Production_BOM_No}','',{bomRow.Line_No})", HttpMethod.Delete);
            request.Headers.Add("If-Match", bomRowMin.odataetag);

            await _httpClientWithLogging.SendAsync(request);
        }
        #endregion

        #region Documents
        // Page 30008: APIV2 - Items
        public async Task<List<Document>> GetDocuments(string number)
        {
            var bcItemMin = await GetItemMin(number);
            if (bcItemMin == null)
                return null;

            var request = CreateRequest($"/Company('{Company}')/Items({bcItemMin.id})/ItemsdocumentAttachments", HttpMethod.Get);

            try
            {
                var response = await _httpClientWithLogging.SendAsync<ODataResponse<Document>>(request);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        // Page 30080: APIV2 - Document Attachments
        public async Task<Document> CreateDocument(string number, string fileName)
        {
            var documents = await GetDocuments(number);
            var exitingDocument = documents.SingleOrDefault(d => d.fileName.Equals(fileName));
            if (exitingDocument != null)
                return exitingDocument;

            var bcItemMin = await GetItemMin(number);

            var request = CreateRequest($"/Company('{Company}')/DocumentAttachments", HttpMethod.Post);
            var body = new
            {
                fileName,
                parentType = "Item",
                parentId = bcItemMin.id,
                lineNumber = documents.Count
            };
            var json = JsonSerializer.Serialize(body, JsonHelper.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClientWithLogging.SendAsync<Document>(request);
        }

        // Page 30008: APIV2 - Items
        public async Task<byte[]> DownloadDocument(string number, string fileName)
        {
            var bcItemMin = await GetItemMin(number);
            if (bcItemMin == null)
                return null;

            var request = CreateRequest($"/Company('{Company}')/Items({bcItemMin.id})/ItemsdocumentAttachments", HttpMethod.Get);

            try
            {
                var response = await _httpClientWithLogging.SendAsync<ODataResponse<Document>>(request);
                var documentAttachments = response.Value;
                var documentAttachment = documentAttachments.FirstOrDefault(d => d.fileName.Equals(fileName));
                if (documentAttachment == null)
                    return null;

                if (documentAttachment.attachmentContentodatamediaReadLink != null)
                {
                    return await GetResourceAsByteArray(documentAttachment.attachmentContentodatamediaReadLink);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return null;
        }

        // Page 30008: APIV2 - Items
        // Page 30080: APIV2 - Document Attachments
        public async Task UploadDocument(string number, string fileName, byte[] bytes)
        {
            var bcItemMin = await GetItemMin(number);
            if (bcItemMin == null)
                return;

            var request = CreateRequest($"/Company('{Company}')/Items({bcItemMin.id})/ItemsdocumentAttachments", HttpMethod.Get);

            try
            {
                var response = await _httpClientWithLogging.SendAsync<ODataResponse<Document>>(request);
                var documentAttachments = response.Value;
                var documentAttachment = documentAttachments.FirstOrDefault(d => d.fileName.Equals(fileName));
                if (documentAttachment == null)
                    return;

                var uploadRequest = CreateRequest($"/Company('{Company}')/DocumentAttachments({documentAttachment.id})/attachmentContent", new HttpMethod("PATCH"));
                uploadRequest.Headers.Add("If-Match", documentAttachment.odataetag);
                uploadRequest.Content = new ByteArrayContent(bytes);
                uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                await _httpClientWithLogging.SendAsync(uploadRequest);
            }
            catch
            {
                // Ignore errors
            }
        }
        #endregion
    }
}
