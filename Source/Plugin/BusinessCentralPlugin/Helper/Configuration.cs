using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BusinessCentralPlugin.BusinessCentral;

namespace BusinessCentralPlugin.Helper
{
    public class Configuration
    {
        public static string Company;        
        public static string AuthType;
        public static bool EnableStartupCheck;

        public static string BaseUrl;

        public static string TenantId;
        public static string ClientId;
        public static string ClientSecret;

        public static string Username;
        public static string Password;

        public static string DefaultItemType;
        public static string DefaultItemCategoryCode;
        public static string DefaultInventoryPostingGroup;
        public static string DefaultGeneralProductPostingGroup;
        public static string DefaultReplenishmentSystem;
        public static string ReplenishmentSystemPurchaseIndicator;
        public static string ItemAttributeDescription;
        public static string ItemAttributeMaterial;
        public static string ItemLinkThinClient;
        public static string ItemLinkThickClient;
        public static string RoutingLinkRawMaterial;

        public static List<Lookup> UnitsOfMeasures;
        public static List<Vendor> Vendors;

        private static List<Company> _companies;
        private static List<AttributeDefinition> _attributeDefinitions;
        private static List<Lookup> _itemCategories;
        private static List<Lookup> _inventoryPostingGroups;
        private static List<Lookup> _generalProductPostingGroups;
        private static List<RoutingLink> _routingLinks;

        public static void Initialize()
        {
            using (new Timer())
            {
                ReadConfiguration();
                ValidateConfiguration();

                var tasks = new List<Task>
                {
                    Task.Run(ValidateConfigurationAdvanced),
                    Task.Run(CacheLookups)
                };
                Task.WaitAll(tasks.ToArray());
            }
        }

        private static void ReadConfiguration()
        {
            var configFullName = Assembly.GetExecutingAssembly().Location + ".config";
            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = configFullName };
            var configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            var mainSection = configuration.GetSection("BusinessCentral") as AppSettingsSection;
            if (mainSection == null)
                throw new ConfigurationErrorsException(
                    "BusinessCentral section is missing in powerGateBusinessCentralPlugin.dll.config");

            Company = mainSection.Settings["Company"].Value;            
            AuthType = mainSection.Settings["AuthType"].Value;
            EnableStartupCheck = mainSection.Settings["EnableStartupCheck"].Value.ToLower() == "true";

            if (AuthType == "Basic")
            {
                var basicSection = configuration.GetSection("BusinessCentral.BasicAuth") as AppSettingsSection;
                if (basicSection == null)
                    throw new ConfigurationErrorsException(
                        "BusinessCentral.BasicAuth section is missing in powerGateBusinessCentralPlugin.dll.config");

                BaseUrl = basicSection.Settings["BaseUrl"].Value.TrimEnd('/');
                Username = basicSection.Settings["Username"].Value;
                Password = basicSection.Settings["Password"].Value;
            }
            else if (AuthType == "OAuth")
            {
                var oAuthSection = configuration.GetSection("BusinessCentral.OAuth") as AppSettingsSection;
                if (oAuthSection == null)
                    throw new ConfigurationErrorsException(
                        "BusinessCentral.OAuth section is missing in powerGateBusinessCentralPlugin.dll.config");

                BaseUrl = oAuthSection.Settings["BaseUrl"].Value.TrimEnd('/');
                TenantId = oAuthSection.Settings["TenantId"].Value;
                ClientId = oAuthSection.Settings["ClientId"].Value;
                ClientSecret = oAuthSection.Settings["ClientSecret"].Value;
            }

            var settingsSection = configuration.GetSection("BusinessCentral.Settings") as AppSettingsSection;
            if (settingsSection == null)
                throw new ConfigurationErrorsException(
                    "BusinessCentral.Settings section is missing in powerGateBusinessCentralPlugin.dll.config");

            DefaultItemType = settingsSection.Settings["Default_Item_Type"]?.Value;
            DefaultItemCategoryCode = settingsSection.Settings["Default_Item_Category_Code"]?.Value;
            DefaultInventoryPostingGroup = settingsSection.Settings["Default_Inventory_Posting_Group"]?.Value;
            DefaultGeneralProductPostingGroup = settingsSection.Settings["Default_General_Product_Posting_Group"]?.Value;
            DefaultReplenishmentSystem = settingsSection.Settings["Default_Replenishment_System"]?.Value;
            ReplenishmentSystemPurchaseIndicator = settingsSection.Settings["Replenishment_System_Purchase_Indicator"]?.Value;
            ItemAttributeDescription = settingsSection.Settings["Item_Attribute_Description"]?.Value;
            ItemAttributeMaterial = settingsSection.Settings["Item_Attribute_Material"]?.Value;
            ItemLinkThinClient = settingsSection.Settings["Item_Link_ThinClient"]?.Value;
            ItemLinkThickClient = settingsSection.Settings["Item_Link_ThickClient"]?.Value;
            RoutingLinkRawMaterial = settingsSection.Settings["Routing_Link_RawMaterial"]?.Value;
        }

