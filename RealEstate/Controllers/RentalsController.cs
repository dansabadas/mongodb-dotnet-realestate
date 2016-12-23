using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using RealEstate.Rentals;

namespace RealEstate.Controllers
{
    public class RentalsController : Controller
    {
        private readonly RealEstateContext Context = new RealEstateContext();

        public ActionResult Index(RentalsFilter filters)
        {
            var rentals = FilterRentals(filters);
                //.SetSortOrder(SortBy<Rental>.Ascending(r => r.Price));
            var model = new RentalsList
            {
                Rentals = rentals,
                Filters = filters
            };

            return View(model);
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

        // GET: Rentals
        public ActionResult Post()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Post(PostRental postRental)
        {
            var rental = new Rental(postRental);
            Context.Rentals.Insert(rental);
            return RedirectToAction("Index");
        }

        public ActionResult AdjustPrice(string id)
        {
            var rental = GetRental(id);
            return View(rental);
        }

        private Rental GetRental(string id)
        {
            var rental = Context.Rentals.FindOneById(new ObjectId(id));
            return rental;
        }

        [HttpPost]
        public ActionResult AdjustPrice(string id, AdjustPrice adjustPrice)
        {
            var rental = GetRental(id);
            //rental.AdjustPrice(adjustPrice);
            //Context.Rentals.Save(rental); // old way with complete replacement
            var adjustment = new PriceAdjustment(adjustPrice, rental.Price);
            var modificationUpdate = new UpdateBuilder<Rental>()
                .Push(r => r.Adjustments, adjustment)   // TRANSLATED TO $push
                .Set(r => r.Price, adjustPrice.NewPrice);   // => to $set
            Context.Rentals.Update(Query.EQ("_id", new ObjectId(id)), modificationUpdate);  // so we now have a modification
            return RedirectToAction("Index");
        }

        public ActionResult Delete(string id)
        {
            Context.Rentals.Remove(Query.EQ("_id", new ObjectId(id)));
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

            //
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
    }
}