namespace GenZCut.Models
{
    public class Barber
    {
        public int BarberId { get; set; }

        public int UserId { get; set; }

        public int Experience { get; set; }

        public string Specialization { get; set; } = "";

        public string Status { get; set; } = "";
    }
}