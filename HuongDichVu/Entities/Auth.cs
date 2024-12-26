using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HuongDichVu.Entities
{
    public partial class Auth
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        [JsonIgnore]
        public string? ResetToken { get; set; }
        [JsonIgnore]
        public virtual ICollection<FavoriteBooks> FavoriteBooks { get; set; }
        [JsonIgnore]
        public virtual ICollection<FeedBack> Feedbacks { get; set; }
    }
}
