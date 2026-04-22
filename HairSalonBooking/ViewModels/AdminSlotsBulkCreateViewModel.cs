using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HairSalonBooking.ViewModels;

public class AdminSlotsBulkCreateViewModel
{
    [Required(ErrorMessage = "Wybierz usługę.")]
    [Display(Name = "Usługa")]
    public int? ServiceId { get; set; }

    [Required(ErrorMessage = "Wybierz datę początkową.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data od")]
    public DateTime? DateFrom { get; set; }

    [Required(ErrorMessage = "Wybierz datę końcową.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data do")]
    public DateTime? DateTo { get; set; }

    [Required(ErrorMessage = "Podaj godzinę rozpoczęcia dnia.")]
    [Display(Name = "Godzina od")]
    public TimeSpan? DayStartTime { get; set; }

    [Required(ErrorMessage = "Podaj godzinę zakończenia dnia.")]
    [Display(Name = "Godzina do")]
    public TimeSpan? DayEndTime { get; set; }

    [Range(0, 240, ErrorMessage = "Przerwa musi mieścić się w zakresie 0-240 minut.")]
    [Display(Name = "Przerwa między slotami (min)")]
    public int GapMinutes { get; set; }

    [Display(Name = "Terminy oznaczone jako zajęte")]
    public bool IsBooked { get; set; }

    [Display(Name = "Pomijaj duplikaty")]
    public bool SkipDuplicates { get; set; } = true;

    [Display(Name = "Poniedziałek")]
    public bool Monday { get; set; } = true;

    [Display(Name = "Wtorek")]
    public bool Tuesday { get; set; } = true;

    [Display(Name = "Środa")]
    public bool Wednesday { get; set; } = true;

    [Display(Name = "Czwartek")]
    public bool Thursday { get; set; } = true;

    [Display(Name = "Piątek")]
    public bool Friday { get; set; } = true;

    [Display(Name = "Sobota")]
    public bool Saturday { get; set; } = true;

    [Display(Name = "Niedziela")]
    public bool Sunday { get; set; } = true;

    public IReadOnlyList<SelectListItem> ServiceOptions { get; set; } = [];

    public bool HasAnySelectedDay() =>
        Monday || Tuesday || Wednesday || Thursday || Friday || Saturday || Sunday;
}
