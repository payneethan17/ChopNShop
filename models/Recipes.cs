namespace ChopNShop.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new();
        public List<Ingredient> Ingredients { get; set; } = new();
        public int Servings { get; set; }
        public TimeSpan PrepTime { get; set; }
        public string ImagePath { get; set; } = "";
    }

    public class Ingredient
    {
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public string Unit { get; set; } = "";
    }
}