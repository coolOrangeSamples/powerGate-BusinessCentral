using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.Linq;
using System.Threading.Tasks;
using BusinessCentralPlugin.BusinessCentral;
using BusinessCentralPlugin.Helper;
using powerGateServer.SDK;

namespace BusinessCentralPlugin
{
    [DataServiceKey(nameof(Number))]
    [DataServiceEntity]
    public class Item
    {
        public string Number { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public double Weight { get; set; }
        public string Material { get; set; }
        public double Price { get; set; } // readonly
        public double Stock { get; set; } // readonly
        public bool MakeBuy { get; set; } // readonly
        public bool Blocked { get; set; } // readonly
        public string Supplier { get; set; } // readonly
        public byte[] Thumbnail { get; set; }
        public string ThinClientLink { get; set; }
        public string ThickClientLink { get; set; }
    }

    public class Items : ServiceMethod<Item>
    {
        public override IEnumerable<Item> Query(IExpression<Item> expression)
        {
            if (expression.IsSimpleWhereToken())
            {
                var number = (string)expression.GetWhereValueByName(nameof(Item.Number));
                return new List<Item> { GetItemByNumberAsync(number, true).GetAwaiter().GetResult() };
            }

            //TODO: Implement filter
            return GetAllItems();
        }

        public static async Task<Item> GetItemByNumberAsync(string number, bool includeThumbnail = false)
        {
            var bcItemCardTask = Api.Instance.GetItemCard(number);
            var bcAttributesTask = Api.Instance.GetItemAttributes(number);
            var bcLinksTask = Api.Instance.GetItemLinks(number);

            //await Task.WhenAll(bcItemCardTask, bcAttributesTask, bcLinksTask);

            var bcItemCard = await bcItemCardTask;
            var bcAttributes = await bcAttributesTask;
            var bcLinks = await bcLinksTask;

            if (bcItemCard == null)
                return new Item();

            var item = ComposeItem(bcItemCard, bcAttributes, bcLinks);
            if (includeThumbnail)
            {
                var picture = await Api.Instance.GetItemPicture(number);
                item.Thumbnail = picture;
            }

            return item;
        }

        private static IEnumerable<Item> GetAllItems()
        {
            var items = new List<Item>();

            var bcItemCardsTask = Api.Instance.GetItemCards();
            var bcAttributesTask = Api.Instance.GetItemAttributes();
            var bcLinksTask = Api.Instance.GetItemLinks();

            Task.WhenAll(bcItemCardsTask, bcAttributesTask, bcLinksTask).GetAwaiter().GetResult();

            var bcItemCards = bcItemCardsTask.GetAwaiter().GetResult();
            var bcAttributes = bcAttributesTask.GetAwaiter().GetResult();
            var bcLinks = bcLinksTask.GetAwaiter().GetResult();

            foreach (var bcItemCard in bcItemCards)
                items.Add(ComposeItem(bcItemCard, bcAttributes, bcLinks));

            return items;
        }

        private static Item ComposeItem(ItemCard bcItemCard, List<BusinessCentral.Attribute> bcAttributes, List<Link> bcLinks)
        {
            return new Item
            {
                Number = bcItemCard.No,
                Title = bcItemCard.Description,
                Description = bcAttributes.SingleOrDefault(l => l.itemNumber.Equals(bcItemCard.No) && l.attribute.Equals(Configuration.ItemAttributeDescription))?.value,
                UnitOfMeasure = bcItemCard.Base_Unit_of_Measure,
                Weight = bcItemCard.Net_Weight,
                Material = bcAttributes.SingleOrDefault(l => l.itemNumber.Equals(bcItemCard.No) && l.attribute.Equals(Configuration.ItemAttributeMaterial))?.value,
                Price = bcItemCard.Unit_Price,
                Stock = bcItemCard.Inventory,
                MakeBuy = bcItemCard.Replenishment_System == Configuration.ReplenishmentSystemPurchaseIndicator,
                Blocked = bcItemCard.Blocked,
                Supplier = Configuration.Vendors.SingleOrDefault(v => v.number.Equals(bcItemCard.Vendor_No))?.displayName,
                ThinClientLink = bcLinks.FirstOrDefault(l => l.itemNumber.Equals(bcItemCard.No) && l.description.Equals(Configuration.ItemLinkThinClient))?.url,
                ThickClientLink = bcLinks.FirstOrDefault(l => l.itemNumber.Equals(bcItemCard.No) && l.description.Equals(Configuration.ItemLinkThickClient))?.url
            };
        }

        public override void Update(Item entity)
        {
            var bcItemCard = Api.Instance.UpdateItemCard(new ItemCard
            {
                No = entity.Number,
                Description = entity.Title,
                Base_Unit_of_Measure = entity.UnitOfMeasure,
                Net_Weight = entity.Weight
            }).GetAwaiter().GetResult();

            var tasks = new List<Task>
            {
                Api.Instance.SetItemPicture(bcItemCard.No, entity.Thumbnail),
                Api.Instance.SetItemAttributes(
                    bcItemCard.No,
                    new []{ Configuration.ItemAttributeDescription, Configuration.ItemAttributeMaterial },
                    new []{ entity.Description, entity.Material }
                    ),
                Api.Instance.SetItemLinks(
                    bcItemCard.No,
                    new []{ Configuration.ItemLinkThinClient, Configuration.ItemLinkThickClient },
                    new []{ entity.ThinClientLink, entity.ThickClientLink }
                    )
            };
            Task.WhenAll(tasks.ToArray()).GetAwaiter().GetResult();
        }

        public override void Create(Item entity)
        {
            var bcItemCard = Api.Instance.CreateItemCard(new ItemCard
            {
                No = entity.Number,
                Description = entity.Title,
                Base_Unit_of_Measure = entity.UnitOfMeasure,
                Net_Weight = entity.Weight
            }).GetAwaiter().GetResult();

            var tasks = new List<Task>
            {
                Api.Instance.SetItemPicture(bcItemCard.No, entity.Thumbnail),
                Api.Instance.SetItemAttributes(
                    bcItemCard.No,
                    new []{ Configuration.ItemAttributeDescription, Configuration.ItemAttributeMaterial },
                    new []{ entity.Description, entity.Material }
                ),
                Api.Instance.SetItemLinks(
                    bcItemCard.No,
                    new []{ Configuration.ItemLinkThinClient, Configuration.ItemLinkThickClient },
                    new []{ entity.ThinClientLink, entity.ThickClientLink }
                )
            };
            Task.WhenAll(tasks.ToArray()).GetAwaiter().GetResult();
        }

        public override void Delete(Item entity)
        {
            throw new System.NotSupportedException();
        }
    }
}