using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using RealEstate.Rentals;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RealEstate.Controllers
{
    public class RentalsController : Controller
    {
        private readonly RealEstateContextNewApis ContextNew = new RealEstateContextNewApis();

        public async Task<ActionResult> Index(RentalsFilter filters)
        {
            var filterDefinition = filters.ToFilterDefinition();

            var rentalsQuery = FilterRentals2(filters)
               .Select(r => new RentalViewModel   // Mongo v2 driver is smart enough that converts to the apropriate select
                {
                   Id = r.Id,
                   Address = r.Address,
                   Description = r.Description,
                   NumberOfRooms = r.NumberOfRooms,
                   Price = r.Price
               })
               .OrderBy(r => r.Price) // overload for .Sort(Builders<Rental>.Sort.Ascending(r => r.Price))
               .ThenByDescending(r => r.NumberOfRooms);

            //var queryObj = rentalsQuery.Filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<Rental>(), BsonSerializer.SerializerRegistry);
            //Debug.WriteLine(queryObj);

            //.Find(Builders<Rental>.Filter.Gte(r => r.NumberOfRooms, filters.MinimumRooms.Value))
            //.Find(new BsonDocument())           // basically we apply no filter if we specify only an empty new BsonDocument()
            var rentals = await rentalsQuery.ToListAsync();                     // sync/async call .ToList, .ForEachAsync, etc

            var model = new RentalsList
            {
                Rentals = rentals,
                Filters = filters
            };

            return View(model);
        }


        // GET: Rentals
        public ActionResult Post()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Post(PostRental postRental)
        {
            var rental = new Rental(postRental);
            //Context.Rentals.Insert(rental);
            await ContextNew.Rentals.InsertOneAsync(rental);
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> AdjustPrice(string id)
        {
            var rental = await GetRental(id);
            return View(rental);
        }

        [HttpPost]
        public async Task<ActionResult> AdjustPrice(string id, AdjustPrice adjustPrice)
        {
            var rental = await GetRental(id);
            //rental.AdjustPrice(adjustPrice);
            //Context.Rentals.Save(rental); // old way with complete replacement
            //UpdateOptions options = new UpdateOptions
            //{
            //    IsUpsert = true // if somehow the doccument was deleted in the mean time => do another re-insert for this document
            //};
            //await ContextNew.Rentals.ReplaceOneAsync(r => r.Id == id, rental, options); // the new v2 replacement technique

            var adjustment = new PriceAdjustment(adjustPrice, rental.Price);
            //var modificationUpdate = new UpdateBuilder<Rental>()
            //    .Push(r => r.Adjustments, adjustment)   // TRANSLATED TO $push
            //    .Set(r => r.Price, adjustPrice.NewPrice);   // => to $set
            //Context.Rentals.Update(Query.EQ("_id", new ObjectId(id)), modificationUpdate);  // so we now have a modification
            var modificationUpdate = Builders<Rental>.Update
                .Push(r => r.Adjustments, adjustment)
                .Set(r => r.Price, adjustPrice.NewPrice);
            await ContextNew.Rentals.UpdateOneAsync(r => r.Id == id, modificationUpdate);   //the same strict update/not-replace approach but with v2 driver
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Delete(string id)
        {
            //Context.Rentals.Remove(Query.EQ("_id", new ObjectId(id)));  //v1
            await ContextNew.Rentals.DeleteOneAsync(r => r.Id == id);   // v2
            return RedirectToAction("Index");
        }

        public string PriceDistribution()
        {
            //return new QueryPriceDistribution()
            //    .Run(Context.Rentals)
            //    .ToJson();

            //return new QueryPriceDistribution()
            //    .RunAggregationFluent(ContextNew.Rentals)
            //    .ToJson();

            return new QueryPriceDistribution()
                .RunLinq(ContextNew.Rentals)
                .ToJson();
        }

        public async Task<ActionResult> AttachImage(string id)
        {
            var rental = await GetRental(id);
            return View(rental);
        }

        [HttpPost]
        public async Task<ActionResult> AttachImage(string id, HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return RedirectToAction("Index");
            }

            var rental = await GetRental(id);

            if (rental.HasImage())
            {
                DeleteImageAsync(rental);
            }
            await StoreImageAsync(file, rental);

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> JoinPreLookup()
        {
            var rentals = await ContextNew.Rentals.Find(new BsonDocument()).ToListAsync();
            var rentalZips = rentals.Select(r => r.ZipCode).Distinct().ToArray();

            var zipsList = await ContextNew.Database.GetCollection<ZipCode>("zips")
                .Find(z => rentalZips.Contains(z.Id))
                .ToListAsync();
            var zipsById = zipsList.ToDictionary(d => d.Id);

            var report =
                rentals.Select(
                    r =>
                        new
                        {
                            Rental = r,
                            ZipCode = r.ZipCode != null && zipsById.ContainsKey(r.ZipCode) 
                                ? zipsById[r.ZipCode] 
                                : null
                        });

            return Content(report.ToJson(new JsonWriterSettings {OutputMode = JsonOutputMode.Strict}), "application/json");
        }

        public async Task<ActionResult> JoinWithLookup()
        {
            var report = await ContextNew.Rentals
                .Aggregate()
                //.Lookup<Rental, ZipCode, BsonDocument>(ContextNew.Database.GetCollection<ZipCode>("zips"),
                //    r => r.ZipCode,
                //    z => z.Id,
                //    d => d["zips"]
                //)
                .Lookup<Rental, ZipCode, RentalWithZipCodes>(ContextNew.Database.GetCollection<ZipCode>("zips"),
                    r => r.ZipCode,
                    z => z.Id,
                    w => w.ZipCodes
                )
                .ToListAsync();
            
            return Content(report.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict }), "application/json");
        }

        public class RentalWithZipCodes : Rental
        {
            public ZipCode[] ZipCodes { get; set; }
        }

        private async void DeleteImageAsync(Rental rental)
        {
            //better performance but less elegant than the full Replace scenario commented above
            await ContextNew.ImagesBucket.DeleteAsync(new ObjectId(rental.ImageId));
            await SetRentalImageIdAsync(rental.Id, null);
        }

        private async Task StoreImageAsync(HttpPostedFileBase file, Rental rental)
        {
           // Context.Database.GridFS.Upload(file.InputStream, file.FileName, options);
            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument("contentType", file.ContentType)
            };

           var imageId = await ContextNew.ImagesBucket.UploadFromStreamAsync(file.FileName, file.InputStream, options);
           await SetRentalImageIdAsync(rental.Id, imageId.ToString());
        }

        private async Task SetRentalImageIdAsync(string rentalId, string imageId)
        {
            //var rentalById = Query<Rental>.Where(r => r.Id == rentalId);
            //var setRentalImageId = Update<Rental>.Set(r => r.ImageId, imageId);
            //Context.Rentals.Update(rentalById, setRentalImageId);

            var setRentalImageId = Builders<Rental>.Update.Set(r => r.ImageId, imageId);
            //await ContextNew.Rentals.UpdateOneAsync(Builders<Rental>.Filter.Where(r => r.Id == rentalId), setRentalImageId);
            await ContextNew.Rentals.UpdateOneAsync(r => r.Id == rentalId, setRentalImageId);
        }

        public async Task<ActionResult> GetImage(string id)
        {
            //return File(image.OpenRead(), image.ContentType);
            try
            {
                var stream = await ContextNew.ImagesBucket.OpenDownloadStreamAsync(new ObjectId(id));
                Debug.Assert(stream != null, "stream != null");
                var contentType = stream.FileInfo.ContentType ?? stream.FileInfo.Metadata["contentType"].AsString;
                return File(stream, contentType);
            }
            catch (GridFSFileNotFoundException)
            {
                return HttpNotFound();
            }
        }

        private IMongoQueryable<Rental> FilterRentals2(RentalsFilter filters)
        {
            IMongoQueryable<Rental> rentals = ContextNew.Rentals.AsQueryable();

            if (filters.MinimumRooms.HasValue)
            {
                rentals = rentals.Where(r => r.NumberOfRooms >= filters.MinimumRooms.Value);    // the regular LINQ does not work with nullable! must specify explicily the .Value of a nullable!
            }

            if (filters.PriceLimit.HasValue)
            {
                rentals = rentals.Where(r => r.Price <= filters.PriceLimit.Value);
            }

            return rentals;
        }

        private async Task<Rental> GetRental(string id)
        {
            //var rental = Context.Rentals.FindOneById(new ObjectId(id));
            var rental = await ContextNew.Rentals
                //.Find(Builders<Rental>.Filter.Where(r => r.Id == id)) //or the version below
                .Find(r => r.Id == id)
                .FirstOrDefaultAsync();

            return rental;
        }
    }
}