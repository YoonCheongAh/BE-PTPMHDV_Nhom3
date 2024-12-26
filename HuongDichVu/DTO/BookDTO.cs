using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HuongDichVu.DTO
{
    public class BookDTO
    {
        public int Id { get; set; }
        public string? Upc { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public decimal? Price { get; set; }
        public string? ImgSrc { get; set; }
        public int? StarRating { get; set; }
        public string? Status { get; set; }
        public int? ViewCount { get; set; }
        public string? Description { get; set; }
    }
}
