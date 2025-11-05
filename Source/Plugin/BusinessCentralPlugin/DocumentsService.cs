using System.Collections.Generic;
using System.Data.Services.Common;
using System.IO;
using System.Linq;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using BusinessCentralPlugin.BusinessCentral;
using BusinessCentralPlugin.Helper;
using powerGateServer.SDK;

namespace BusinessCentralPlugin
{
    [DataServiceKey("Number", "FileName")]
    [DataServiceEntity]
    public class Document : Streamable
    {
        public string Number { get; set; }

        public override string GetContentType()
        {
            return ContentTypes.Application.Pdf;
        }
    }

    public class Documents : ServiceMethod<Document>, IStreamableServiceMethod<Document>
    {
        public override IEnumerable<Document> Query(IExpression<Document> expression)
        {
            var results = new List<Document>();

            if (expression.Where.Any(b => b.PropertyName.Equals("Number")))
            {
                var number = (string)expression.GetWhereValueByName(nameof(Item.Number));
                var documents = Api.Instance.GetDocuments(number).GetAwaiter().GetResult();
                foreach (var document in documents)
                {
                    results.Add(new Document
                    {
                        Number = number,
                        FileName = document.fileName
                    });
                }
            }
            else
            {
                throw new System.NotSupportedException();
            }

            return results;
        }

        public override void Create(Document entity)
        {
            _ = Api.Instance.CreateDocument(entity.Number, entity.FileName).GetAwaiter().GetResult();
        }

        public override void Update(Document entity)
        {
            throw new System.NotImplementedException();
        }

        public override void Delete(Document entity)
        {
            throw new System.NotImplementedException();
        }

        public IStream Download(Document entity)
        {
            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");

            var bytes = Api.Instance.DownloadDocument(entity.Number, entity.FileName).GetAwaiter().GetResult();

            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.Headers["Content-Disposition"] = $"filename={entity.FileName}";

            return new powerGateServer.SDK.Streams.ByteStream(bytes);
        }

        public void Upload(Document entity, IStream stream)
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                stream.Source.CopyToAsync(ms).GetAwaiter().GetResult();
                bytes = ms.ToArray();
            }

            Api.Instance.UploadDocument(entity.Number, entity.FileName, bytes).GetAwaiter().GetResult();
        }

        public void DeleteStream(Document entity)
        {
            throw new System.NotImplementedException();
        }
    }
}