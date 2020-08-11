using System;
using System.Collections.Generic;
using System.Text;

namespace Functions
{
    public class CreateRatingRequest
    {
        public string userId { get; set; }
        public string productId { get; set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }
    }

    public class CreateRatingResponse : CreateRatingRequest
    {
        public CreateRatingResponse()
        {

        }

        public CreateRatingResponse(CreateRatingRequest request)
        {
            userId = request.userId;
            productId = request.productId;
            locationName = request.locationName;
            rating = request.rating;
            userNotes = request.userNotes;
        }

        public string id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
