using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LanternCardGame.Pages
{
    public class NewRoomModel
    {
        public NewRoomModel()
        {
            this.NumberOfPlayers = 1;
            this.MaxPoints = 100;
        }

        //[Required]
        [StringLength(20, MinimumLength = 3)]
        public string Name { get; set; }

        [Range(1, 4)]
        [Display(Name = "Number of Players")]
        public int NumberOfPlayers { get; set; }

        [Range(20, 1000)]
        [Display(Name = "Max number of points")]
        public int MaxPoints { get; set; }

        public bool DeveloperMode { get; set; }
    }
}
