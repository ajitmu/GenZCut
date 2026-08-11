namespace GenZCut.Models
{
    public class Hairstyle
    {
        public int StyleId { get; set; }

        public string StyleName { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Price { get; set; }

        public int Duration { get; set; }

        public string Image { get; set; } = "";
    }
}