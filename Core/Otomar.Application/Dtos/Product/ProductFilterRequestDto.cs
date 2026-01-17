using System.ComponentModel.DataAnnotations;

namespace Otomar.Application.Dtos.Product
{
    public class ProductFilterRequestDto
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        [Range(1, int.MaxValue)]
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value <= 0 ? 1 : value;
        }

        [Range(10, 100)]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value switch
            {
                <= 10 => 10,
                >= 100 => 100,
                _ => value
            };
        }

        public string? OrderBy { get; set; }
        public string? MainCategory { get; set; }
        public string? SubCategory { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Year { get; set; }
        public string? Manufacturer { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxPrice { get; set; }

        [MaxLength(100)]
        public string? SearchTerm { get; set; }
    }
}