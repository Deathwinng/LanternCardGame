using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LanternCardGame.Pages
{
    public class NewRoomModel
    {
        private const int numberOfMinPlayers = 2;

        public NewRoomModel()
        {
            this.NumberOfPlayers = numberOfMinPlayers;
            this.MaxPoints = 100;
        }

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "{0} must be between {2} and {1} characters.")]
        [Display(Name = "Room name")]
        public string Name { get; set; }

        [Range(numberOfMinPlayers, 4, ErrorMessage = "{0} must be between {1} and {2}.")]
        [Display(Name = "Number of Players")]
        public int NumberOfPlayers { get; set; }

        [Range(20, 1000, ErrorMessage = "{0} must be between {1} and {2}.")]
        [Display(Name = "Maximum number of points")]
        public int MaxPoints { get; set; }

        public bool DeveloperMode { get; set; }
    }
}
