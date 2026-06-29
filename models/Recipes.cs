namespace ChopNShop.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new();
        public List<Ingredient> Ingredients { get; set; } = new();
        public int? Servings { get; set; }
        public TimeSpan? PrepTime { get; set; }
        public string ImagePath { get; set; } = "";

        // Builds a human-friendly string for prep time, used by the .cshtml
        // pages instead of working with the raw TimeSpan directly.
        // Examples: null -> "-", 45 minutes -> "45 min", 90 minutes -> "1 hr 30 min",
        // 120 minutes -> "2 hr".
        public string PrepTimeDisplay
        {
            get
            {
                //No prep time was ever entered
                if (!PrepTime.HasValue)
                {
                    return "-";
                }

                int totalMinutes = (int)PrepTime.Value.TotalMinutes;
                int hours = totalMinutes / 60;
                int minutes = totalMinutes % 60;

                //59 minutes or fewer just shows minutes
                if (hours == 0)
                {
                    return $"{minutes} min";
                }

                //minutes equal an even number of hours with no leftover minutes
                if(minutes == 0)
                {
                    return $"{hours} hr";
                }

                //general case with both hours and minutes
                return $"{hours} hr {minutes} min";
            }
        }

        // Similarly, builds a friendly string for Servings, showing "-" when
        // it was never set, instead of leaving the .cshtml files to repeat
        // this same null-check logic in multiple places.
        public string ServingsDisplay => Servings.HasValue ? Servings.Value.ToString() : "-";
    }

    public class Ingredient
    {
        public string Name { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string Unit { get; set; } = "";
    }
}