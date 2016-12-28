using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using RealEstate.Rentals;

namespace RealEstate.Controllers
{
    public class RentalsController : Controller
    {
        private readonly RealEstateContext Context = new RealEstateContext();
        private readonly RealEstateContextNewApis ContextNew = new RealEstateContextNewApis();

        public async Task<ActionResult> Index(RentalsFilter filters)
        {
            //var rentals = FilterRentals(filters);
            //    //.SetSortOrder(SortBy<Rental>.Ascending(r => r.Price));
            var filterDefinition = filters.ToFilterDefinition();

            var rentalsQuery = ContextNew.Rentals
                .Find(filterDefinition)
                .Project(r => new RentalViewModel   // Mongo v2 driver is smart enough that converts to the apropriate select
                {
                    Id = r.Id,
                    Address = r.Address,
                    Description = r.Description,
                    NumberOfRooms = r.NumberOfRooms,
                    Price = r.Price
                })
                .SortBy(r => r.Price) // overload for .Sort(Builders<Rental>.Sort.Ascending(r => r.Price))
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

        public ActionResult AdjustPrice(string id)
        {
            var rental = GetRental(id);
            return View(rental);
        }

        [HttpPost]
        public async Task<ActionResult> AdjustPrice(string id, AdjustPrice adjustPrice)
        {
            var rental = GetRental(id);
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
            return new QueryPriceDistribution()
                .Run(Context.Rentals)
                .ToJson();
        }

        public ActionResult AttachImage(string id)
        {
            var rental = GetRental(id);
            return View(rental);
        }

        [HttpPost]
        public ActionResult AttachImage(string id, HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return RedirectToAction("Index");
            }

            var rental = GetRental(id);

            //scenario 1: first save the image file => 2. then save the rental with reference to the previsouly saved file
            //var options = new MongoGridFSCreateOptions
            //{
            //    ContentType = file.ContentType
            //};
            //var fileInfo = Context.Database.GridFS.Upload(file.InputStream, file.FileName, options);
            //rental.ImageId = fileInfo.Id.AsObjectId.ToString();
            //Context.Rentals.Save(rental);

            //scenario 2: if the file upload fails, we will only will have an invalid reference in the rental to it, but at least we won't have
            //possible orphaned files in the gridfs system

            if (rental.HasImage())
            {
                DeleteImage(rental);
            }
            StoreImage(file, rental);

            return RedirectToAction("Index");
        }

        private void DeleteImage(Rental rental)
        {
            Context.Database.GridFS.DeleteById(new ObjectId(rental.ImageId));
            //rental.ImageId = null;
            //Context.Rentals.Save(rental);

            //better performance but less elegant than the full Replace scenario commented above
            SetRentalImageId(rental.Id, null);
        }

        private void StoreImage(HttpPostedFileBase file, Rental rental)
        {
            var imageId = ObjectId.GenerateNewId();
            rental.ImageId = imageId.ToString();
            Context.Rentals.Save(rental);
            var options = new MongoGridFSCreateOptions
            {
                ContentType = file.ContentType,
                Id = imageId
            };

            Context.Database.GridFS.Upload(file.InputStream, file.FileName, options);
        }

        private void SetRentalImageId(string rentalId, string imageId)
        {
            var rentalById = Query<Rental>.Where(r => r.Id == rentalId);
            var setRentalImageId = Update<Rental>.Set(r => r.ImageId, imageId);
            Context.Rentals.Update(rentalById, setRentalImageId);
        }

        public ActionResult GetImage(string id)
        {
            var image = Context.Database.GridFS.FindOneById(new ObjectId(id));
            if (image == null)
            {
                return HttpNotFound();
            }

            return File(image.OpenRead(), image.ContentType);
        }

        private IEnumerable<Rental> FilterRentals(RentalsFilter filters)
        {
            IQueryable<Rental> rentals = Context.Rentals.AsQueryable()
                .OrderBy(r => r.Price);

            if (filters.MinimumRooms.HasValue)
            {
                //var query = Query<Rental>.GTE(r => r.NumberOfRooms, filters.MinimumRooms);
                //rentals = rentals.Where(r => query.Inject());
                rentals = rentals.Where(r => r.NumberOfRooms >= filters.MinimumRooms.Value);    // the regular LINQ does not work with nullable! must specify explicily the .Value of a nullable!
            }

            if (filters.PriceLimit.HasValue)
            {
                var query = Query<Rental>.LTE(r => r.Price, filters.PriceLimit);
                rentals = rentals.Where(r => query.Inject());
            }

            return rentals;

            //var query = Query<Rental>.LTE(r => r.Price, filters.PriceLimit);
            //return Context.Rentals.Find(query);
        }

        private Rental GetRental(string id)
        {
            //var rental = Context.Rentals.FindOneById(new ObjectId(id));
            var rental = ContextNew.Rentals
                //.Find(Builders<Rental>.Filter.Where(r => r.Id == id)) //or the version below
                .Find(r => r.Id == id)
                .FirstOrDefault();

            return rental;
        }
    }
}