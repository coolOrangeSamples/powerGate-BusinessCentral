using System.Collections.Concurrent;
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
    public class BomHeader
    {
        public string Number { get; set; }
        public Item Item { get; set; }
        public List<BomRow> Children { get; set; } = new List<BomRow>();
    }

    public class BomHeaders : ServiceMethod<BomHeader>
    {
        public override IEnumerable<BomHeader> Query(IExpression<BomHeader> expression)
        {
            if (expression.IsSimpleWhereToken())
            {
                var number = (string)expression.GetWhereValueByName(nameof(BomHeader.Number));

                var bomRowTask = Api.Instance.GetBomHeaderAndRows(number);
                var itemTask = Items.GetItemByNumberAsync(number);
                Task.WhenAll(bomRowTask, itemTask).GetAwaiter().GetResult();

                var bomRow = bomRowTask.GetAwaiter().GetResult();
                if (bomRow == null)
                    return new List<BomHeader>();

                var entity = new BomHeader
                {
                    Number = bomRow.No,
                    Item = itemTask.GetAwaiter().GetResult(),
                    Children = new List<BomRow>()
                };

                var bag = new ConcurrentBag<BomRow>();
                var rowTasks = bomRow.ProductionBOMsProdBOMLine.Select(async line =>
                {
                    var row = new BomRow
                    {
                        ParentNumber = line.Production_BOM_No,
                        ChildNumber = line.No,
                        Position = line.Line_No,
                        Quantity = (double)line.Quantity_per,
                        Item = await Items.GetItemByNumberAsync(line.No),
                        IsRawMaterial = line.Routing_Link_Code == Configuration.RoutingLinkRawMaterial
                    };
                    bag.Add(row);
                });
                Task.WhenAll(rowTasks).GetAwaiter().GetResult();
                entity.Children.AddRange(bag.ToArray());

                return new List<BomHeader> { entity };
            }

            throw new System.NotSupportedException();
        }

        public override void Update(BomHeader entity)
        {
            var item = Api.Instance.GetItemCard(entity.Number).GetAwaiter().GetResult();
            var bomHeader = new ProductionBOM
            {
                No = entity.Number,
                Description = item.Description,
                Unit_of_Measure_Code = item.Base_Unit_of_Measure
            };
            var tasks = new List<Task>
            {
                Api.Instance.UpdateBomHeader(bomHeader),
                Api.Instance.UpdateItemCardProductionBom(item.No)
            };
            Task.WhenAll(tasks.ToArray()).GetAwaiter().GetResult();
        }

        public override void Create(BomHeader entity)
        {
            var item = Api.Instance.GetItemCard(entity.Number).GetAwaiter().GetResult();
            var bomHeader = new ProductionBOM
            {
                No = entity.Number,
                Description = item.Description,
                Unit_of_Measure_Code = item.Base_Unit_of_Measure
            };
            var tasks = new List<Task>
            {
                Api.Instance.CreateBomHeader(bomHeader),
                Api.Instance.UpdateItemCardProductionBom(item.No)
            };
            Task.WhenAll(tasks.ToArray()).GetAwaiter().GetResult();
        }

        public override void Delete(BomHeader entity)
        {
            throw new System.NotSupportedException();
        }
    }
}