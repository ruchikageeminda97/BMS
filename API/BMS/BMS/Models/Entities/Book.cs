using System.ComponentModel.DataAnnotations;

namespace BMS.Models.Entities
{
    public class Book
    {
        public Guid bookId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public required string title { get; set; }

        public string description { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        public string author { get; set; }

        [Required(ErrorMessage = "ISBN is required.")]
        [RegularExpression(@"^\d{10}$|^\d{13}$|^(?:\d{1,5}[-]\d{1,7}[-]\d{1,6}[-]\d)$|^(?:\d{1,5}[-]\d{1,7}[-]\d{1,6}[-][\dX])$", ErrorMessage = "ISBN must be a valid 10 or 13-digit number (with or without hyphens).")]
        public string isbn { get; set; }

        [Required(ErrorMessage = "Publication date is required.")]
        public DateOnly publicationDate { get; set; }
    }
}