        private static void ValidateConfiguration()
        {
            if (AuthType == "OAuth")
            {
                if (string.IsNullOrEmpty(BaseUrl))
                    throw new ConfigurationErrorsException("BaseUrl cannot be empty");

                if (string.IsNullOrEmpty(TenantId))
                    throw new ConfigurationErrorsException("TenantId cannot be empty");

                if (string.IsNullOrEmpty(ClientId))
                    throw new ConfigurationErrorsException("ClientId cannot be empty");

                if (string.IsNullOrEmpty(ClientSecret))
                    throw new ConfigurationErrorsException("ClientSecret cannot be empty");
            }
            else if (AuthType == "Basic")
            {
                if (string.IsNullOrEmpty(BaseUrl))
                    throw new ConfigurationErrorsException("BaseUrl cannot be empty");

                if (string.IsNullOrEmpty(Username))
                    throw new ConfigurationErrorsException("Username cannot be empty");

                if (string.IsNullOrEmpty(Password))
                    throw new ConfigurationErrorsException("Password cannot be empty");
            }
            else
            {
                throw new ConfigurationErrorsException("AuthType is not configured correctly in powerGateBusinessCentralPlugin.dll.config");
            }

            if (string.IsNullOrEmpty(DefaultItemType))
                throw new ConfigurationErrorsException("Default_Item_Type cannot be empty");

            if (!new[] { "Inventory", "Service", "Non-Inventory" }.Contains(DefaultItemType))
                throw new ConfigurationErrorsException($"Default_Item_Type cannot be '{DefaultItemType}'");

            if (string.IsNullOrEmpty(ItemAttributeDescription))
                throw new ConfigurationErrorsException($"Item_Attribute_Description cannot be '{ItemAttributeDescription}'");

            if (string.IsNullOrEmpty(ItemAttributeMaterial))
                throw new ConfigurationErrorsException($"Item_Attribute_Material cannot be '{ItemAttributeMaterial}'");

            if (string.IsNullOrEmpty(ItemLinkThinClient))
                throw new ConfigurationErrorsException($"Item_Link_ThinClient cannot be '{ItemLinkThinClient}'");

            if (string.IsNullOrEmpty(ItemLinkThickClient))
                throw new ConfigurationErrorsException($"Item_Link_ThickClient cannot be '{ItemLinkThickClient}'");
        }

        private static void ValidateConfigurationAdvanced()
        {
            if (EnableStartupCheck)
            {
                var companies = Api.Instance.GetCompanies();
                var attributeDefinitions = Api.Instance.GetItemAttributeDefinitions();
                var itemCategories = Api.Instance.GetItemCategories();
                var inventoryPostingGroups = Api.Instance.GetInventoryPostingGroups();
                var generalProductPostingGroups = Api.Instance.GetGeneralProductPostingGroups();
                var routingLinks = Api.Instance.GetRoutingLinks();

                var tasks = new List<Task>
                {
                    companies,
                    attributeDefinitions,
                    itemCategories,
                    inventoryPostingGroups,
                    generalProductPostingGroups,
                    routingLinks
                };

                Task.WaitAll(tasks.ToArray());

                _companies = companies.Result;
                _attributeDefinitions = attributeDefinitions.Result;
                _itemCategories = itemCategories.Result;
                _inventoryPostingGroups = inventoryPostingGroups.Result;
                _generalProductPostingGroups = generalProductPostingGroups.Result;
                _routingLinks = routingLinks.Result;

                if (!_companies.Any(i => i.Name.Equals(Company)))
                    throw new ConfigurationErrorsException($"Company cannot be '{Company}'");

                if (!_attributeDefinitions.Any(i => i.Name.Equals(ItemAttributeDescription)))
                    throw new ConfigurationErrorsException($"Item_Attribute_Description cannot be '{ItemAttributeDescription}'");

                if (!_attributeDefinitions.Any(i => i.Name.Equals(ItemAttributeMaterial)))
                    throw new ConfigurationErrorsException($"Item_Attribute_Description cannot be '{ItemAttributeMaterial}'");

                if (!_itemCategories.Any(i => i.code.Equals(DefaultItemCategoryCode)))
                    throw new ConfigurationErrorsException($"Default_Item_Category_Code cannot be '{DefaultItemCategoryCode}'");

                if (!_inventoryPostingGroups.Any(i => i.code.Equals(DefaultInventoryPostingGroup)))
                    throw new ConfigurationErrorsException($"Default_Inventory_Posting_Group cannot be '{DefaultInventoryPostingGroup}'");

                if (!_generalProductPostingGroups.Any(i => i.code.Equals(DefaultGeneralProductPostingGroup)))
                    throw new ConfigurationErrorsException($"Default_General_Product_Posting_Group cannot be '{DefaultGeneralProductPostingGroup}'");

                if (!_generalProductPostingGroups.Any(i => i.code.Equals(ReplenishmentSystemPurchaseIndicator)))
                    throw new ConfigurationErrorsException($"Replenishment_System_Purchase_Indicator cannot be '{ReplenishmentSystemPurchaseIndicator}'");

                if (!_routingLinks.Any(i => i.Code.Equals(RoutingLinkRawMaterial)))
                    throw new ConfigurationErrorsException($"Routing_Link_RawMaterial cannot be '{RoutingLinkRawMaterial}'");
            }
        }

        private static void CacheLookups()
        {
            var unitsOfMeasures = Api.Instance.GetUnitsOfMeasures();
            var vendors = Api.Instance.GetVendors();
            var tasks = new List<Task> { unitsOfMeasures, vendors };
            Task.WaitAll(tasks.ToArray());

            UnitsOfMeasures = unitsOfMeasures.Result;
            Vendors = vendors.Result;
        }
    }
}